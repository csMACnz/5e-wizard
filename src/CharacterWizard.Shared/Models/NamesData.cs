using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class NamesData
{
    [JsonPropertyName("full")]
    public List<string> Full { get; set; } = [];

    [JsonPropertyName("given")]
    public List<string> Given { get; set; } = [];

    [JsonPropertyName("surname")]
    public List<string> Surname { get; set; } = [];
}
