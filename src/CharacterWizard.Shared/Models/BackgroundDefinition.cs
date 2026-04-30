using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class BackgroundDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("featureId")]
    public string FeatureId { get; set; } = string.Empty;

    [JsonPropertyName("skillProficiencies")]
    public List<string> SkillProficiencies { get; set; } = [];

    [JsonPropertyName("startingEquipmentIds")]
    public List<string> StartingEquipmentIds { get; set; } = [];

    [JsonPropertyName("startingGold")]
    public int StartingGold { get; set; }
}

public class BackgroundsData
{
    [JsonPropertyName("backgrounds")]
    public List<BackgroundDefinition> Backgrounds { get; set; } = [];
}
