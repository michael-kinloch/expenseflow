namespace ExpenseFlow.Data.Entities;

public enum DecisionOutcome
{
    Approved,
    Rejected
}

public class ClaimDecision
{
    public Guid Id { get; set; }

    public Guid ClaimId { get; set; }

    public ExpenseClaim? Claim { get; set; }

    public Guid DecidedBy { get; set; }

    public User? DecidedByUser { get; set; }

    public DecisionOutcome Decision { get; set; }

    public string? Comment { get; set; }

    public DateTimeOffset DecidedAt { get; set; }
}
