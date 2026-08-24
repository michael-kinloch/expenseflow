using System.Net;
using System.Net.Http.Json;
using ExpenseFlow.Api.Endpoints;
using ExpenseFlow.Data.Entities;
using Xunit;

namespace ExpenseFlow.Api.Tests;

public class ClaimsEndpointsTests : IClassFixture<ClaimsEndpointsTestFactory>
{
    private readonly ClaimsEndpointsTestFactory _factory;

    public ClaimsEndpointsTests(ClaimsEndpointsTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, TestUser User)> CreateLoggedInUserAsync(
        string emailPrefix, string name, UserRole role, Guid? managerId = null)
    {
        var email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com";
        var user = await _factory.SeedUserAsync(email, name, role, managerId);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, user.Password));
        response.EnsureSuccessStatusCode();

        return (client, user);
    }

    [Fact]
    public async Task PostClaims_WithValidData_Returns201AndPendingStatus()
    {
        var (client, _) = await CreateLoggedInUserAsync("employee", "Employee One", UserRole.Employee);

        var request = new CreateClaimRequest(25.00m, "GBP", "Travel", _factory.TimeProvider.Now.AddDays(-1).Date.ToDateOnly(), "Taxi", null);
        var response = await client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ClaimResponse>();
        Assert.NotNull(created);
        Assert.Equal("Pending", created!.Status);

        var mine = await client.GetFromJsonAsync<List<ClaimResponse>>("/api/claims/mine");
        Assert.Contains(mine!, c => c.Id == created.Id && c.Status == "Pending");
    }

    [Fact]
    public async Task PostClaims_WithNonPositiveAmount_ReturnsValidationError()
    {
        var (client, _) = await CreateLoggedInUserAsync("employee", "Employee Two", UserRole.Employee);

        var request = new CreateClaimRequest(0m, "GBP", "Travel", _factory.TimeProvider.Now.Date.ToDateOnly(), "Taxi", null);
        var response = await client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var mine = await client.GetFromJsonAsync<List<ClaimResponse>>("/api/claims/mine");
        Assert.Empty(mine!);
    }

    [Fact]
    public async Task PostClaims_WithFutureExpenseDate_ReturnsValidationError()
    {
        var (client, _) = await CreateLoggedInUserAsync("employee", "Employee Three", UserRole.Employee);

        var request = new CreateClaimRequest(10m, "GBP", "Travel", _factory.TimeProvider.Now.AddDays(5).Date.ToDateOnly(), "Taxi", null);
        var response = await client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var mine = await client.GetFromJsonAsync<List<ClaimResponse>>("/api/claims/mine");
        Assert.Empty(mine!);
    }

    [Fact]
    public async Task GetPendingClaims_ReturnsOnlyDirectReportsClaims()
    {
        var manager = await _factory.SeedUserAsync($"manager-{Guid.NewGuid():N}@example.com", "Manager A", UserRole.Manager);
        var (reportClient, report) = await CreateLoggedInUserAsync("report", "Report A", UserRole.Employee, manager.Id);
        var (otherClient, _) = await CreateLoggedInUserAsync("other", "Other Employee", UserRole.Employee);

        var claimDate = _factory.TimeProvider.Now.Date.ToDateOnly();
        await reportClient.PostAsJsonAsync("/api/claims", new CreateClaimRequest(15m, "GBP", "Meals", claimDate, "Lunch", null));
        await otherClient.PostAsJsonAsync("/api/claims", new CreateClaimRequest(15m, "GBP", "Meals", claimDate, "Lunch", null));

        var managerClient = _factory.CreateClient();
        var loginResponse = await managerClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(manager.Email, manager.Password));
        loginResponse.EnsureSuccessStatusCode();

        var pending = await managerClient.GetFromJsonAsync<List<ClaimResponse>>("/api/claims/pending");

        Assert.NotNull(pending);
        Assert.All(pending!, c => Assert.Equal(report.Id, c.EmployeeId));
        Assert.Contains(pending!, c => c.EmployeeId == report.Id);
    }

    [Fact]
    public async Task GetPendingClaims_WithNoPendingClaims_ReturnsEmptyList()
    {
        var (managerClient, _) = await CreateLoggedInUserAsync("lonelymanager", "Lonely Manager", UserRole.Manager);

        var response = await managerClient.GetAsync("/api/claims/pending");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var pending = await response.Content.ReadFromJsonAsync<List<ClaimResponse>>();
        Assert.NotNull(pending);
        Assert.Empty(pending!);
    }

    [Fact]
    public async Task PostDecision_ByDirectManager_Returns200AndUpdatesClaim()
    {
        var manager = await _factory.SeedUserAsync($"manager-{Guid.NewGuid():N}@example.com", "Manager B", UserRole.Manager);
        var (reportClient, _) = await CreateLoggedInUserAsync("report", "Report B", UserRole.Employee, manager.Id);

        var claimDate = _factory.TimeProvider.Now.Date.ToDateOnly();
        var createResponse = await reportClient.PostAsJsonAsync("/api/claims", new CreateClaimRequest(30m, "GBP", "Meals", claimDate, "Dinner", null));
        var created = await createResponse.Content.ReadFromJsonAsync<ClaimResponse>();

        var managerClient = _factory.CreateClient();
        var loginResponse = await managerClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(manager.Email, manager.Password));
        loginResponse.EnsureSuccessStatusCode();

        var decisionResponse = await managerClient.PostAsJsonAsync(
            $"/api/claims/{created!.Id}/decision", new DecisionRequest("Approved", "Looks good"));

        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);

        var decided = await decisionResponse.Content.ReadFromJsonAsync<ClaimResponse>();
        Assert.Equal("Approved", decided!.Status);
        Assert.NotNull(decided.Decision);
        Assert.Equal(manager.Id, decided.Decision!.DecidedBy);
        Assert.Equal("Looks good", decided.Decision.Comment);
    }

    [Fact]
    public async Task PostDecision_ByNonManager_Returns403()
    {
        var manager = await _factory.SeedUserAsync($"manager-{Guid.NewGuid():N}@example.com", "Manager C", UserRole.Manager);
        var (reportClient, _) = await CreateLoggedInUserAsync("report", "Report C", UserRole.Employee, manager.Id);
        var (strangerClient, _) = await CreateLoggedInUserAsync("stranger", "Stranger", UserRole.Manager);

        var claimDate = _factory.TimeProvider.Now.Date.ToDateOnly();
        var createResponse = await reportClient.PostAsJsonAsync("/api/claims", new CreateClaimRequest(30m, "GBP", "Meals", claimDate, "Dinner", null));
        var created = await createResponse.Content.ReadFromJsonAsync<ClaimResponse>();

        var decisionResponse = await strangerClient.PostAsJsonAsync(
            $"/api/claims/{created!.Id}/decision", new DecisionRequest("Approved", null));

        Assert.Equal(HttpStatusCode.Forbidden, decisionResponse.StatusCode);
    }

    [Fact]
    public async Task GetClaim_ByOtherEmployee_Returns403()
    {
        var (ownerClient, _) = await CreateLoggedInUserAsync("owner", "Owner", UserRole.Employee);
        var (otherClient, _) = await CreateLoggedInUserAsync("other", "Other", UserRole.Employee);

        var claimDate = _factory.TimeProvider.Now.Date.ToDateOnly();
        var createResponse = await ownerClient.PostAsJsonAsync("/api/claims", new CreateClaimRequest(12m, "GBP", "Meals", claimDate, "Coffee", null));
        var created = await createResponse.Content.ReadFromJsonAsync<ClaimResponse>();

        var response = await otherClient.GetAsync($"/api/claims/{created!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostDecision_OnAlreadyDecidedClaim_Returns409AndDoesNotChangeDecision()
    {
        var manager = await _factory.SeedUserAsync($"manager-{Guid.NewGuid():N}@example.com", "Manager D", UserRole.Manager);
        var (reportClient, _) = await CreateLoggedInUserAsync("report", "Report D", UserRole.Employee, manager.Id);

        var claimDate = _factory.TimeProvider.Now.Date.ToDateOnly();
        var createResponse = await reportClient.PostAsJsonAsync("/api/claims", new CreateClaimRequest(30m, "GBP", "Meals", claimDate, "Dinner", null));
        var created = await createResponse.Content.ReadFromJsonAsync<ClaimResponse>();

        var managerClient = _factory.CreateClient();
        var loginResponse = await managerClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(manager.Email, manager.Password));
        loginResponse.EnsureSuccessStatusCode();

        var firstDecision = await managerClient.PostAsJsonAsync(
            $"/api/claims/{created!.Id}/decision", new DecisionRequest("Approved", "First decision"));
        Assert.Equal(HttpStatusCode.OK, firstDecision.StatusCode);

        var secondDecision = await managerClient.PostAsJsonAsync(
            $"/api/claims/{created.Id}/decision", new DecisionRequest("Rejected", "Second decision"));
        Assert.Equal(HttpStatusCode.Conflict, secondDecision.StatusCode);

        var stored = await _factory.GetClaimAsync(created.Id);
        Assert.Equal(ClaimStatus.Approved, stored!.Status);
        Assert.Equal("First decision", stored.Decision!.Comment);
    }

    [Fact]
    public async Task GetClaim_AfterDecision_ShowsOutcomeAndComment()
    {
        var manager = await _factory.SeedUserAsync($"manager-{Guid.NewGuid():N}@example.com", "Manager E", UserRole.Manager);
        var (reportClient, _) = await CreateLoggedInUserAsync("report", "Report E", UserRole.Employee, manager.Id);

        var claimDate = _factory.TimeProvider.Now.Date.ToDateOnly();
        var createResponse = await reportClient.PostAsJsonAsync("/api/claims", new CreateClaimRequest(30m, "GBP", "Meals", claimDate, "Dinner", null));
        var created = await createResponse.Content.ReadFromJsonAsync<ClaimResponse>();

        var managerClient = _factory.CreateClient();
        var loginResponse = await managerClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(manager.Email, manager.Password));
        loginResponse.EnsureSuccessStatusCode();

        await managerClient.PostAsJsonAsync($"/api/claims/{created!.Id}/decision", new DecisionRequest("Rejected", "Missing receipt"));

        var viewResponse = await reportClient.GetAsync($"/api/claims/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, viewResponse.StatusCode);

        var viewed = await viewResponse.Content.ReadFromJsonAsync<ClaimResponse>();
        Assert.Equal("Rejected", viewed!.Status);
        Assert.Equal("Missing receipt", viewed.Decision!.Comment);
    }
}

internal static class DateTimeExtensions
{
    public static DateOnly ToDateOnly(this DateTime dateTime) => DateOnly.FromDateTime(dateTime);
}
