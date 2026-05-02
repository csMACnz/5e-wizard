using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

public class PointBuyValidatorTests
{
    [Fact]
    public void ValidBudget_ExactlySpent_ReturnsValid()
    {
        // 8+8+8+8+8+15 = 0+0+0+0+0+9 = 9 points — not 27, but let's use a proper combo
        // 15+15+15+8+8+8   = 9+9+9+0+0+0 = 27 ✓
        var scores = new[] { 15, 15, 15, 8, 8, 8 };
        var result = PointBuyValidator.Validate(scores);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void ValidBudget_Underspend_ReturnsWarning()
    {
        // All 8s = 0 cost; valid but warning
        var scores = new[] { 8, 8, 8, 8, 8, 8 };
        var result = PointBuyValidator.Validate(scores);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Contains("WARN_POINTBUY_UNDERSPEND", result.Warnings[0]);
    }

    [Fact]
    public void OverBudget_ReturnsError()
    {
        // 15+15+15+15+8+8 = 9+9+9+9+0+0 = 36 > 27
        var scores = new[] { 15, 15, 15, 15, 8, 8 };
        var result = PointBuyValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_POINTBUY_BUDGET"));
    }

    [Fact]
    public void ScoreOutOfRange_Low_ReturnsError()
    {
        var scores = new[] { 7, 10, 10, 10, 10, 10 };
        var result = PointBuyValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_POINTBUY_RANGE"));
    }

    [Fact]
    public void ScoreOutOfRange_High_ReturnsError()
    {
        var scores = new[] { 16, 10, 10, 10, 10, 10 };
        var result = PointBuyValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_POINTBUY_RANGE"));
    }

    [Fact]
    public void WrongCount_ReturnsError()
    {
        var scores = new[] { 10, 10, 10, 10, 10 }; // only 5
        var result = PointBuyValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_POINTBUY_COUNT"));
    }

    [Fact]
    public void CustomBudget_RespectsNewBudget()
    {
        // With budget 9, spending 9 exactly: 15+8+8+8+8+8
        var scores = new[] { 15, 8, 8, 8, 8, 8 };
        var result = PointBuyValidator.Validate(scores, budget: 9);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void PointBuyConfig_Overload_ValidScores_ReturnsValid()
    {
        var config = new PointBuyConfig
        {
            Budget = 27,
            MinScore = 8,
            MaxScore = 15,
            Costs =
            [
                new PointBuyCost { Score = 8, Cost = 0 },
                new PointBuyCost { Score = 9, Cost = 1 },
                new PointBuyCost { Score = 10, Cost = 2 },
                new PointBuyCost { Score = 11, Cost = 3 },
                new PointBuyCost { Score = 12, Cost = 4 },
                new PointBuyCost { Score = 13, Cost = 5 },
                new PointBuyCost { Score = 14, Cost = 7 },
                new PointBuyCost { Score = 15, Cost = 9 },
            ],
        };
        var scores = new[] { 15, 15, 15, 8, 8, 8 }; // 9+9+9+0+0+0 = 27
        var result = PointBuyValidator.Validate(scores, config);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void PointBuyConfig_Overload_OverBudget_ReturnsError()
    {
        var config = new PointBuyConfig
        {
            Budget = 27,
            MinScore = 8,
            MaxScore = 15,
            Costs =
            [
                new PointBuyCost { Score = 8, Cost = 0 },
                new PointBuyCost { Score = 15, Cost = 9 },
            ],
        };
        var scores = new[] { 15, 15, 15, 15, 8, 8 }; // 36 > 27
        var result = PointBuyValidator.Validate(scores, config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_POINTBUY_BUDGET"));
    }

    [Fact]
    public void PointBuyConfig_Overload_ScoreOutOfRange_ReturnsError()
    {
        var config = new PointBuyConfig
        {
            Budget = 27,
            MinScore = 8,
            MaxScore = 15,
            Costs = [new PointBuyCost { Score = 8, Cost = 0 }],
        };
        var scores = new[] { 7, 10, 10, 10, 10, 10 }; // 7 < min 8
        var result = PointBuyValidator.Validate(scores, config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_POINTBUY_RANGE"));
    }

    [Fact]
    public void CustomMinMax_OverrideDefaults()
    {
        // Custom range: 6–18, budget 40
        var customCosts = new Dictionary<int, int>
        {
            { 6, 0 }, { 7, 1 }, { 8, 2 }, { 9, 3 }, { 10, 4 },
            { 11, 5 }, { 12, 6 }, { 13, 8 }, { 14, 10 }, { 15, 12 },
            { 16, 14 }, { 17, 16 }, { 18, 18 },
        };
        var scores = new[] { 6, 6, 6, 6, 6, 6 }; // all min = 0 points
        var result = PointBuyValidator.Validate(scores, budget: 40, costs: customCosts, minScore: 6, maxScore: 18);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
