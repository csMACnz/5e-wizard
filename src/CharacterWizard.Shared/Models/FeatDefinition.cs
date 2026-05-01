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
}

public class FeatsData
{
    [JsonPropertyName("feats")]
    public List<FeatDefinition> Feats { get; set; } = [];
}
