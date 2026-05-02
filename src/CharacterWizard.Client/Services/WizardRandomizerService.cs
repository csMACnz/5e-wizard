using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Client.Services;

/// <summary>
/// Provides randomization and dice-roll helpers for the character wizard steps.
/// All methods mutate WizardContext state; the caller is responsible for triggering
/// validation and auto-save (typically by invoking an OnChanged EventCallback).
/// </summary>
public sealed class WizardRandomizerService(WizardContext ctx)
{
    private static readonly string[] _characterNameOptions =
    [
        "Aric Stonehammer", "Lyra Swiftwind", "Gareth Ironwood", "Senna Brightflame",
        "Dorin Ashgrove", "Mira Dawnwhisper", "Talon Greymark", "Elara Frostweave",
        "Bram Coldstone", "Zara Nightfall", "Owen Steelforge", "Nara Moonveil",
        "Caden Embercroft", "Isra Thistledown", "Finn Blackthorn", "Selene Goldleaf",
        "Rowan Whitepeak", "Vex Shadowmere", "Cleo Starhelm", "Drake Thornfield",
    ];

    private static readonly string[] _campaignNameOptions =
    [
        "Curse of Strahd", "Descent into Avernus", "Icewind Dale", "Waterdeep Adventures",
        "The Lost Mines", "Secrets of the Sunken City", "The Dragon's Lair",
        "Shadows of the Underdark", "The Ancient Ruins", "War of the Five Kingdoms",
        "The Emerald Enclave", "Tomb of the Old Gods",
    ];

    // ── Meta ──────────────────────────────────────────────────────────────

    public async Task RandomizeCharacterNameAsync(IReadOnlyList<string> fullNames)
    {
        var namePool = fullNames.Count > 0 ? fullNames : _characterNameOptions;
        ctx.CharacterName = namePool[Random.Shared.Next(namePool.Count)];
    }

    public void RandomizeCampaignName()
    {
        ctx.CampaignName = _campaignNameOptions[Random.Shared.Next(_campaignNameOptions.Length)];
    }

    // ── Ability Scores ────────────────────────────────────────────────────

    public void RandomlyAssignStandardArray(AbilitiesConfig abilitiesConfig)
    {
        var shuffled = abilitiesConfig.StandardArray.OrderBy(_ => Random.Shared.Next()).ToArray();
        for (int i = 0; i < WizardContext.Abilities.Length; i++)
            ctx.AbilitySelections[WizardContext.Abilities[i]] = shuffled[i];
    }

    public void RollAllAbilities()
    {
        foreach (var ab in WizardContext.Abilities)
            ctx.RollValues[ab] = RollAbilityScore();
    }

    public void RerollAbility(string ability)
    {
        ctx.RollValues[ability] = RollAbilityScore();
    }

    // ── Race ──────────────────────────────────────────────────────────────

    public void RandomizeRace(IReadOnlyList<RaceDefinition> races)
    {
        if (races.Count == 0) return;
        ctx.SelectedRaceId = races[Random.Shared.Next(races.Count)].Id;
        ctx.SelectedSubraceId = string.Empty;
    }

    public void RandomizeSubrace(IReadOnlyList<RaceDefinition> races)
    {
        var race = races.FirstOrDefault(r => r.Id == ctx.SelectedRaceId);
        if (race == null || race.Subraces.Count == 0) return;
        ctx.SelectedSubraceId = race.Subraces[Random.Shared.Next(race.Subraces.Count)].Id;
    }

    // ── Class ─────────────────────────────────────────────────────────────

    public void RandomizeClass(int classIdx, IReadOnlyList<ClassDefinition> classes)
    {
        if (classes.Count == 0 || classIdx >= ctx.ClassEntries.Count) return;
        ctx.ClassEntries[classIdx].ClassId = classes[Random.Shared.Next(classes.Count)].Id;
        ctx.ClassEntries[classIdx].SubclassId = string.Empty;
    }

    public void RandomizeSubclass(int classIdx, IReadOnlyList<ClassDefinition> classes)
    {
        if (classIdx >= ctx.ClassEntries.Count) return;
        var clsDef = classes.FirstOrDefault(c => c.Id == ctx.ClassEntries[classIdx].ClassId);
        if (clsDef == null || clsDef.SubclassOptions.Count == 0) return;
        ctx.ClassEntries[classIdx].SubclassId = clsDef.SubclassOptions[Random.Shared.Next(clsDef.SubclassOptions.Count)].Id;
    }

    // ── Background ────────────────────────────────────────────────────────

    public void RandomizeBackground(IReadOnlyList<BackgroundDefinition> backgrounds)
    {
        if (backgrounds.Count == 0) return;
        ctx.SetBackground(backgrounds[Random.Shared.Next(backgrounds.Count)].Id, backgrounds);
    }

    // ── Skills ────────────────────────────────────────────────────────────

