namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates rolled ability scores against SRD rules.
/// </summary>
public static class RollValidator
{
    public const int MinScore = 3;
    public const int MaxScore = 18;
    public const int RequiredCount = 6;

    /// <summary>
    /// Validates rolled ability scores (each in [3, 18], exactly 6 values).
    /// </summary>
    /// <param name="scores">The six rolled scores (before racial bonuses).</param>
    public static ValidationResult Validate(IReadOnlyList<int> scores)
    {
        var result = new ValidationResult();

        if (scores.Count != RequiredCount)
        {
            result.Errors.Add(
                $"ERR_ROLL_COUNT: Exactly {RequiredCount} ability scores are required, but got {scores.Count}.");
            return result;
        }

        for (int i = 0; i < scores.Count; i++)
        {
            int score = scores[i];
            if (score < MinScore || score > MaxScore)
            {
                result.Errors.Add(
                    $"ERR_ROLL_RANGE: Score {score} at index {i} is outside the allowed range " +
                    $"[{MinScore},{MaxScore}].");
            }
        }

        return result;
    }
}
