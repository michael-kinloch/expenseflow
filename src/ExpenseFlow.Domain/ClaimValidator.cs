namespace ExpenseFlow.Domain;

public record ClaimValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ClaimValidationResult Success { get; } = new(true, Array.Empty<string>());
}

public static class ClaimValidator
{
    public static ClaimValidationResult Validate(
        decimal amount,
        DateOnly expenseDate,
        DateOnly today,
        string? currency,
        string? category,
        string? description)
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

        if (string.IsNullOrWhiteSpace(currency))
        {
            errors.Add("Currency is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            errors.Add("Category is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors.Add("Description is required.");
        }

        return errors.Count == 0 ? ClaimValidationResult.Success : new ClaimValidationResult(false, errors);
    }
}
