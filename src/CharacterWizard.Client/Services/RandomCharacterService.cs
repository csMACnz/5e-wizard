using CharacterWizard.Shared.Models;

namespace CharacterWizard.Client.Services;

public sealed class RandomCharacterService(IDataService dataService)
{
    private static readonly string[] _characterNameOptions =
    [
        "Aric Stonehammer", "Lyra Swiftwind", "Gareth Ironwood", "Senna Brightflame",
        "Dorin Ashgrove", "Mira Dawnwhisper", "Talon Greymark", "Elara Frostweave",
        "Bram Coldstone", "Zara Nightfall", "Owen Steelforge", "Nara Moonveil",
        "Caden Embercroft", "Isra Thistledown", "Finn Blackthorn", "Selene Goldleaf",
        "Rowan Whitepeak", "Vex Shadowmere", "Cleo Starhelm", "Drake Thornfield",
    ];

    private static readonly List<string> _allSkillIds =
    [
        "skill:acrobatics", "skill:animal-handling", "skill:arcana", "skill:athletics",
        "skill:deception", "skill:history", "skill:insight", "skill:intimidation",
        "skill:investigation", "skill:medicine", "skill:nature", "skill:perception",
        "skill:performance", "skill:persuasion", "skill:religion", "skill:sleight-of-hand",
        "skill:stealth", "skill:survival",
    ];

    public async Task<Character> GenerateAsync()
    {
        var abilitiesConfig = await dataService.GetAbilitiesConfigAsync();
        var races = await dataService.GetRacesAsync();
        var classes = await dataService.GetClassesAsync();
        var backgrounds = await dataService.GetBackgroundsAsync();
        var spells = await dataService.GetSpellsAsync();
        var equipment = await dataService.GetEquipmentAsync();
        var classStartingEquipment = await dataService.GetClassStartingEquipmentAsync();
        var fullNames = await dataService.GetFullNamesAsync();

        var namePool = fullNames.Count > 0 ? fullNames : _characterNameOptions;

        var c = new Character();
        var rng = Random.Shared;

        // Step 1 — Meta
        c.Name = namePool[rng.Next(namePool.Count)];
        c.GenerationMethod = GenerationMethod.Roll;

        // Step 2 — Ability Scores (method from config: 4d6 drop lowest)
        string[] abilityNames = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];
        int rollCount = abilitiesConfig.Roll.Count > 0 ? abilitiesConfig.Roll.Count : abilityNames.Length;
        var rolledScores = Enumerable.Range(0, rollCount).Select(_ => RollAbilityScore()).ToArray();
        for (int i = 0; i < Math.Min(rollCount, abilityNames.Length); i++)
            GetAbilityScoreBlock(c, abilityNames[i]).Base = rolledScores[i];

        // Step 3 — Race
        var race = races[rng.Next(races.Count)];
        c.RaceId = race.Id;
        if (race.Subraces.Count > 0)
            c.SubraceId = race.Subraces[rng.Next(race.Subraces.Count)].Id;

        var raceBonuses = GetCombinedRacialBonuses(race, c.SubraceId ?? string.Empty);
        c.AbilityScores.STR.RacialBonus = raceBonuses.GetValueOrDefault("STR", 0);
        c.AbilityScores.DEX.RacialBonus = raceBonuses.GetValueOrDefault("DEX", 0);
        c.AbilityScores.CON.RacialBonus = raceBonuses.GetValueOrDefault("CON", 0);
        c.AbilityScores.INT.RacialBonus = raceBonuses.GetValueOrDefault("INT", 0);
        c.AbilityScores.WIS.RacialBonus = raceBonuses.GetValueOrDefault("WIS", 0);
        c.AbilityScores.CHA.RacialBonus = raceBonuses.GetValueOrDefault("CHA", 0);

        // Step 4 — Class (level 1 character)
        var cls = classes[rng.Next(classes.Count)];
        const int level = 1;
        string? subclassId = null;
        if (cls.SubclassOptions.Count > 0 && level >= cls.SubclassLevel)
            subclassId = cls.SubclassOptions[rng.Next(cls.SubclassOptions.Count)].Id;
        c.Levels.Add(new ClassLevel { ClassId = cls.Id, Level = level, SubclassId = subclassId });
        c.TotalLevel = level;

        // Step 5 — Background + Skill Proficiencies
        var bg = backgrounds[rng.Next(backgrounds.Count)];
        c.BackgroundId = bg.Id;
        foreach (var sk in bg.SkillProficiencies)
            c.Skills[sk] = "background";

        var classSkillOptions = cls.SkillChoices.Options.Contains("skill:any")
            ? _allSkillIds
            : cls.SkillChoices.Options;
        var availableClassSkills = classSkillOptions
            .Where(sk => !c.Skills.ContainsKey(sk))
            .OrderBy(_ => rng.Next())
            .ToList();
        int classSkillCount = Math.Min(cls.SkillChoices.Count, availableClassSkills.Count);
        for (int i = 0; i < classSkillCount; i++)
            c.Skills[availableClassSkills[i]] = "class";

