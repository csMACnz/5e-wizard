using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class FeatDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("srdReference")]
    public string? SrdReference { get; set; }

    /// <summary>
    /// Categorises the feat for filtering and display.
    /// One of: "asi" (sentinel), "class" (auto-granted class feature),
    /// "background" (auto-granted background feature), "general" (player-selectable ASI alternative).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class FeatsData
{
    [JsonPropertyName("feats")]
    public List<FeatDefinition> Feats { get; set; } = [];
}
