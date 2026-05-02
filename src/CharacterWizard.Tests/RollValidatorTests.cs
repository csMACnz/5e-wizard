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

    [Fact]
    public void CustomCount_Valid_ReturnsValid()
    {
        // config specifies 7 rolls
        var scores = new[] { 16, 14, 13, 12, 10, 9, 8 };
        var result = RollValidator.Validate(scores, count: 7);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void CustomCount_WrongCount_ReturnsError()
    {
        var scores = new[] { 16, 14, 13, 12, 10, 9 }; // 6 scores but count = 7 required
        var result = RollValidator.Validate(scores, count: 7);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_ROLL_COUNT") && e.Contains("7"));
    }

    [Fact]
    public void CustomCount_ErrorMessage_MentionsConfiguredCount()
    {
        var scores = new[] { 16, 14, 13 }; // 3 scores but count = 4 required
        var result = RollValidator.Validate(scores, count: 4);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("4") && e.Contains("3"));
    }
}
