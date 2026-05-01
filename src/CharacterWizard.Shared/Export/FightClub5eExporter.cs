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
    private static readonly Dictionary<string, string> AbilityFullNames = new()
    {
        ["STR"] = "Strength",
        ["DEX"] = "Dexterity",
        ["CON"] = "Constitution",
        ["INT"] = "Intelligence",
        ["WIS"] = "Wisdom",
        ["CHA"] = "Charisma",
    };

    private static readonly Dictionary<string, string> SkillDisplayNames = new()
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
            new XElement("document", BuildCharacterElement(character)).Save(writer);
        }
        return sb.ToString();
    }

    private XElement BuildCharacterElement(Character c)
    {
        int totalLevel = c.TotalLevel > 0 ? c.TotalLevel : c.Levels.Sum(l => l.Level);
        int conMod = Modifier(c.AbilityScores.CON.Final);
        int dexMod = Modifier(c.AbilityScores.DEX.Final);
        int wisMod = Modifier(c.AbilityScores.WIS.Final);
        int profBonus = ProficiencyBonus(totalLevel);
        int maxHp = CalculateMaxHp(c, conMod);

        bool hasPerception = c.Skills.ContainsKey("skill:perception");
        int passivePerception = 10 + wisMod + (hasPerception ? profBonus : 0);

        var race = _races.FirstOrDefault(r => r.Id == c.RaceId);
        var subrace = race?.Subraces.FirstOrDefault(s => s.Id == c.SubraceId);
        var bg = _backgrounds.FirstOrDefault(b => b.Id == c.BackgroundId);

        string raceDisplay = subrace?.DisplayName ?? race?.DisplayName ?? string.Empty;
        string bgDisplay = bg?.DisplayName ?? string.Empty;
        int speed = race?.Speed ?? 30;

        string classDisplay = c.Levels.Count > 0
            ? string.Join("/", c.Levels.Select(l =>
            {
                var cls = _classes.FirstOrDefault(cl => cl.Id == l.ClassId);
                return $"{cls?.DisplayName ?? string.Empty} {l.Level}".Trim();
            }))
            : string.Empty;

        // Collect unique saving throw proficiencies across all class entries
        var saveProficiencies = c.Levels
            .SelectMany(l =>
            {
                var cls = _classes.FirstOrDefault(cl => cl.Id == l.ClassId);
                return cls?.SavingThrows ?? [];
            })
            .Distinct()
            .Select(ab => AbilityFullNames.TryGetValue(ab, out var name) ? name : ab)
            .ToList();

        var elements = new List<object>
        {
            new XElement("name", c.Name),
            new XElement("player", c.PlayerName ?? string.Empty),
            new XElement("alignment", string.Empty),
            new XElement("background", bgDisplay),
            new XElement("race", raceDisplay),
            new XElement("class", classDisplay),
            new XElement("level", totalLevel),
            new XElement("exp", 0),
            new XElement("str", c.AbilityScores.STR.Final),
            new XElement("dex", c.AbilityScores.DEX.Final),
            new XElement("con", c.AbilityScores.CON.Final),
            new XElement("int", c.AbilityScores.INT.Final),
            new XElement("wis", c.AbilityScores.WIS.Final),
            new XElement("cha", c.AbilityScores.CHA.Final),
            new XElement("hp",
                new XAttribute("max", maxHp),
                new XAttribute("current", maxHp),
                new XAttribute("temp", 0)),
            new XElement("speed", speed),
            new XElement("initiative", dexMod),
            new XElement("proficiencybonus", profBonus),
            new XElement("passiveperception", passivePerception),
            new XElement("inspiration", 0),
            new XElement("ac", 10 + dexMod),
        };

        // Saving throws
        elements.AddRange(saveProficiencies.Select(s => (object)new XElement("save", s)));

        // Skill proficiencies
        foreach (var (skillId, _) in c.Skills)
        {
            string skillName = SkillDisplayNames.TryGetValue(skillId, out var sn) ? sn : skillId;
            elements.Add(new XElement("skillprof", new XAttribute("name", skillName)));
        }

        // Armor, weapon, tool, and language proficiencies
        foreach (var p in c.Proficiencies.Armor) elements.Add(new XElement("proficiency", p));
        foreach (var p in c.Proficiencies.Weapons) elements.Add(new XElement("proficiency", p));
        foreach (var p in c.Proficiencies.Tools) elements.Add(new XElement("proficiency", p));
        foreach (var p in c.Proficiencies.Languages) elements.Add(new XElement("proficiency", p));

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

        // Features (best-effort: populated from Character.Features when present)
        foreach (var feature in c.Features)
        {
            elements.Add(new XElement("feature",
                new XElement("name", feature.DisplayOverride ?? feature.FeatureId),
                new XElement("source", feature.SourceId)));
        }

        return new XElement("character", elements);
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

    private static int ProficiencyBonus(int totalLevel) => Math.Max(0, totalLevel - 1) / 4 + 2;

    private static string BuildComponentsString(SpellComponents components)
    {
        var parts = new List<string>();
        if (components.Verbal) parts.Add("V");
        if (components.Somatic) parts.Add("S");
        if (components.Material != null) parts.Add($"M ({components.Material})");
        return string.Join(", ", parts);
    }
}
