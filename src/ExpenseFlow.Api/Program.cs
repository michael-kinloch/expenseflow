using ExpenseFlow.Api.Authorization;
using ExpenseFlow.Api.Endpoints;
using ExpenseFlow.Data;
using ExpenseFlow.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<ExpenseFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ExpenseFlow")));

builder.Services
    .AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ExpenseFlowDbContext>()
    .AddSignInManager();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ManagerOfClaim", policy => policy.Requirements.Add(new ManagerOfClaimRequirement()));

builder.Services.AddScoped<IAuthorizationHandler, ManagerOfClaimHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapClaimsEndpoints();

app.Run();

public partial class Program;
