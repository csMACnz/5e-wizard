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

    [JsonPropertyName("skillChoices")]
    public SkillChoices SkillChoices { get; set; } = new();

    [JsonPropertyName("multiclassPrereqs")]
    public Dictionary<string, int> MulticlassPrereqs { get; set; } = [];
}

public class ClassesData
{
    [JsonPropertyName("classes")]
    public List<ClassDefinition> Classes { get; set; } = [];
}
