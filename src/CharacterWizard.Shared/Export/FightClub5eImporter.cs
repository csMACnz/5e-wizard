using System.Xml.Linq;
using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Export;

/// <summary>
/// Imports a FightClub 5e character XML file into a <see cref="Character"/> object.
/// </summary>
/// <remarks>
/// The import is best-effort: fields without a direct mapping in the FC5e XML format
/// (e.g. generation method, ASI choices, starting equipment choices) are left as their
/// default values. Ability scores are imported as base scores only — the racial and other
/// bonus split is not preserved in the FC5e format.
/// </remarks>
public class FightClub5eImporter
{
    // Reverse mapping: proficiency number → skill ID (100–117 in FC5e).
    private static readonly Dictionary<int, string> SkillIdByNumber = new()
    {
        [100] = "skill:acrobatics",
        [101] = "skill:animal-handling",
        [102] = "skill:arcana",
        [103] = "skill:athletics",
        [104] = "skill:deception",
        [105] = "skill:history",
        [106] = "skill:insight",
        [107] = "skill:intimidation",
        [108] = "skill:investigation",
        [109] = "skill:medicine",
        [110] = "skill:nature",
        [111] = "skill:perception",
        [112] = "skill:performance",
        [113] = "skill:persuasion",
        [114] = "skill:religion",
        [115] = "skill:sleight-of-hand",
        [116] = "skill:stealth",
        [117] = "skill:survival",
    };

    private readonly IReadOnlyList<RaceDefinition> _races;
    private readonly IReadOnlyList<ClassDefinition> _classes;
    private readonly IReadOnlyList<BackgroundDefinition> _backgrounds;
    private readonly IReadOnlyList<SpellDefinition> _spells;
    private readonly IReadOnlyList<EquipmentItemDefinition> _equipment;
    private readonly IReadOnlyList<FeatDefinition> _feats;

    public FightClub5eImporter(
        IReadOnlyList<RaceDefinition> races,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<BackgroundDefinition> backgrounds,
        IReadOnlyList<SpellDefinition> spells,
        IReadOnlyList<EquipmentItemDefinition> equipment,
        IReadOnlyList<FeatDefinition>? feats = null)
    {
        _races = races;
        _classes = classes;
        _backgrounds = backgrounds;
        _spells = spells;
        _equipment = equipment;
        _feats = feats ?? [];
    }

