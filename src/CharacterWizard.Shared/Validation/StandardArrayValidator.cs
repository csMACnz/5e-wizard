namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates that the six base scores match the standard array [15, 14, 13, 12, 10, 8].
/// </summary>
public static class StandardArrayValidator
{
    private static readonly IReadOnlyList<int> DefaultStandardArray = [15, 14, 13, 12, 10, 8];

    /// <summary>
    /// Validates that the provided scores are exactly the standard array values (any order, each used exactly once).
    /// </summary>
    /// <param name="scores">The six base scores (before racial bonuses).</param>
    /// <param name="standardArray">The expected standard array; defaults to [15, 14, 13, 12, 10, 8].</param>
    public static ValidationResult Validate(IReadOnlyList<int> scores, IReadOnlyList<int>? standardArray = null)
    {
        var result = new ValidationResult();
        var expected = standardArray ?? DefaultStandardArray;

        var sorted = scores.OrderByDescending(x => x).ToList();
        var sortedExpected = expected.OrderByDescending(x => x).ToList();

        if (!sorted.SequenceEqual(sortedExpected))
        {
            var arrayStr = string.Join(", ", sortedExpected);
            result.Errors.Add(
                $"ERR_STDARRAY_INVALID: Scores do not match the standard array [{arrayStr}]. " +
                "Each value must be used exactly once.");
        }

        return result;
    }
}
