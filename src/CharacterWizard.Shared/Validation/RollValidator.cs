namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates rolled ability scores against SRD rules.
/// </summary>
public static class RollValidator
{
    /// <summary>Minimum possible score from 4d6-drop-lowest.</summary>
    public const int MinScore = 3;

    /// <summary>Maximum possible score from 4d6-drop-lowest.</summary>
    public const int MaxScore = 18;

    /// <summary>Default number of ability scores to roll (matches <c>roll.count</c> in abilities.json).</summary>
    public const int RequiredCount = 6;

    /// <summary>
    /// Validates rolled ability scores (each in [3, 18], exactly <paramref name="count"/> values).
    /// </summary>
    /// <param name="scores">The rolled scores (before racial bonuses).</param>
    /// <param name="count">Expected number of scores; defaults to <see cref="RequiredCount"/> (6).</param>
    public static ValidationResult Validate(IReadOnlyList<int> scores, int count = RequiredCount)
    {
        var result = new ValidationResult();

        if (scores.Count != count)
        {
            result.Errors.Add(
                $"ERR_ROLL_COUNT: Exactly {count} ability scores are required, but got {scores.Count}.");
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
