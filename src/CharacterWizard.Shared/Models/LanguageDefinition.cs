using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class LanguageDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("script")]
    public string? Script { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}

public class LanguagesData
{
    [JsonPropertyName("languages")]
    public List<LanguageDefinition> Languages { get; set; } = [];
}
