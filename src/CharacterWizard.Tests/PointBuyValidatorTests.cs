using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

public class PointBuyValidatorTests
{
    [Fact]
    public void ValidBudget_ExactlySpent_ReturnsValid()
    {
        // 8+8+8+8+8+15 = 0+0+0+0+0+9 = 9 points — not 27, but let's use a proper combo
        // 15+15+13+12+10+8 = 9+9+5+4+2+0 = 29 — over budget
        // 14+14+13+12+10+8 = 7+7+5+4+2+0 = 25 — under 27
        // 15+14+13+12+10+9 = 9+7+5+4+2+1 = 28 — over
        // 15+14+13+10+10+8 = 9+7+5+2+2+0 = 25
        // 15+14+13+12+8+8  = 9+7+5+4+0+0 = 25
        // 15+15+12+10+8+8  = 9+9+4+2+0+0 = 24
        // 15+15+13+10+8+8  = 9+9+5+2+0+0 = 25
        // 15+15+14+8+8+8   = 9+9+7+0+0+0 = 25
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
}
