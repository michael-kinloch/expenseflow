using System.Security.Claims;
using System.Text.Json.Serialization;
using ExpenseFlow.Api.Authorization;
using ExpenseFlow.Data;
using ExpenseFlow.Data.Entities;
using ExpenseFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Api.Endpoints;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record CreateClaimRequest(
    decimal Amount,
    string Currency,
    string Category,
    DateOnly ExpenseDate,
    string Description,
    string? ReceiptUrl);

public record DecisionRequest(string Decision, string? Comment);

public record DecisionResponse(Guid DecidedBy, string Decision, string? Comment, DateTimeOffset DecidedAt);

public record ClaimResponse(
    Guid Id,
    Guid EmployeeId,
    decimal Amount,
    string Currency,
    string Category,
    DateOnly ExpenseDate,
    string Description,
    string? ReceiptUrl,
    string Status,
    DateTimeOffset SubmittedAt,
    DecisionResponse? Decision);

public static class ClaimsEndpoints
{
    public static void MapClaimsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/claims").RequireAuthorization();

        group.MapPost("", CreateClaim);
        group.MapGet("/mine", GetMyClaims);
        group.MapGet("/pending", GetPendingClaims);
        group.MapGet("/{id:guid}", GetClaimById);
        group.MapPost("/{id:guid}/decision", DecideClaim);
    }

    private static async Task<IResult> CreateClaim(
        CreateClaimRequest request,
        ClaimsPrincipal principal,
        ExpenseFlowDbContext db,
        TimeProvider timeProvider,
        ILogger<Program> logger)
    {
        var employeeId = GetUserId(principal);
        if (employeeId is null)
        {
            return Results.Unauthorized();
        }

        var now = timeProvider.GetUtcNow();
        var validation = ClaimValidator.Validate(
            request.Amount,
            request.ExpenseDate,
            DateOnly.FromDateTime(now.UtcDateTime),
            request.Currency,
            request.Category,
            request.Description);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["claim"] = [.. validation.Errors] });
        }

        var claim = new ExpenseClaim
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId.Value,
            Amount = request.Amount,
            Currency = request.Currency,
            Category = request.Category,
            ExpenseDate = request.ExpenseDate,
            Description = request.Description,
            ReceiptUrl = request.ReceiptUrl,
            Status = ClaimStatus.Pending,
            SubmittedAt = now
        };

        db.ExpenseClaims.Add(claim);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Actor {ActorId} performed {Action} on claim {ClaimId} at {Timestamp}",
            employeeId, "SubmitClaim", claim.Id, now);

        return Results.Created($"/api/claims/{claim.Id}", ToResponse(claim));
    }

    private static async Task<IResult> GetMyClaims(ClaimsPrincipal principal, ExpenseFlowDbContext db)
    {
        var employeeId = GetUserId(principal);
        if (employeeId is null)
        {
            return Results.Unauthorized();
        }

        var claims = await db.ExpenseClaims
            .Include(c => c.Decision)
            .Where(c => c.EmployeeId == employeeId)
            .OrderByDescending(c => c.SubmittedAt)
            .ToListAsync();

        return Results.Ok(claims.Select(ToResponse));
    }

    private static async Task<IResult> GetPendingClaims(
        ClaimsPrincipal principal, ExpenseFlowDbContext db, IAuthorizationService authorizationService)
    {
        var managerId = GetUserId(principal);
        if (managerId is null)
        {
            return Results.Unauthorized();
        }

        var pendingClaims = await db.ExpenseClaims
            .Include(c => c.Employee)
            .Where(c => c.Status == ClaimStatus.Pending)
            .OrderBy(c => c.SubmittedAt)
            .ToListAsync();

        var authorized = new List<ExpenseClaim>();
        foreach (var claim in pendingClaims)
        {
            var authorizationResult = await authorizationService.AuthorizeAsync(principal, claim, "ManagerOfClaim");
            if (authorizationResult.Succeeded)
            {
                authorized.Add(claim);
            }
        }

        return Results.Ok(authorized.Select(ToResponse));
    }

    private static async Task<IResult> GetClaimById(
        Guid id, ClaimsPrincipal principal, ExpenseFlowDbContext db, IAuthorizationService authorizationService)
    {
        var callerId = GetUserId(principal);
        if (callerId is null)
        {
            return Results.Unauthorized();
        }

        var claim = await db.ExpenseClaims
            .Include(c => c.Employee)
            .Include(c => c.Decision)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (claim is null)
        {
            return Results.NotFound();
        }

        if (claim.EmployeeId != callerId)
        {
            var authorizationResult = await authorizationService.AuthorizeAsync(principal, claim, "ManagerOfClaim");
            if (!authorizationResult.Succeeded)
            {
                return Results.Forbid();
            }
        }

        return Results.Ok(ToResponse(claim));
    }

    private static async Task<IResult> DecideClaim(
        Guid id,
        DecisionRequest request,
        ClaimsPrincipal principal,
        ExpenseFlowDbContext db,
        IAuthorizationService authorizationService,
        TimeProvider timeProvider,
        ILogger<Program> logger)
    {
        var callerId = GetUserId(principal);
        if (callerId is null)
        {
            return Results.Unauthorized();
        }

        var claim = await db.ExpenseClaims
            .Include(c => c.Employee)
            .Include(c => c.Decision)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (claim is null)
        {
            return Results.NotFound();
        }

        var authorizationResult = await authorizationService.AuthorizeAsync(principal, claim, "ManagerOfClaim");
        if (!authorizationResult.Succeeded)
        {
            return Results.Forbid();
        }

        if (claim.Status != ClaimStatus.Pending || claim.Decision is not null)
        {
            return Results.Conflict(new { message = "This claim has already been decided." });
        }

        if (!Enum.TryParse<DecisionOutcome>(request.Decision, ignoreCase: true, out var outcome))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["decision"] = ["Decision must be 'approved' or 'rejected'."]
            });
        }

        var decidedAt = timeProvider.GetUtcNow();

        var decision = new ClaimDecision
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            DecidedBy = callerId.Value,
            Decision = outcome,
            Comment = request.Comment,
            DecidedAt = decidedAt
        };

        claim.Status = outcome == DecisionOutcome.Approved ? ClaimStatus.Approved : ClaimStatus.Rejected;
        db.ClaimDecisions.Add(decision);
        claim.Decision = decision;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // The pre-check above is check-then-act and not race-safe: a concurrent request can pass it
            // before either commits. ExpenseClaim.Status is a concurrency token (see ExpenseFlowDbContext)
            // and ClaimDecision.ClaimId has a unique index, so the loser of a genuine race fails here
            // instead of double-deciding the claim.
            return Results.Conflict(new { message = "This claim has already been decided." });
        }

        logger.LogInformation(
            "Actor {ActorId} performed {Action} on claim {ClaimId} at {Timestamp}",
            callerId, "DecideClaim", claim.Id, decidedAt);

        return Results.Ok(ToResponse(claim));
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var guid) ? guid : null;
    }

    private static ClaimResponse ToResponse(ExpenseClaim claim) => new(
        claim.Id,
        claim.EmployeeId,
        claim.Amount,
        claim.Currency,
        claim.Category,
        claim.ExpenseDate,
        claim.Description,
        claim.ReceiptUrl,
        claim.Status.ToString(),
        claim.SubmittedAt,
        claim.Decision is null
            ? null
            : new DecisionResponse(claim.Decision.DecidedBy, claim.Decision.Decision.ToString(), claim.Decision.Comment, claim.Decision.DecidedAt));
}
