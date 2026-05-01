using System.Text;
using System.Xml;
using System.Xml.Linq;
using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Export;

/// <summary>
/// Exports a <see cref="Character"/> as a FightClub 5e character XML file.
/// </summary>
/// <remarks>
/// The export is best-effort: fields with no direct mapping (e.g. alignment, personality traits)
/// are emitted as empty or default values. HP is calculated using the maximum hit die value on
/// every level (not average), which produces the theoretical maximum HP.
/// </remarks>
public class FightClub5eExporter
{
    // Saving throw proficiency numbers used by FightClub 5e (0–5 = STR–CHA).
    private static readonly Dictionary<string, int> SaveProficiencyNumbers = new()
    {
        ["STR"] = 0,
        ["DEX"] = 1,
        ["CON"] = 2,
        ["INT"] = 3,
        ["WIS"] = 4,
        ["CHA"] = 5,
    };

    // Skill proficiency numbers used by FightClub 5e (100–117).
    private static readonly Dictionary<string, int> SkillProficiencyNumbers = new()
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

    private readonly IReadOnlyList<RaceDefinition> _races;
    private readonly IReadOnlyList<ClassDefinition> _classes;
    private readonly IReadOnlyList<BackgroundDefinition> _backgrounds;
    private readonly IReadOnlyList<SpellDefinition> _spells;
    private readonly IReadOnlyList<EquipmentItemDefinition> _equipment;

    public FightClub5eExporter(
        IReadOnlyList<RaceDefinition> races,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<BackgroundDefinition> backgrounds,
        IReadOnlyList<SpellDefinition> spells,
        IReadOnlyList<EquipmentItemDefinition> equipment)
    {
        _races = races;
        _classes = classes;
        _backgrounds = backgrounds;
        _spells = spells;
        _equipment = equipment;
    }