        // Step 6 — Spells (if spellcaster)
        if (cls.Spellcasting != null)
        {
            var sc = cls.Spellcasting;
            int maxCantrips = sc.CantripsKnownByLevel.Count >= level ? sc.CantripsKnownByLevel[level - 1] : 0;
            int maxKnown = sc.SpellsKnownByLevel.Count >= level ? sc.SpellsKnownByLevel[level - 1] : 0;
            var classSpells = spells.Where(s => s.ClassIds.Contains(cls.Id)).ToList();

            foreach (var spell in classSpells.Where(s => s.Level == 0).OrderBy(_ => rng.Next()).Take(maxCantrips))
                c.Spells.Add(new CharacterSpell { SpellId = spell.Id, ClassId = cls.Id, Prepared = false });

            var levelOneSpells = classSpells.Where(s => s.Level == 1).OrderBy(_ => rng.Next()).ToList();
            // For prepare-spell classes (wizard, cleric, druid, paladin), pick a few level 1 spells to prepare
            const int defaultPreparedSpellsAtLevelOne = 3;
            int spellsToPick = sc.PrepareSpells ? Math.Min(defaultPreparedSpellsAtLevelOne, levelOneSpells.Count) : Math.Min(maxKnown, levelOneSpells.Count);
            foreach (var spell in levelOneSpells.Take(spellsToPick))
                c.Spells.Add(new CharacterSpell { SpellId = spell.Id, ClassId = cls.Id, Prepared = true });
        }

        // Step 7 — Equipment (class starting equipment via choice groups)
        var classEquipConfig = classStartingEquipment.FirstOrDefault(e => e.ClassId == cls.Id);
        if (classEquipConfig != null)
        {
            // Fixed items
            foreach (var fixedItem in classEquipConfig.FixedItems)
                c.Equipment.Add(new CharacterEquipmentItem { ItemId = fixedItem.ItemId, Quantity = fixedItem.Quantity });

            // Random choices for each required group
            foreach (var group in classEquipConfig.ChoiceGroups.Where(g => g.Required))
            {
                if (group.Options.Count == 0) continue;
                var chosenOption = group.Options[rng.Next(group.Options.Count)];

                if (chosenOption.PickOne && chosenOption.GrantItems.Count > 0)
                {
                    var picked = chosenOption.GrantItems[rng.Next(chosenOption.GrantItems.Count)];
                    c.StartingEquipmentChoices.Add(new EquipmentGroupChoice
                    {
                        GroupId = group.Id,
                        ChosenOptionId = chosenOption.Id,
                        PickedItemId = picked.ItemId,
                    });
                    c.Equipment.Add(new CharacterEquipmentItem { ItemId = picked.ItemId, Quantity = picked.Quantity });
                }
                else
                {
                    c.StartingEquipmentChoices.Add(new EquipmentGroupChoice
                    {
                        GroupId = group.Id,
                        ChosenOptionId = chosenOption.Id,
                    });
                    foreach (var grant in chosenOption.GrantItems)
                        c.Equipment.Add(new CharacterEquipmentItem { ItemId = grant.ItemId, Quantity = grant.Quantity });
                }
            }
        }
        else
        {
            // Fallback: use legacy flat equipment list
            foreach (var itemId in cls.StartingEquipmentIds)
                c.Equipment.Add(new CharacterEquipmentItem { ItemId = itemId, Quantity = 1 });
        }

        return c;
    }

    private static int RollAbilityScore()
    {
        var rng = Random.Shared;
        int r1 = rng.Next(1, 7);
        int r2 = rng.Next(1, 7);
        int r3 = rng.Next(1, 7);
        int r4 = rng.Next(1, 7);
        return r1 + r2 + r3 + r4 - Math.Min(Math.Min(r1, r2), Math.Min(r3, r4));
    }

    private static AbilityBlock GetAbilityScoreBlock(Character c, string ability) => ability switch
    {
        "STR" => c.AbilityScores.STR,
        "DEX" => c.AbilityScores.DEX,
        "CON" => c.AbilityScores.CON,
        "INT" => c.AbilityScores.INT,
        "WIS" => c.AbilityScores.WIS,
        "CHA" => c.AbilityScores.CHA,
        _ => throw new ArgumentOutOfRangeException(nameof(ability), ability, null),
    };

    private static Dictionary<string, int> GetCombinedRacialBonuses(RaceDefinition race, string subraceId)
    {
        var bonuses = new Dictionary<string, int>(race.AbilityBonuses);
        if (!string.IsNullOrEmpty(subraceId))
        {
            var sub = race.Subraces.FirstOrDefault(s => s.Id == subraceId);
            if (sub != null)
                foreach (var (ab, v) in sub.AbilityBonuses)
                    bonuses[ab] = bonuses.TryGetValue(ab, out int e) ? e + v : v;
        }

        return bonuses;
    }
}
