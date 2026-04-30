using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class SkillChoices
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = [];
}

public class ClassDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("hitDie")]
    public int HitDie { get; set; }

    [JsonPropertyName("savingThrows")]
    public List<string> SavingThrows { get; set; } = [];

    [JsonPropertyName("skillChoices")]
    public SkillChoices SkillChoices { get; set; } = new();

    [JsonPropertyName("multiclassPrereqs")]
    public Dictionary<string, int> MulticlassPrereqs { get; set; } = [];

    [JsonPropertyName("subclassLevel")]
    public int SubclassLevel { get; set; }
}

public class ClassesData
{
    [JsonPropertyName("classes")]
    public List<ClassDefinition> Classes { get; set; } = [];
}