    /// <summary>Exports the character as a FightClub 5e XML string.</summary>
    public string Export(Character character)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };
        using (var writer = XmlWriter.Create(sb, settings))
        {
            new XElement("pc", new XAttribute("version", "5"), BuildCharacterElement(character)).Save(writer);
        }
        return sb.ToString();
    }

    private XElement BuildCharacterElement(Character c)
    {
        int conMod = Modifier(c.AbilityScores.CON.Final);
        int maxHp = CalculateMaxHp(c, conMod);

        var race = _races.FirstOrDefault(r => r.Id == c.RaceId);
        var subrace = race?.Subraces.FirstOrDefault(s => s.Id == c.SubraceId);
        var bg = _backgrounds.FirstOrDefault(b => b.Id == c.BackgroundId);

        string raceDisplay = subrace?.DisplayName ?? race?.DisplayName ?? string.Empty;
        string bgDisplay = bg?.DisplayName ?? string.Empty;
        int speed = race?.Speed ?? 30;

        // Ability scores as comma-separated string: STR,DEX,CON,INT,WIS,CHA,
        string abilities = $"{c.AbilityScores.STR.Final},{c.AbilityScores.DEX.Final}," +
                           $"{c.AbilityScores.CON.Final},{c.AbilityScores.INT.Final}," +
                           $"{c.AbilityScores.WIS.Final},{c.AbilityScores.CHA.Final},";

        // Split skill proficiencies by source
        var classSkillIds = c.Skills
            .Where(kvp => kvp.Value == "class")
            .Select(kvp => kvp.Key)
            .ToList();
        var bgSkillIds = c.Skills
            .Where(kvp => kvp.Value == "background")
            .Select(kvp => kvp.Key)
            .ToList();

        var elements = new List<object>
        {
            new XElement("name", c.Name),
            new XElement("player", c.PlayerName ?? string.Empty),
            BuildRaceElement(raceDisplay, speed),
        };

        // One <class> element per class entry
        bool isFirstClass = true;
        foreach (var classLevel in c.Levels)
        {
            var cls = _classes.FirstOrDefault(cl => cl.Id == classLevel.ClassId);
            elements.Add(BuildClassElement(cls, classLevel.Level, isFirstClass ? classSkillIds : [], isFirstClass, c.Proficiencies));
            isFirstClass = false;
        }

        // Background
        elements.Add(BuildBackgroundElement(bgDisplay, bgSkillIds));

        // Abilities, HP, XP
        elements.Add(new XElement("abilities", abilities));
        elements.Add(new XElement("hpMax", maxHp));
        elements.Add(new XElement("hpCurrent", maxHp));
        elements.Add(new XElement("xp", 0));

        // Equipment
        foreach (var item in c.Equipment)
        {
            var itemDef = _equipment.FirstOrDefault(e => e.Id == item.ItemId);
            string itemName = itemDef?.DisplayName ?? string.Empty;
            elements.Add(new XElement("item",
                new XElement("name", itemName),
                new XElement("quantity", item.Quantity)));
        }

        // Spells
        foreach (var cs in c.Spells)
        {
            var spellDef = _spells.FirstOrDefault(s => s.Id == cs.SpellId);
            if (spellDef == null) continue;
            string components = BuildComponentsString(spellDef.Components);
            elements.Add(new XElement("spell",
                new XElement("name", spellDef.DisplayName),
                new XElement("level", spellDef.Level),
                new XElement("school", spellDef.School),
                new XElement("time", spellDef.CastingTime),
                new XElement("range", spellDef.Range),
                new XElement("duration", spellDef.Duration),
                new XElement("components", components),
                new XElement("text", spellDef.Description)));
        }

        // Features as <feat> elements (FC5e naming)
        foreach (var feature in c.Features)
        {
            elements.Add(new XElement("feat",
                new XElement("name", feature.DisplayOverride ?? feature.FeatureId),
                new XElement("text", feature.SourceId)));
        }

        return new XElement("character", elements);
    }

    private static XElement BuildRaceElement(string raceName, int speed)
    {
        return new XElement("race",
            new XElement("name", raceName),
            new XElement("speed", speed));
    }

    private XElement BuildClassElement(
        ClassDefinition? cls,
        int level,
        IList<string> classSkillIds,
        bool isFirstClass,
        CharacterProficiencies proficiencies)
    {
        var classElements = new List<object>
        {
            new XElement("name", cls?.DisplayName ?? string.Empty),
            new XElement("level", level),
            new XElement("hd", cls?.HitDie ?? 8),
        };

        // Armor, weapon, and tool proficiency text on the first class only
        if (isFirstClass)
        {
            classElements.Add(new XElement("armor", string.Join(", ", proficiencies.Armor)));
            classElements.Add(new XElement("weapons", string.Join(", ", proficiencies.Weapons)));
            classElements.Add(new XElement("tools", string.Join(", ", proficiencies.Tools)));
        }

        // Saving throw proficiencies as numeric IDs (0–5)
        if (cls != null)
        {
            foreach (var save in cls.SavingThrows)
            {
                if (SaveProficiencyNumbers.TryGetValue(save, out var num))
                    classElements.Add(new XElement("proficiency", num));
            }
        }

        // Class skill proficiencies as numeric IDs (100–117), first class only
        foreach (var skillId in classSkillIds)
        {
            if (SkillProficiencyNumbers.TryGetValue(skillId, out var num))
                classElements.Add(new XElement("proficiency", num));
        }

        return new XElement("class", classElements);
    }

    private static XElement BuildBackgroundElement(string bgName, IList<string> bgSkillIds)
    {
        var bgElements = new List<object>
        {
            new XElement("name", bgName),
            new XElement("align", string.Empty),
        };

        // Background skill proficiencies as numeric IDs (100–117)
        foreach (var skillId in bgSkillIds)
        {
            if (SkillProficiencyNumbers.TryGetValue(skillId, out var num))
                bgElements.Add(new XElement("proficiency", num));
        }

        return new XElement("background", bgElements);
    }

    /// <summary>
    /// Calculates max HP using the maximum hit die value on every level (no averaging).
    /// Per-level contribution: max(1, hitDie + CON modifier).
    /// </summary>
    private int CalculateMaxHp(Character c, int conMod)
    {
        int total = 0;
        foreach (var classLevel in c.Levels)
        {
            var cls = _classes.FirstOrDefault(cl => cl.Id == classLevel.ClassId);
            int hitDie = cls?.HitDie ?? 8;
            int perLevel = Math.Max(1, hitDie + conMod);
            total += classLevel.Level * perLevel;
        }
        return total;
    }

    private static int Modifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    private static string BuildComponentsString(SpellComponents components)
    {
        var parts = new List<string>();
        if (components.Verbal) parts.Add("V");
        if (components.Somatic) parts.Add("S");
        if (components.Material != null) parts.Add($"M ({components.Material})");
        return string.Join(", ", parts);
    }
}
