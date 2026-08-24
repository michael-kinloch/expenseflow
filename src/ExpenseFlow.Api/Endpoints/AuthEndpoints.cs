using System.Security.Claims;
using ExpenseFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace ExpenseFlow.Api.Endpoints;

public record LoginRequest(string Email, string Password);

public record CurrentUserResponse(Guid Id, string Name, string Email, string Role);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (LoginRequest request, SignInManager<User> signInManager) =>
        {
            var result = await signInManager.PasswordSignInAsync(
                request.Email, request.Password, isPersistent: true, lockoutOnFailure: true);

            return result.Succeeded ? Results.Ok() : Results.Unauthorized();
        });

        group.MapPost("/logout", async (SignInManager<User> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok();
        }).RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<User> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new CurrentUserResponse(user.Id, user.Name, user.Email!, user.Role.ToString()));
        }).RequireAuthorization();
    }
}
