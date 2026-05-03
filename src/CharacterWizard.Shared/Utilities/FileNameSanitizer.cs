using System.Text.RegularExpressions;

namespace CharacterWizard.Shared.Utilities;

/// <summary>
/// Provides helpers for building safe download filenames from character data.
/// </summary>
public static partial class FileNameSanitizer
{
    /// <summary>Fallback name used when the character name is blank after sanitization.</summary>
    public const string FallbackName = "character";

    [GeneratedRegex(@"[^\w\s\-]")]
    private static partial Regex InvalidCharsPattern();

    [GeneratedRegex(@"[\s\-]+")]
    private static partial Regex WhitespaceOrDashRunPattern();

    [GeneratedRegex(@"^[\-]+|[\-]+$")]
    private static partial Regex LeadingTrailingDashPattern();

    /// <summary>
    /// Returns a safe filename of the form <c>{name}-level{level}.{extension}</c>.
    /// Invalid filename characters are removed; runs of whitespace/dashes are collapsed
    /// to a single dash. A blank or all-invalid name falls back to <see cref="FallbackName"/>.
    /// </summary>
    /// <param name="name">Raw character name (may be null, empty, or contain invalid characters).</param>
    /// <param name="level">Total character level to embed in the filename.</param>
    /// <param name="extension">File extension without leading dot, e.g. <c>"json"</c>.</param>
    public static string SanitizeCharacterFileName(string? name, int level, string extension)
    {
        var sanitized = name ?? string.Empty;

        // Remove characters that are not word chars, whitespace, or dashes.
        sanitized = InvalidCharsPattern().Replace(sanitized, "-");

        // Collapse runs of whitespace/dashes into a single dash.
        sanitized = WhitespaceOrDashRunPattern().Replace(sanitized, "-");

        // Strip leading/trailing dashes.
        sanitized = LeadingTrailingDashPattern().Replace(sanitized, string.Empty);

        // Fallback when name is blank or reduced to nothing.
        if (string.IsNullOrEmpty(sanitized))
        {
            sanitized = FallbackName;
        }

        return $"{sanitized}-level{level}.{extension}";
    }
}