    /// <summary>
    /// Parses a FightClub 5e XML string and returns the represented <see cref="Character"/>.
    /// </summary>
    public Character Import(string xml)
    {
        var doc = XDocument.Parse(xml);
        var charEl = doc.Root!.Element("character")!;

        var result = new Character();

        // Name and player
        result.Name = charEl.Element("name")?.Value ?? string.Empty;
        var player = charEl.Element("player")?.Value;
        result.PlayerName = string.IsNullOrEmpty(player) ? null : player;

        // Race — the FC5e exporter writes the subrace display name when a subrace is selected,
        // so we try subraces first and fall back to the parent race name.
        var raceName = charEl.Element("race")?.Element("name")?.Value ?? string.Empty;
        ResolveRace(raceName, result);

        // Classes
        bool isFirstClass = true;
        foreach (var classEl in charEl.Elements("class"))
        {
            var className = classEl.Element("name")?.Value ?? string.Empty;
            var level = int.TryParse(classEl.Element("level")?.Value, out var lvl) ? lvl : 0;
            var cls = _classes.FirstOrDefault(c => c.DisplayName == className);

            result.Levels.Add(new ClassLevel
            {
                ClassId = cls?.Id ?? string.Empty,
                Level = level,
            });

            // The exporter places armor/weapons/tools and class skill proficiencies only on
            // the first class element; subsequent class elements only carry saving-throw
            // proficiency numbers (0–5), which we ignore here as they are inferred from the
            // class definition.
            if (isFirstClass)
            {
                var armorText = classEl.Element("armor")?.Value ?? string.Empty;
                if (!string.IsNullOrEmpty(armorText))
                    result.Proficiencies.Armor = [.. armorText.Split(", ", StringSplitOptions.RemoveEmptyEntries)];

                var weaponsText = classEl.Element("weapons")?.Value ?? string.Empty;
                if (!string.IsNullOrEmpty(weaponsText))
                    result.Proficiencies.Weapons = [.. weaponsText.Split(", ", StringSplitOptions.RemoveEmptyEntries)];

                var toolsText = classEl.Element("tools")?.Value ?? string.Empty;
                if (!string.IsNullOrEmpty(toolsText))
                    result.Proficiencies.Tools = [.. toolsText.Split(", ", StringSplitOptions.RemoveEmptyEntries)];

                foreach (var profEl in classEl.Elements("proficiency"))
                {
                    if (int.TryParse(profEl.Value, out var num) && num >= 100
                        && SkillIdByNumber.TryGetValue(num, out var skillId))
                    {
                        result.Skills[skillId] = "class";
                    }
                }

                isFirstClass = false;
            }
        }

        result.TotalLevel = result.Levels.Sum(l => l.Level);

        // Background
        var bgName = charEl.Element("background")?.Element("name")?.Value ?? string.Empty;
        var bg = _backgrounds.FirstOrDefault(b => b.DisplayName == bgName);
        result.BackgroundId = bg?.Id ?? string.Empty;

        // Background skill proficiencies (100–117 inside <background>)
        var bgEl = charEl.Element("background");
        if (bgEl != null)
        {
            foreach (var profEl in bgEl.Elements("proficiency"))
            {
                if (int.TryParse(profEl.Value, out var num) && num >= 100
                    && SkillIdByNumber.TryGetValue(num, out var skillId))
                {
                    result.Skills[skillId] = "background";
                }
            }
        }

        // Ability scores — only Final values are stored in the FC5e format, so they are
        // imported as Base scores with no racial or other bonus split.
        var abilitiesText = charEl.Element("abilities")?.Value ?? string.Empty;
        var scores = abilitiesText
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var v) ? v : 10)
            .ToArray();
        if (scores.Length >= 6)
        {
            result.AbilityScores.STR.Base = scores[0];
            result.AbilityScores.DEX.Base = scores[1];
            result.AbilityScores.CON.Base = scores[2];
            result.AbilityScores.INT.Base = scores[3];
            result.AbilityScores.WIS.Base = scores[4];
            result.AbilityScores.CHA.Base = scores[5];
        }

        // Equipment
        foreach (var itemEl in charEl.Elements("item"))
        {
            var itemName = itemEl.Element("name")?.Value ?? string.Empty;
            var quantity = int.TryParse(itemEl.Element("quantity")?.Value, out var q) ? q : 1;
            var itemDef = _equipment.FirstOrDefault(e => e.DisplayName == itemName);
            if (itemDef != null)
            {
                result.Equipment.Add(new CharacterEquipmentItem { ItemId = itemDef.Id, Quantity = quantity });
            }
        }

        // Spells
        foreach (var spellEl in charEl.Elements("spell"))
        {
            var spellName = spellEl.Element("name")?.Value ?? string.Empty;
            var prepared = spellEl.Element("prepared")?.Value == "YES";
            var classesValue = spellEl.Element("classes")?.Value ?? string.Empty;
            var spellDef = _spells.FirstOrDefault(s => s.DisplayName == spellName);
            var spellClass = _classes.FirstOrDefault(c => c.DisplayName == classesValue);
            if (spellDef != null)
            {
                result.Spells.Add(new CharacterSpell
                {
                    SpellId = spellDef.Id,
                    ClassId = spellClass?.Id ?? string.Empty,
                    Prepared = prepared,
                });
            }
        }

        // Features — stored as <feat> elements in FC5e XML.
        // The exporter writes: <name> = DisplayOverride ?? feat.DisplayName ?? feat.FeatureId
        //                      <text> = SourceId
        // We reverse by looking up the feat by DisplayName.  When no match is found the
        // raw name is used as the FeatureId so that re-export produces the same XML.
        foreach (var featEl in charEl.Elements("feat"))
        {
            var featName = featEl.Element("name")?.Value ?? string.Empty;
            var sourceId = featEl.Element("text")?.Value ?? string.Empty;
            var featDef = _feats.FirstOrDefault(f => f.DisplayName == featName);
            result.Features.Add(new CharacterFeature
            {
                FeatureId = featDef?.Id ?? featName,
                SourceId = sourceId,
            });
        }

        return result;
    }

    private void ResolveRace(string raceName, Character result)
    {
        // Try matching a subrace display name first (the exporter uses the subrace display name
        // when a subrace is selected).
        foreach (var race in _races)
        {
            foreach (var subrace in race.Subraces)
            {
                if (string.Equals(subrace.DisplayName, raceName, StringComparison.OrdinalIgnoreCase))
                {
                    result.RaceId = race.Id;
                    result.SubraceId = subrace.Id;
                    return;
                }
            }
        }

        // Fall back to matching the parent race display name.
        var matchedRace = _races.FirstOrDefault(
            r => string.Equals(r.DisplayName, raceName, StringComparison.OrdinalIgnoreCase));
        if (matchedRace != null)
        {
            result.RaceId = matchedRace.Id;
        }
    }
}
