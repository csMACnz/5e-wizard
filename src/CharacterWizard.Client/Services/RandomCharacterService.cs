using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Utilities;

namespace CharacterWizard.Client.Services;

public sealed class RandomCharacterService(IDataService dataService, IRngFactory rngFactory)
{
    private static readonly string[] _characterNameOptions =
    [
        "Aric Stonehammer", "Lyra Swiftwind", "Gareth Ironwood", "Senna Brightflame",
        "Dorin Ashgrove", "Mira Dawnwhisper", "Talon Greymark", "Elara Frostweave",
        "Bram Coldstone", "Zara Nightfall", "Owen Steelforge", "Nara Moonveil",
        "Caden Embercroft", "Isra Thistledown", "Finn Blackthorn", "Selene Goldleaf",
        "Rowan Whitepeak", "Vex Shadowmere", "Cleo Starhelm", "Drake Thornfield",
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
        var languages = await dataService.GetLanguagesAsync();

        var namePool = fullNames.Count > 0 ? fullNames : _characterNameOptions;

        var c = new Character();
        var rng = rngFactory.Create();

        // Step 1 — Meta
        c.Name = namePool[rng.Next(namePool.Count)];
        c.GenerationMethod = GenerationMethod.Roll;

        // Step 2 — Ability Scores (method from config: 4d6 drop lowest)
        string[] abilityNames = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];
        int rollCount = abilitiesConfig.Roll.Count > 0 ? abilitiesConfig.Roll.Count : abilityNames.Length;
        var rolledScores = Enumerable.Range(0, rollCount).Select(_ => DiceHelper.RollAbilityScore(rng)).ToArray();
        for (int i = 0; i < Math.Min(rollCount, abilityNames.Length); i++)
            AbilityHelper.GetAbilityBlock(c, abilityNames[i]).Base = rolledScores[i];

        // Step 3 — Race
        var race = races[rng.Next(races.Count)];
        c.RaceId = race.Id;
        if (race.Subraces.Count > 0)
            c.SubraceId = race.Subraces[rng.Next(race.Subraces.Count)].Id;

        var raceBonuses = AbilityHelper.GetCombinedRacialBonuses(race, c.SubraceId ?? string.Empty);
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

        // Step 6 — Background + Skill Proficiencies + Languages
        var bg = backgrounds[rng.Next(backgrounds.Count)];
        c.BackgroundId = bg.Id;
        foreach (var sk in bg.SkillProficiencies)
            c.Skills[sk] = "background";

        // Fixed race languages
        var fixedLangIds = LanguageHelper.GetFixedLanguageIds(races, race.Id, c.SubraceId ?? string.Empty);
        foreach (var id in fixedLangIds)
            if (!c.Proficiencies.Languages.Contains(id))
                c.Proficiencies.Languages.Add(id);

        // Extra language slots
        int extraSlots = LanguageHelper.GetExtraLanguageSlots(races, backgrounds, race.Id, c.SubraceId ?? string.Empty, bg.Id);
        if (extraSlots > 0)
        {
            var available = languages
                .Where(l => !c.Proficiencies.Languages.Contains(l.Id))
                .OrderBy(_ => rng.Next(int.MaxValue))
                .Take(extraSlots)
                .Select(l => l.Id);
            foreach (var id in available)
                c.Proficiencies.Languages.Add(id);
        }

        var classSkillOptions = cls.SkillChoices.Options.Contains("skill:any")
            ? SkillCatalog.AllSkillIds
            : cls.SkillChoices.Options;
        var availableClassSkills = classSkillOptions
            .Where(sk => !c.Skills.ContainsKey(sk))
            .OrderBy(_ => rng.Next(int.MaxValue))
            .ToList();
        int classSkillCount = Math.Min(cls.SkillChoices.Count, availableClassSkills.Count);
        for (int i = 0; i < classSkillCount; i++)
            c.Skills[availableClassSkills[i]] = "class";

        // Step 7 — Spells (if spellcaster)
        if (cls.Spellcasting != null)
        {
            var sc = cls.Spellcasting;
            int maxCantrips = sc.CantripsKnownByLevel.Count >= level ? sc.CantripsKnownByLevel[level - 1] : 0;
            int maxKnown = sc.SpellsKnownByLevel.Count >= level ? sc.SpellsKnownByLevel[level - 1] : 0;
            var classSpells = spells.Where(s => s.ClassIds.Contains(cls.Id)).ToList();

            foreach (var spell in classSpells.Where(s => s.Level == 0).OrderBy(_ => rng.Next(int.MaxValue)).Take(maxCantrips))
                c.Spells.Add(new CharacterSpell { SpellId = spell.Id, ClassId = cls.Id, Prepared = false });

            var levelOneSpells = classSpells.Where(s => s.Level == 1).OrderBy(_ => rng.Next(int.MaxValue)).ToList();
            // For prepare-spell classes (wizard, cleric, druid, paladin), pick a few level 1 spells to prepare
            const int defaultPreparedSpellsAtLevelOne = 3;
            int spellsToPick = sc.PrepareSpells ? Math.Min(defaultPreparedSpellsAtLevelOne, levelOneSpells.Count) : Math.Min(maxKnown, levelOneSpells.Count);
            foreach (var spell in levelOneSpells.Take(spellsToPick))
                c.Spells.Add(new CharacterSpell { SpellId = spell.Id, ClassId = cls.Id, Prepared = true });
        }

        // Step 8 — Equipment (class starting equipment via choice groups)
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

}
