namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates that the six base scores match the standard array [15, 14, 13, 12, 10, 8].
/// </summary>
public static class StandardArrayValidator
{
    private static readonly IReadOnlyList<int> StandardArray = [15, 14, 13, 12, 10, 8];

    /// <summary>
    /// Validates that the provided scores are exactly the standard array values (any order, each used exactly once).
    /// </summary>
    /// <param name="scores">The six base scores (before racial bonuses).</param>
    public static ValidationResult Validate(IReadOnlyList<int> scores)
    {
        var result = new ValidationResult();

        var sorted = scores.OrderByDescending(x => x).ToList();
        var expected = StandardArray.OrderByDescending(x => x).ToList();

        if (!sorted.SequenceEqual(expected))
        {
            result.Errors.Add(
                "ERR_STDARRAY_INVALID: Scores do not match the standard array [15, 14, 13, 12, 10, 8]. " +
                "Each value must be used exactly once.");
        }

        return result;
    }
}
