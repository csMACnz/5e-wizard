using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Utilities;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Client.Services;

/// <summary>
/// Provides randomization and dice-roll helpers for the character wizard steps.
/// All methods mutate WizardContext state; the caller is responsible for triggering
/// validation and auto-save (typically by invoking an OnChanged EventCallback).
/// Each public method creates its own independent <see cref="IRng"/> via the injected
/// <see cref="IRngFactory"/>, keeping separate randomisation actions isolated.
/// </summary>
public sealed class WizardRandomizerService(WizardContext ctx, IRngFactory rngFactory)
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
        var rng = rngFactory.Create();
        var namePool = fullNames.Count > 0 ? fullNames : _characterNameOptions;
        ctx.CharacterName = namePool[rng.Next(namePool.Count)];
    }

    public void RandomizeCampaignName()
    {
        var rng = rngFactory.Create();
        ctx.CampaignName = _campaignNameOptions[rng.Next(_campaignNameOptions.Length)];
    }

    // ── Ability Scores ────────────────────────────────────────────────────

    public void RandomlyAssignStandardArray(AbilitiesConfig abilitiesConfig)
    {
        var rng = rngFactory.Create();
        var shuffled = abilitiesConfig.StandardArray.OrderBy(_ => rng.Next(int.MaxValue)).ToArray();
        for (int i = 0; i < WizardContext.Abilities.Length; i++)
            ctx.AbilitySelections[WizardContext.Abilities[i]] = shuffled[i];
    }

    public void RollAllAbilities()
    {
        var rng = rngFactory.Create();
        foreach (var ab in WizardContext.Abilities)
            ctx.RollValues[ab] = DiceHelper.RollAbilityScore(rng);
    }

    public void RerollAbility(string ability)
    {
        var rng = rngFactory.Create();
        ctx.RollValues[ability] = DiceHelper.RollAbilityScore(rng);
    }

    // ── Race ──────────────────────────────────────────────────────────────

    public void RandomizeRace(IReadOnlyList<RaceDefinition> races)
    {
        if (races.Count == 0) return;
        var rng = rngFactory.Create();
        ctx.SelectedRaceId = races[rng.Next(races.Count)].Id;
        ctx.SelectedSubraceId = string.Empty;
    }

    public void RandomizeSubrace(IReadOnlyList<RaceDefinition> races)
    {
        var race = races.FirstOrDefault(r => r.Id == ctx.SelectedRaceId);
        if (race == null || race.Subraces.Count == 0) return;
        var rng = rngFactory.Create();
        ctx.SelectedSubraceId = race.Subraces[rng.Next(race.Subraces.Count)].Id;
    }

    // ── Class ─────────────────────────────────────────────────────────────

    public void RandomizeClass(int classIdx, IReadOnlyList<ClassDefinition> classes)
    {
        if (classes.Count == 0 || classIdx >= ctx.ClassEntries.Count) return;
        var rng = rngFactory.Create();
        ctx.ClassEntries[classIdx].ClassId = classes[rng.Next(classes.Count)].Id;
        ctx.ClassEntries[classIdx].SubclassId = string.Empty;
    }

    public void RandomizeSubclass(int classIdx, IReadOnlyList<ClassDefinition> classes)
    {
        if (classIdx >= ctx.ClassEntries.Count) return;
        var clsDef = classes.FirstOrDefault(c => c.Id == ctx.ClassEntries[classIdx].ClassId);
        if (clsDef == null || clsDef.SubclassOptions.Count == 0) return;
        var rng = rngFactory.Create();
        ctx.ClassEntries[classIdx].SubclassId = clsDef.SubclassOptions[rng.Next(clsDef.SubclassOptions.Count)].Id;
    }

    // ── Background ────────────────────────────────────────────────────────

    public void RandomizeBackground(
        IReadOnlyList<BackgroundDefinition> backgrounds,
        IReadOnlyList<RaceDefinition> races)
    {
        if (backgrounds.Count == 0) return;
        var rng = rngFactory.Create();
        ctx.SetBackground(backgrounds[rng.Next(backgrounds.Count)].Id, backgrounds, races);
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

        var rng = rngFactory.Create();
        var available = ctx.ClassSkillOptionIds
            .Select((id, idx) => (id, idx))
            .Where(x => !bgSkills.Contains(x.id))
            .OrderBy(_ => rng.Next(int.MaxValue))
            .Take(primaryCls.SkillChoices.Count);

        foreach (var (_, idx) in available)
            ctx.ClassSkillSelections[idx] = true;
    }

    // ── HP ────────────────────────────────────────────────────────────────

    public void RollHp(string classId, int classLevel, int hitDie)
    {
        var rng = rngFactory.Create();
        int rolled = rng.Next(1, hitDie + 1);
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

        var rng = rngFactory.Create();
        foreach (var spell in cantrips.OrderBy(_ => rng.Next(int.MaxValue)).Take(maxCantrips))
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
            var block = AbilityHelper.GetAbilityBlock(character, spellcastingAbility);
            int modifier = WizardContext.Modifier(block.Final);
            countToSelect = Math.Max(1, Math.Min(modifier + classLevel, leveledSpells.Count));
        }
        else
        {
            countToSelect = maxKnown;
        }

        var rng = rngFactory.Create();
        foreach (var spell in leveledSpells.OrderBy(_ => rng.Next(int.MaxValue)).Take(countToSelect))
            ctx.SelectedSpells[classId].Add(spell.Id);
    }

    // ── Equipment ─────────────────────────────────────────────────────────

    public void RollStartingWealth(IReadOnlyList<ClassStartingEquipmentEntry> classStartingEquipmentConfigs)
    {
        var primaryClassId = ctx.ClassEntries.Count > 0 ? ctx.ClassEntries[0].ClassId : string.Empty;
        var config = classStartingEquipmentConfigs.FirstOrDefault(e => e.ClassId == primaryClassId);
        if (config == null) return;
        var rng = rngFactory.Create();
        ctx.ClassStartingGold = DiceHelper.TryRollExpression(rng, config.StartingWealthRoll, out int rolled)
            ? rolled
            : 0;
    }

    // ── Static helpers ────────────────────────────────────────────────────

    public static int RollAbilityScore(IRng rng) => DiceHelper.RollAbilityScore(rng);

    public static int RollWealthExpression(IRng rng, string expression) =>
        DiceHelper.TryRollExpression(rng, expression, out int result) ? result : 0;
}
