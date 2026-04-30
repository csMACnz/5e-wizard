using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates point-buy ability score allocations against SRD rules.
/// </summary>
public static class PointBuyValidator
{
    /// <summary>
    /// Standard SRD point-buy cost table: score → cost.
    /// </summary>
    private static readonly Dictionary<int, int> DefaultCosts = new()
    {
        { 8, 0 }, { 9, 1 }, { 10, 2 }, { 11, 3 },
        { 12, 4 }, { 13, 5 }, { 14, 7 }, { 15, 9 },
    };

    public const int DefaultBudget = 27;
    public const int DefaultMinScore = 8;
    public const int DefaultMaxScore = 15;

    /// <summary>
    /// Validates the six base ability scores against point-buy rules.
    /// </summary>
    /// <param name="scores">The six base scores (before racial bonuses).</param>
    /// <param name="budget">Total point budget (default 27).</param>
    /// <param name="costs">Cost table; if null uses the SRD defaults.</param>
    public static ValidationResult Validate(
        IReadOnlyList<int> scores,
        int budget = DefaultBudget,
        Dictionary<int, int>? costs = null)
    {
        var result = new ValidationResult();
        var costTable = costs ?? DefaultCosts;

        if (scores.Count != 6)
        {
            result.Errors.Add("ERR_POINTBUY_COUNT: Exactly six ability scores are required.");
            return result;
        }

        int total = 0;
        for (int i = 0; i < scores.Count; i++)
        {
            int score = scores[i];
            if (score < DefaultMinScore || score > DefaultMaxScore)
            {
                result.Errors.Add(
                    $"ERR_POINTBUY_RANGE: Score {score} at index {i} is outside the allowed range " +
                    $"[{DefaultMinScore},{DefaultMaxScore}].");
                continue;
            }

            if (!costTable.TryGetValue(score, out int cost))
            {
                result.Errors.Add($"ERR_POINTBUY_UNKNOWN_SCORE: No cost defined for score {score}.");
                continue;
            }

            total += cost;
        }

        if (result.Errors.Count > 0)
            return result;

        if (total > budget)
        {
            result.Errors.Add(
                $"ERR_POINTBUY_BUDGET: Total cost {total} exceeds the allowed budget of {budget}.");
        }
        else if (total < budget)
        {
            result.Warnings.Add(
                $"WARN_POINTBUY_UNDERSPEND: Total cost {total} is below budget {budget}; " +
                "some points are unused.");
        }

        return result;
    }
}
