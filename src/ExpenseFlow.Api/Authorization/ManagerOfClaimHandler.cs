using System.Security.Claims;
using ExpenseFlow.Data.Entities;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseFlow.Api.Authorization;

public class ManagerOfClaimRequirement : IAuthorizationRequirement;

public class ManagerOfClaimHandler : AuthorizationHandler<ManagerOfClaimRequirement, ExpenseClaim>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ManagerOfClaimRequirement requirement,
        ExpenseClaim resource)
    {
        var callerId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(callerId, out var callerGuid) && resource.Employee?.ManagerId == callerGuid)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
