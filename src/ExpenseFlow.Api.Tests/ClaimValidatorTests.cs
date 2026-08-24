using ExpenseFlow.Domain;
using Xunit;

namespace ExpenseFlow.Api.Tests;

public class ClaimValidatorTests
{
    private static readonly DateOnly Today = new(2026, 8, 24);

    [Fact]
    public void Validate_WithPositiveAmountAndNonFutureDate_ReturnsValid()
    {
        var result = ClaimValidator.Validate(42.50m, Today.AddDays(-1), Today);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_WithNonPositiveAmount_ReturnsInvalid(decimal amount)
    {
        var result = ClaimValidator.Validate(amount, Today, Today);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("positive", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithFutureExpenseDate_ReturnsInvalid()
    {
        var result = ClaimValidator.Validate(10m, Today.AddDays(1), Today);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("future", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithTodayAsExpenseDate_ReturnsValid()
    {
        var result = ClaimValidator.Validate(10m, Today, Today);

        Assert.True(result.IsValid);
    }
}
