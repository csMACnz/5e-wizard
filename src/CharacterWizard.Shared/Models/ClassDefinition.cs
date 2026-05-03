using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class SubclassBonusSpell
{
    [JsonPropertyName("spellId")]
    public string SpellId { get; set; } = string.Empty;

    [JsonPropertyName("grantLevel")]
    public int GrantLevel { get; set; }
}

public class SubclassDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("bonusSpells")]
    public List<SubclassBonusSpell>? BonusSpells { get; set; }

    [JsonPropertyName("spellcasting")]
    public SpellcastingInfo? Spellcasting { get; set; }
}

public class SpellcastingInfo
{
    [JsonPropertyName("castingType")]
    public string CastingType { get; set; } = string.Empty;

    [JsonPropertyName("spellcastingAbility")]
    public string SpellcastingAbility { get; set; } = string.Empty;

    [JsonPropertyName("prepareSpells")]
    public bool PrepareSpells { get; set; }

    [JsonPropertyName("spellListId")]
    public string SpellListId { get; set; } = string.Empty;

    [JsonPropertyName("cantripsKnownByLevel")]
    public List<int> CantripsKnownByLevel { get; set; } = [];

    [JsonPropertyName("spellsKnownByLevel")]
    public List<int> SpellsKnownByLevel { get; set; } = [];

    [JsonPropertyName("ritualCasting")]
    public bool RitualCasting { get; set; }
}

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

    [JsonPropertyName("subclassLabel")]
    public string SubclassLabel { get; set; } = string.Empty;

    [JsonPropertyName("subclassOptions")]
    public List<SubclassDefinition> SubclassOptions { get; set; } = [];

    [JsonPropertyName("featuresByLevel")]
    public Dictionary<string, List<string>> FeaturesByLevel { get; set; } = [];

    [JsonPropertyName("startingEquipmentIds")]
    public List<string> StartingEquipmentIds { get; set; } = [];

    [JsonPropertyName("spellcasting")]
    public SpellcastingInfo? Spellcasting { get; set; }
}

public class ClassesData
{
    [JsonPropertyName("classes")]
    public List<ClassDefinition> Classes { get; set; } = [];
}
