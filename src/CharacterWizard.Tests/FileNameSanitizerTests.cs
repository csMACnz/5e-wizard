using CharacterWizard.Shared.Utilities;

namespace CharacterWizard.Tests;

/// <summary>
/// Tests for <see cref="FileNameSanitizer.SanitizeCharacterFileName"/>.
/// </summary>
public class FileNameSanitizerTests
{
    // ── Blank / empty name fallback ───────────────────────────────────────

    [Fact]
    public void NullName_UsesFallback()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName(null, 5, "json");
        Assert.Equal("character-level5.json", result);
    }

    [Fact]
    public void EmptyName_UsesFallback()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName(string.Empty, 3, "json");
        Assert.Equal("character-level3.json", result);
    }

    [Fact]
    public void WhitespaceName_UsesFallback()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("   ", 2, "xml");
        Assert.Equal("character-level2.xml", result);
    }

    // ── Normal names ──────────────────────────────────────────────────────

    [Fact]
    public void SimpleName_PassesThrough()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("Thorin", 5, "json");
        Assert.Equal("Thorin-level5.json", result);
    }

    [Fact]
    public void NameWithSpaces_SpacesCollapsedToDash()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("Aric Stonehammer", 3, "json");
        Assert.Equal("Aric-Stonehammer-level3.json", result);
    }

    // ── Invalid characters ────────────────────────────────────────────────

    [Fact]
    public void NameWithColon_ColonRemovedAndDashesCollapsed()
    {
        // "Scar:Face" → invalid char replaced with "-" → "Scar-Face"
        var result = FileNameSanitizer.SanitizeCharacterFileName("Scar:Face", 4, "json");
        Assert.Equal("Scar-Face-level4.json", result);
    }

    [Fact]
    public void NameWithSlash_SlashReplaced()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("Al/ice", 1, "xml");
        Assert.Equal("Al-ice-level1.xml", result);
    }

    [Fact]
    public void NameWithBackslash_BackslashReplaced()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName(@"Al\ice", 1, "xml");
        Assert.Equal("Al-ice-level1.xml", result);
    }

    [Fact]
    public void NameWithAngleBrackets_Replaced()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("The<Great>One", 10, "json");
        Assert.Equal("The-Great-One-level10.json", result);
    }

    [Fact]
    public void NameWithQuote_Replaced()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("D\"Artagnan", 6, "json");
        Assert.Equal("D-Artagnan-level6.json", result);
    }

    [Fact]
    public void NameWithPipeAndStar_Replaced()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("Dark|Star*", 7, "json");
        Assert.Equal("Dark-Star-level7.json", result);
    }

    // ── Edge cases ────────────────────────────────────────────────────────

    [Fact]
    public void NameWithLeadingAndTrailingSpaces_Trimmed()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("  Elara  ", 8, "json");
        Assert.Equal("Elara-level8.json", result);
    }

    [Fact]
    public void NameWithConsecutiveSpecialChars_CollapsedToSingleDash()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("Mo:::rag", 2, "json");
        Assert.Equal("Mo-rag-level2.json", result);
    }

    [Fact]
    public void NameAllInvalidChars_UsesFallback()
    {
        // Name consisting only of invalid chars that become dashes, then get stripped
        var result = FileNameSanitizer.SanitizeCharacterFileName(":::", 1, "json");
        Assert.Equal("character-level1.json", result);
    }

    [Fact]
    public void LevelZero_StillProducesValidFilename()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("Zara", 0, "json");
        Assert.Equal("Zara-level0.json", result);
    }

    [Fact]
    public void XmlExtension_ProducedCorrectly()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("Thorin", 5, "xml");
        Assert.Equal("Thorin-level5.xml", result);
    }

    [Fact]
    public void NameWithExistingDash_Preserved()
    {
        var result = FileNameSanitizer.SanitizeCharacterFileName("Half-Orc", 4, "json");
        Assert.Equal("Half-Orc-level4.json", result);
    }
}
