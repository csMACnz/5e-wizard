using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class CharacterSession
{
    /// <summary>
    /// Storage format version. Increment when the shape of a persisted session changes
    /// in a way that is not backward-compatible. The loader rejects sessions whose version
    /// it does not recognise so that stale data is never silently mis-read.
    /// Current supported version: 1.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

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
