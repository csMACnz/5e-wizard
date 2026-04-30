using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class BackgroundDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("skillProficiencies")]
    public List<string> SkillProficiencies { get; set; } = [];
}

public class BackgroundsData
{
    [JsonPropertyName("backgrounds")]
    public List<BackgroundDefinition> Backgrounds { get; set; } = [];
}
