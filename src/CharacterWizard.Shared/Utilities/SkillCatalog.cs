namespace CharacterWizard.Shared.Utilities;

/// <summary>
/// Canonical skill IDs, labels, governing abilities, and FightClub 5e numeric mappings.
/// </summary>
public static class SkillCatalog
{
    public static readonly IReadOnlyDictionary<string, string> SkillNames = new Dictionary<string, string>
    {
        ["skill:acrobatics"] = "Acrobatics",
        ["skill:animal-handling"] = "Animal Handling",
        ["skill:arcana"] = "Arcana",
        ["skill:athletics"] = "Athletics",
        ["skill:deception"] = "Deception",
        ["skill:history"] = "History",
        ["skill:insight"] = "Insight",
        ["skill:intimidation"] = "Intimidation",
        ["skill:investigation"] = "Investigation",
        ["skill:medicine"] = "Medicine",
        ["skill:nature"] = "Nature",
        ["skill:perception"] = "Perception",
        ["skill:performance"] = "Performance",
        ["skill:persuasion"] = "Persuasion",
        ["skill:religion"] = "Religion",
        ["skill:sleight-of-hand"] = "Sleight of Hand",
        ["skill:stealth"] = "Stealth",
        ["skill:survival"] = "Survival",
    };

    public static readonly IReadOnlyList<string> AllSkillIds =
    [
        "skill:acrobatics", "skill:animal-handling", "skill:arcana", "skill:athletics",
        "skill:deception", "skill:history", "skill:insight", "skill:intimidation",
        "skill:investigation", "skill:medicine", "skill:nature", "skill:perception",
        "skill:performance", "skill:persuasion", "skill:religion", "skill:sleight-of-hand",
        "skill:stealth", "skill:survival",
    ];

    public static readonly IReadOnlyList<(string SkillId, string Ability)> SkillAbilityMap =
    [
        ("skill:acrobatics", "DEX"),
        ("skill:animal-handling", "WIS"),
        ("skill:arcana", "INT"),
        ("skill:athletics", "STR"),
        ("skill:deception", "CHA"),
        ("skill:history", "INT"),
        ("skill:insight", "WIS"),
        ("skill:intimidation", "CHA"),
        ("skill:investigation", "INT"),
        ("skill:medicine", "WIS"),
        ("skill:nature", "INT"),
        ("skill:perception", "WIS"),
        ("skill:performance", "CHA"),
        ("skill:persuasion", "CHA"),
        ("skill:religion", "INT"),
        ("skill:sleight-of-hand", "DEX"),
        ("skill:stealth", "DEX"),
        ("skill:survival", "WIS"),
    ];

    public static readonly IReadOnlyDictionary<string, int> FightClubNumberBySkillId = new Dictionary<string, int>
    {
        ["skill:acrobatics"] = 100,
        ["skill:animal-handling"] = 101,
        ["skill:arcana"] = 102,
        ["skill:athletics"] = 103,
        ["skill:deception"] = 104,
        ["skill:history"] = 105,
        ["skill:insight"] = 106,
        ["skill:intimidation"] = 107,
        ["skill:investigation"] = 108,
        ["skill:medicine"] = 109,
        ["skill:nature"] = 110,
        ["skill:perception"] = 111,
        ["skill:performance"] = 112,
        ["skill:persuasion"] = 113,
        ["skill:religion"] = 114,
        ["skill:sleight-of-hand"] = 115,
        ["skill:stealth"] = 116,
        ["skill:survival"] = 117,
    };

    public static readonly IReadOnlyDictionary<int, string> FightClubSkillIdByNumber =
        FightClubNumberBySkillId.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static string SkillLabel(string id) =>
        SkillNames.TryGetValue(id, out string? name) ? name : id;
}
