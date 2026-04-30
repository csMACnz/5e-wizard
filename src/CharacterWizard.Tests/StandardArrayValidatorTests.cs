using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

public class StandardArrayValidatorTests
{
    [Fact]
    public void ValidStandardArray_ExactOrder_ReturnsValid()
    {
        var scores = new[] { 15, 14, 13, 12, 10, 8 };
        var result = StandardArrayValidator.Validate(scores);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidStandardArray_AnyOrder_ReturnsValid()
    {
        var scores = new[] { 8, 10, 12, 13, 14, 15 };
        var result = StandardArrayValidator.Validate(scores);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void DuplicateValue_ReturnsError()
    {
        // 15 appears twice, 14 is missing
        var scores = new[] { 15, 15, 13, 12, 10, 8 };
        var result = StandardArrayValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_STDARRAY_INVALID"));
    }

    [Fact]
    public void WrongValue_ReturnsError()
    {
        // 9 instead of 8
        var scores = new[] { 15, 14, 13, 12, 10, 9 };
        var result = StandardArrayValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_STDARRAY_INVALID"));
    }

    [Fact]
    public void WrongCount_TooFew_ReturnsError()
    {
        var scores = new[] { 15, 14, 13, 12, 10 }; // only 5
        var result = StandardArrayValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_STDARRAY_INVALID"));
    }

    [Fact]
    public void WrongCount_TooMany_ReturnsError()
    {
        var scores = new[] { 15, 14, 13, 12, 10, 8, 8 }; // 7 scores
        var result = StandardArrayValidator.Validate(scores);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_STDARRAY_INVALID"));
    }
}