    public void RandomizeSkills(IReadOnlyList<ClassDefinition> classes, IReadOnlyList<BackgroundDefinition> backgrounds)
    {
        var primaryId = ctx.ClassEntries.Count > 0 ? ctx.ClassEntries[0].ClassId : string.Empty;
        var primaryCls = classes.FirstOrDefault(c => c.Id == primaryId);
        if (primaryCls == null) return;

        var bg = backgrounds.FirstOrDefault(b => b.Id == ctx.SelectedBackgroundId);
        var bgSkills = bg?.SkillProficiencies ?? [];

        for (int i = 0; i < ctx.ClassSkillSelections.Count; i++)
            ctx.ClassSkillSelections[i] = false;

        var available = ctx.ClassSkillOptionIds
            .Select((id, idx) => (id, idx))
            .Where(x => !bgSkills.Contains(x.id))
            .OrderBy(_ => Random.Shared.Next())
            .Take(primaryCls.SkillChoices.Count);

        foreach (var (_, idx) in available)
            ctx.ClassSkillSelections[idx] = true;
    }

    // ── HP ────────────────────────────────────────────────────────────────

    public void RollHp(string classId, int classLevel, int hitDie)
    {
        int rolled = Random.Shared.Next(1, hitDie + 1);
        string key = $"{classId}|{classLevel}";
        ctx.AllHpChoicesByKey[key] = ("manual", rolled);
    }

    // ── Spells ────────────────────────────────────────────────────────────

    public void RandomizeCantrips(string classId, int maxCantrips, IReadOnlyList<SpellDefinition> spells)
    {
        var cantrips = spells.Where(s => s.ClassIds.Contains(classId) && s.Level == 0).ToList();

        if (!ctx.SelectedSpells.ContainsKey(classId))
            ctx.SelectedSpells[classId] = [];

        var currentLeveled = ctx.SelectedSpells[classId]
            .Where(id => spells.FirstOrDefault(s => s.Id == id)?.Level > 0)
            .ToHashSet();

        ctx.SelectedSpells[classId] = currentLeveled;

        foreach (var spell in cantrips.OrderBy(_ => Random.Shared.Next()).Take(maxCantrips))
            ctx.SelectedSpells[classId].Add(spell.Id);
    }

    public void RandomizeSpells(
        string classId,
        int maxKnown,
        bool isPrepare,
        int classLevel,
        string spellcastingAbility,
        IReadOnlyList<SpellDefinition> spells,
        Character character)
    {
        var leveledSpells = spells.Where(s => s.ClassIds.Contains(classId) && s.Level > 0).ToList();

        if (!ctx.SelectedSpells.ContainsKey(classId))
            ctx.SelectedSpells[classId] = [];

        var currentCantrips = ctx.SelectedSpells[classId]
            .Where(id => spells.FirstOrDefault(s => s.Id == id)?.Level == 0)
            .ToHashSet();

        ctx.SelectedSpells[classId] = currentCantrips;

        int countToSelect;
        if (isPrepare)
        {
            var block = GetAbilityBlock(character, spellcastingAbility);
            int modifier = WizardContext.Modifier(block.Final);
            countToSelect = Math.Max(1, Math.Min(modifier + classLevel, leveledSpells.Count));
        }
        else
        {
            countToSelect = maxKnown;
        }

        foreach (var spell in leveledSpells.OrderBy(_ => Random.Shared.Next()).Take(countToSelect))
            ctx.SelectedSpells[classId].Add(spell.Id);
    }

    // ── Equipment ─────────────────────────────────────────────────────────

    public void RollStartingWealth(IReadOnlyList<ClassStartingEquipmentEntry> classStartingEquipmentConfigs)
    {
        var primaryClassId = ctx.ClassEntries.Count > 0 ? ctx.ClassEntries[0].ClassId : string.Empty;
        var config = classStartingEquipmentConfigs.FirstOrDefault(e => e.ClassId == primaryClassId);
        if (config == null) return;
        ctx.ClassStartingGold = RollWealthExpression(config.StartingWealthRoll);
    }

    // ── Static helpers ────────────────────────────────────────────────────

    public static int RollAbilityScore()
    {
        int r1 = Random.Shared.Next(1, 7);
        int r2 = Random.Shared.Next(1, 7);
        int r3 = Random.Shared.Next(1, 7);
        int r4 = Random.Shared.Next(1, 7);
        return r1 + r2 + r3 + r4 - Math.Min(Math.Min(r1, r2), Math.Min(r3, r4));
    }

    public static int RollWealthExpression(string expression)
    {
        var rng = Random.Shared;
        int multiplier = 1;
        var parts = expression.Split('*');
        var dicePart = parts[0].Trim();
        if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int m))
            multiplier = m;

        var diceParts = dicePart.Split('d');
        if (diceParts.Length != 2) return 0;
        if (!int.TryParse(diceParts[0], out int count) || !int.TryParse(diceParts[1], out int sides))
            return 0;

        int total = 0;
        for (int i = 0; i < count; i++)
            total += rng.Next(1, sides + 1);

        return total * multiplier;
    }

    private static AbilityBlock GetAbilityBlock(Character c, string ability) => ability switch
    {
        "STR" => c.AbilityScores.STR,
        "DEX" => c.AbilityScores.DEX,
        "CON" => c.AbilityScores.CON,
        "INT" => c.AbilityScores.INT,
        "WIS" => c.AbilityScores.WIS,
        "CHA" => c.AbilityScores.CHA,
        _ => new AbilityBlock(),
    };
}
