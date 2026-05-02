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

public class AsiChoice
{
    /// <summary>The class that grants this ASI opportunity.</summary>
    public string ClassId { get; set; } = string.Empty;

    /// <summary>The class level at which this ASI fires.</summary>
    public int ClassLevel { get; set; }

    /// <summary>
    /// Tracks the chosen ASI mode: "plus2", "split", "feat", or null (not yet chosen).
    /// Drives the UI radio selection and determines how AbilityOne/AbilityTwo/FeatId are interpreted.
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// When non-null, the player chose to take a general feat instead of an ability bump.
    /// When null, the player chose an ability score improvement.
    /// </summary>
    public string? FeatId { get; set; }

    /// <summary>
    /// For ability-bump choices: the ability to increase by +2 (if Mode is "plus2")
    /// or by +1 (if Mode is "split"). E.g. "STR".
    /// </summary>
    public string? AbilityOne { get; set; }

    /// <summary>
    /// For the +1/+1 split (Mode = "split"): the second ability to increase by +1.
    /// Null when no second ability has been picked yet.
    /// </summary>
    public string? AbilityTwo { get; set; }
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

/// <summary>
/// Records the player's HP die-roll value for a single class level.
/// For level 1 the value is always the class's hit die maximum and the method is "average".
/// For levels 2+ the method is either "average" (floor(hitDie/2)+1) or "manual" (player-entered).
/// The CON modifier is excluded from this value; max HP = sum(DieRollValue) + CON_mod * totalLevel.
/// </summary>
public class HitPointEntry
{
    /// <summary>The class-level number this entry applies to (1-based within the class).</summary>
    public int ClassLevel { get; set; }

    /// <summary>The class this level belongs to.</summary>
    public string ClassId { get; set; } = string.Empty;

    /// <summary>How the HP was assigned: "average" or "manual".</summary>
    public string Method { get; set; } = "average";

    /// <summary>
    /// The die-roll value chosen for this level, in the range [1, hitDie].
    /// Does not include the CON modifier.
    /// </summary>
    public int DieRollValue { get; set; }
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
    /// The player's ASI/feat choices, one per feat:asi opportunity that is active at the
    /// character's current level. When the level is lowered and then re-raised in the same
    /// session, choices are restored from in-memory state; they are stripped from exports
    /// and session saves when the level that grants them is no longer active.
    /// </summary>
    public List<AsiChoice> AsiChoices { get; set; } = [];

    /// <summary>
    /// Per-level hit point entries recording how HP was assigned at each class level.
    /// Maximum HP = sum(DieRollValue across all entries) + (CON modifier * total level),
    /// with each level's contribution floored at 1 after CON is applied.
    /// </summary>
    public List<HitPointEntry> HitPointEntries { get; set; } = [];

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
