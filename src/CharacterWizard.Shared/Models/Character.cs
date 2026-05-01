namespace CharacterWizard.Shared.Models;

public enum GenerationMethod
{
    StandardArray,
    PointBuy,
    Roll,
}

public class AbilityBlock
{
    public int Base { get; set; }
    public int RacialBonus { get; set; }
    public int OtherBonus { get; set; }
    public int Final => Base + RacialBonus + OtherBonus;
}

public class AbilityScores
{
    public AbilityBlock STR { get; set; } = new();
    public AbilityBlock DEX { get; set; } = new();
    public AbilityBlock CON { get; set; } = new();
    public AbilityBlock INT { get; set; } = new();
    public AbilityBlock WIS { get; set; } = new();
    public AbilityBlock CHA { get; set; } = new();
}

public class ClassLevel
{
    public string ClassId { get; set; } = string.Empty;
    public string? SubclassId { get; set; }
    public int Level { get; set; }
}

public class CharacterProficiencies
{
    public List<string> Weapons { get; set; } = [];
    public List<string> Armor { get; set; } = [];
    public List<string> Tools { get; set; } = [];
    public List<string> Languages { get; set; } = [];
}

public class CharacterFeature
{
    public string FeatureId { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string? DisplayOverride { get; set; }
}

public class CharacterSpell
{
    public string SpellId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public bool Prepared { get; set; }
}

public class CharacterEquipmentItem
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class ValidationEntry
{
    public string Severity { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? Path { get; set; }
}

public class Character
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PlayerName { get; set; }
    public string? Campaign { get; set; }
    public int TotalLevel { get; set; }
    public List<ClassLevel> Levels { get; set; } = [];
    public AbilityScores AbilityScores { get; set; } = new();
    public GenerationMethod GenerationMethod { get; set; }
    public string RaceId { get; set; } = string.Empty;
    public string? SubraceId { get; set; }
    public string BackgroundId { get; set; } = string.Empty;
    public CharacterProficiencies Proficiencies { get; set; } = new();
    public Dictionary<string, string> Skills { get; set; } = [];
    public List<CharacterFeature> Features { get; set; } = [];
    public List<CharacterSpell> Spells { get; set; } = [];
    public List<CharacterEquipmentItem> Equipment { get; set; } = [];

    /// <summary>
    /// If true, the player chose to take class starting wealth (rolled gold) instead of class
    /// starting equipment choices. Background starting equipment is separate.
    /// </summary>
    public bool ClassStartingWealthChosen { get; set; } = false;

    /// <summary>
    /// The rolled/assigned starting gold from the class starting wealth alternative.
    /// Only relevant when ClassStartingWealthChosen is true.
    /// </summary>
    public int? ClassStartingGold { get; set; }

    /// <summary>
    /// Records the player's selections for each class equipment choice group.
    /// </summary>
    public List<EquipmentGroupChoice> StartingEquipmentChoices { get; set; } = [];

    public List<ValidationEntry> ValidationReport { get; set; } = [];
}
