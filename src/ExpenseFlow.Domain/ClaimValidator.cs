namespace ExpenseFlow.Domain;

public record ClaimValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ClaimValidationResult Success { get; } = new(true, Array.Empty<string>());
}

public static class ClaimValidator
{
    public static ClaimValidationResult Validate(decimal amount, DateOnly expenseDate, DateOnly today)
    {
        var errors = new List<string>();

        if (amount <= 0)
        {
            errors.Add("Amount must be a positive number.");
        }

        if (expenseDate > today)
        {
            errors.Add("Expense date cannot be in the future.");
        }

        return errors.Count == 0 ? ClaimValidationResult.Success : new ClaimValidationResult(false, errors);
    }
}
