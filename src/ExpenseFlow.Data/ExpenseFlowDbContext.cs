using ExpenseFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Data;

public class ExpenseFlowDbContext(DbContextOptions<ExpenseFlowDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<ExpenseClaim> ExpenseClaims => Set<ExpenseClaim>();

    public DbSet<ClaimDecision> ClaimDecisions => Set<ClaimDecision>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.Name).HasMaxLength(256).IsRequired();

            entity.HasOne(u => u.Manager)
                .WithMany(u => u.DirectReports)
                .HasForeignKey(u => u.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExpenseClaim>(entity =>
        {
            entity.Property(c => c.Amount).HasColumnType("decimal(18,2)");
            entity.Property(c => c.Currency).HasMaxLength(3).IsRequired();
            entity.Property(c => c.Category).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(1000).IsRequired();
            entity.Property(c => c.Status).IsConcurrencyToken();

            entity.HasOne(c => c.Employee)
                .WithMany()
                .HasForeignKey(c => c.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Decision)
                .WithOne(d => d.Claim)
                .HasForeignKey<ClaimDecision>(d => d.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ClaimDecision>(entity =>
        {
            entity.HasIndex(d => d.ClaimId).IsUnique();

            entity.HasOne(d => d.DecidedByUser)
                .WithMany()
                .HasForeignKey(d => d.DecidedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
