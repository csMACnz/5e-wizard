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

    [Fact]
    public void CustomArray_Valid_ReturnsValid()
    {
        var customArray = new[] { 17, 15, 13, 11, 9, 7 };
        var scores = new[] { 7, 9, 11, 13, 15, 17 }; // same values, different order
        var result = StandardArrayValidator.Validate(scores, customArray);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void CustomArray_Invalid_ReturnsError()
    {
        var customArray = new[] { 17, 15, 13, 11, 9, 7 };
        var scores = new[] { 15, 14, 13, 12, 10, 8 }; // SRD values, not matching custom
        var result = StandardArrayValidator.Validate(scores, customArray);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_STDARRAY_INVALID"));
    }

    [Fact]
    public void CustomArray_ErrorMessage_ContainsCustomValues()
    {
        var customArray = new[] { 17, 15, 13, 11, 9, 7 };
        var scores = new[] { 15, 14, 13, 12, 10, 8 };
        var result = StandardArrayValidator.Validate(scores, customArray);

        Assert.False(result.IsValid);
        // Error message should list the custom array values, not the default [15, 14, 13, 12, 10, 8]
        Assert.Contains(result.Errors, e => e.Contains("17") && e.Contains("7"));
    }
}
