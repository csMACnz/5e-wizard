using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class SubraceDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("abilityBonuses")]
    public Dictionary<string, int> AbilityBonuses { get; set; } = [];

    [JsonPropertyName("traitIds")]
    public List<string> TraitIds { get; set; } = [];
}

public class RaceDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("abilityBonuses")]
    public Dictionary<string, int> AbilityBonuses { get; set; } = [];

    [JsonPropertyName("speed")]
    public int Speed { get; set; } = 30;

    [JsonPropertyName("traitIds")]
    public List<string> TraitIds { get; set; } = [];

    [JsonPropertyName("subraces")]
    public List<SubraceDefinition> Subraces { get; set; } = [];
}

public class RacesData
{
    [JsonPropertyName("races")]
    public List<RaceDefinition> Races { get; set; } = [];
}
