namespace ExpenseFlow.Data.Entities;

public enum ClaimStatus
{
    Pending,
    Approved,
    Rejected
}

public class ExpenseClaim
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public User? Employee { get; set; }

    public required decimal Amount { get; set; }

    public required string Currency { get; set; }

    public required string Category { get; set; }

    public required DateOnly ExpenseDate { get; set; }

    public required string Description { get; set; }

    public string? ReceiptUrl { get; set; }

    public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

    public DateTimeOffset SubmittedAt { get; set; }

    public ClaimDecision? Decision { get; set; }
}
