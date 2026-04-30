using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class SpellComponents
{
    [JsonPropertyName("verbal")]
    public bool Verbal { get; set; }

    [JsonPropertyName("somatic")]
    public bool Somatic { get; set; }

    [JsonPropertyName("material")]
    public string? Material { get; set; }
}

public class SpellDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("school")]
    public string School { get; set; } = string.Empty;

    [JsonPropertyName("castingTime")]
    public string CastingTime { get; set; } = string.Empty;

    [JsonPropertyName("range")]
    public string Range { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public SpellComponents Components { get; set; } = new();

    [JsonPropertyName("concentration")]
    public bool Concentration { get; set; }

    [JsonPropertyName("ritual")]
    public bool Ritual { get; set; }

    [JsonPropertyName("damageType")]
    public string? DamageType { get; set; }

    [JsonPropertyName("saveType")]
    public string? SaveType { get; set; }

    [JsonPropertyName("classIds")]
    public List<string> ClassIds { get; set; } = [];
}

public class SpellsData
{
    [JsonPropertyName("spells")]
    public List<SpellDefinition> Spells { get; set; } = [];
}
