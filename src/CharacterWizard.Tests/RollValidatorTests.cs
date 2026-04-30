using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

public class RollValidatorTests
{
    [Fact]
    public void ValidRolls_AllInRange_ReturnsValid()
    {
        var scores = new[] { 16, 14, 13, 12, 10, 9 };
        var result = RollValidator.Validate(scores);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidRolls_BoundaryValues_ReturnsValid()
    {
        // Min (3) and max (18) are both valid
        var scores = new[] { 3, 18, 10, 10, 10, 10 };
        var result = RollValidator.Validate(scores);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ScoreBelowMin_ReturnsError()
    {
        var scores = new[] { 2, 14, 13, 12, 10, 9 }; // 2 < 3
        var result = RollValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_ROLL_RANGE"));
    }

    [Fact]
    public void ScoreAboveMax_ReturnsError()
    {
        var scores = new[] { 19, 14, 13, 12, 10, 9 }; // 19 > 18
        var result = RollValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_ROLL_RANGE"));
    }

    [Fact]
    public void WrongCount_TooFew_ReturnsError()
    {
        var scores = new[] { 16, 14, 13, 12, 10 }; // only 5
        var result = RollValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_ROLL_COUNT"));
    }

    [Fact]
    public void WrongCount_TooMany_ReturnsError()
    {
        var scores = new[] { 16, 14, 13, 12, 10, 9, 8 }; // 7 scores
        var result = RollValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_ROLL_COUNT"));
    }
}
