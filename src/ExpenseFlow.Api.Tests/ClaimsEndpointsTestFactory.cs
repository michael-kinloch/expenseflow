using ExpenseFlow.Data;
using ExpenseFlow.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpenseFlow.Api.Tests;

public class ClaimsEndpointsTestFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public FakeTimeProvider TimeProvider { get; } = new(new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ExpenseFlowDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ExpenseFlowDbContext>>();
            services.RemoveAll<TimeProvider>();

            services.AddDbContext<ExpenseFlowDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.AddSingleton<TimeProvider>(TimeProvider);
        });
    }

    public async Task<TestUser> SeedUserAsync(string email, string name, UserRole role, Guid? managerId = null)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            Name = name,
            Role = role,
            ManagerId = managerId,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, "Password1!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return new TestUser(user.Id, email, "Password1!");
    }

    public async Task SeedClaimAsync(ExpenseClaim claim)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
        db.ExpenseClaims.Add(claim);
        await db.SaveChangesAsync();
    }

    public async Task<ExpenseClaim?> GetClaimAsync(Guid id)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
        return await db.ExpenseClaims.Include(c => c.Decision).FirstOrDefaultAsync(c => c.Id == id);
    }
}

public record TestUser(Guid Id, string Email, string Password);

public class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
