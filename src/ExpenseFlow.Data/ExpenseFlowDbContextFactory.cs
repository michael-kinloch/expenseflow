using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpenseFlow.Data;

public class ExpenseFlowDbContextFactory : IDesignTimeDbContextFactory<ExpenseFlowDbContext>
{
    public ExpenseFlowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ExpenseFlowDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=ExpenseFlow;Trusted_Connection=True;TrustServerCertificate=True;");

        return new ExpenseFlowDbContext(optionsBuilder.Options);
    }
}
