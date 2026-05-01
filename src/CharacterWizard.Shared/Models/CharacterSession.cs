using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class CharacterSession
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("characterName")]
    public string CharacterName { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lastModifiedAt")]
    public DateTime LastModifiedAt { get; set; }

    [JsonPropertyName("activeStep")]
    public int ActiveStep { get; set; }

    [JsonPropertyName("character")]
    public Character Character { get; set; } = new();
}
