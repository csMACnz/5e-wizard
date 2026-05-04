using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Utilities;

namespace CharacterWizard.Client.Services;

/// <summary>
/// Scoped DI service that holds all mutable wizard UI state across steps.
/// This is the single source of truth for every field the wizard steps read or write.
/// </summary>
public sealed class WizardContext
{
    // ── Constants ─────────────────────────────────────────────────────────
    public static readonly string[] Abilities = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];

    public static readonly Dictionary<string, string> SkillNames = new()
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

    public static readonly List<string> AllSkillIds =
    [
        "skill:acrobatics", "skill:animal-handling", "skill:arcana", "skill:athletics",
        "skill:deception", "skill:history", "skill:insight", "skill:intimidation",
        "skill:investigation", "skill:medicine", "skill:nature", "skill:perception",
        "skill:performance", "skill:persuasion", "skill:religion", "skill:sleight-of-hand",
        "skill:stealth", "skill:survival",
    ];

    // ── Step 1 state ──────────────────────────────────────────────────────
    public string CharacterName { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string GenerationMethodStr { get; set; } = nameof(GenerationMethod.StandardArray);

    // ── Step 2 state ──────────────────────────────────────────────────────
    public Dictionary<string, int> AbilitySelections { get; } = new()
    {
        ["STR"] = 0,
        ["DEX"] = 0,
        ["CON"] = 0,
        ["INT"] = 0,
        ["WIS"] = 0,
        ["CHA"] = 0,
    };

    public Dictionary<string, int> PointBuyValues { get; } = new()
    {
        ["STR"] = 8,
        ["DEX"] = 8,
        ["CON"] = 8,
        ["INT"] = 8,
        ["WIS"] = 8,
        ["CHA"] = 8,
    };

    public Dictionary<string, int> RollValues { get; } = new()
    {
        ["STR"] = 8,
        ["DEX"] = 8,
        ["CON"] = 8,
        ["INT"] = 8,
        ["WIS"] = 8,
        ["CHA"] = 8,
    };

    // ── Step 3 state ──────────────────────────────────────────────────────
    public string SelectedRaceId { get; set; } = string.Empty;
    public string SelectedSubraceId { get; set; } = string.Empty;

    // ── Step 4 state ──────────────────────────────────────────────────────
    public List<ClassEntry> ClassEntries { get; set; } = [new ClassEntry()];

    // ── Step 5 state ──────────────────────────────────────────────────────
    // Keyed as "classId|classLevel". Retained even when level is lowered so choices
    // are restored when the level is raised again within the same session.
    public Dictionary<string, AsiChoice> AllAsiChoicesByKey { get; } = [];

    // HP shadow state: keyed as "classId|classLevel" (1-based within class).
    public Dictionary<string, (string Method, int DieRollValue)> AllHpChoicesByKey { get; } = [];

    // ── Step 6 state ──────────────────────────────────────────────────────
    public string SelectedBackgroundId { get; set; } = string.Empty;
    public List<string> ClassSkillOptionIds { get; set; } = [];
    public List<bool> ClassSkillSelections { get; set; } = [];

    /// <summary>
    /// The extra language IDs chosen by the player on the Background step.
    /// Does not include fixed (race-granted) languages.
    /// </summary>
    public List<string> ChosenExtraLanguageIds { get; set; } = [];

    // ── Step 7 state (Spells) ─────────────────────────────────────────────
    // Key = classId, value = set of selected spell IDs.
    public Dictionary<string, HashSet<string>> SelectedSpells { get; } = [];

    // Racial cantrip selection (keyed by race/subrace source).
    public string SelectedRacialCantripId { get; set; } = string.Empty;

    // Bard Magical Secrets: keyed as "feat:magical-secrets-10/14/18", value = list of 2 selected spell IDs.
    public Dictionary<string, List<string>> MagicalSecretsSelections { get; } = [];

    // Warlock Mystic Arcanum: keyed as "feat:mystic-arcanum-6/7/8/9", value = selected spell ID or empty.
    public Dictionary<string, string> MysticArcanumSelections { get; } = [];

    // Wizard spellbook: Set of selected level-1 wizard spell IDs for starting spells.
    public HashSet<string> WizardSpellbookIds { get; } = [];

    // Toggle for spell level gating (per class).
    public bool ShowAllSpellLevels { get; set; } = false;

    // ── Step 8 state (Equipment) ──────────────────────────────────────────
    public HashSet<string> SelectedEquipmentIds { get; } = [];
    public bool StrictEquipment { get; set; } = true;
    // Choice group selections: groupId → (optionId, pickedItemId?)
    public Dictionary<string, (string OptionId, string? PickedItemId)> EquipmentChoices { get; } = [];
    public bool ClassStartingWealthChosen { get; set; } = false;
    public int? ClassStartingGold { get; set; } = null;

    // ── Computed ──────────────────────────────────────────────────────────
    public int TotalClassLevel => ClassEntries.Sum(e => e.Level);

    // ── Pure helpers ──────────────────────────────────────────────────────
    public static int Modifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    public static string SkillLabel(string id) =>
        SkillNames.TryGetValue(id, out string? name) ? name : id;

    public static string TraitName(string id)
    {
        var s = id.Replace("trait:", string.Empty).Replace("-", " ");
        return s.Length > 0 ? char.ToUpper(s[0]) + s[1..] : id;
    }

    public static string FeatureDisplayName(string id, IReadOnlyList<FeatDefinition> feats)
    {
        var featDef = feats.FirstOrDefault(f => f.Id == id);
        if (featDef != null) return featDef.DisplayName;
        return TraitName(id);
    }

    public static Dictionary<string, int> GetCombinedRacialBonuses(RaceDefinition race, string subraceId)
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

    // ── Smart state mutations ─────────────────────────────────────────────

    /// <summary>
    /// Updates the background selection and deselects any class skills that would
    /// conflict with the new background's granted skill proficiencies, and reconciles
    /// extra language picks against the new slot count.
    /// </summary>
    public void SetBackground(
        string value,
        IReadOnlyList<BackgroundDefinition> backgrounds,
        IReadOnlyList<RaceDefinition> races)
    {
        SelectedBackgroundId = value;
        var newBg = backgrounds.FirstOrDefault(b => b.Id == value);
        if (newBg != null)
        {
            for (int i = 0; i < ClassSkillOptionIds.Count; i++)
                if (ClassSkillSelections[i] && newBg.SkillProficiencies.Contains(ClassSkillOptionIds[i]))
                    ClassSkillSelections[i] = false;
        }

        ReconcileExtraLanguages(races, backgrounds);
    }

    /// <summary>
    /// Rebuilds the class skill option list based on the primary class, preserving
    /// any existing selections for skills that are still in the new option list.
    /// </summary>
    public void SyncClassSkillOptions(IReadOnlyList<ClassDefinition> classes)
    {
        var primaryId = ClassEntries.Count > 0 ? ClassEntries[0].ClassId : string.Empty;
        var primaryCls = classes.FirstOrDefault(c => c.Id == primaryId);
        if (primaryCls == null)
        {
            ClassSkillOptionIds = [];
            ClassSkillSelections = [];
            return;
        }

        var options = primaryCls.SkillChoices.Options.Contains("skill:any")
            ? AllSkillIds
            : (IReadOnlyList<string>)primaryCls.SkillChoices.Options;

        var newIds = options.ToList();
        var newSel = new List<bool>(new bool[newIds.Count]);
        for (int i = 0; i < newIds.Count; i++)
        {
            int oldIdx = ClassSkillOptionIds.IndexOf(newIds[i]);
            if (oldIdx >= 0 && oldIdx < ClassSkillSelections.Count)
                newSel[i] = ClassSkillSelections[oldIdx];
        }

        ClassSkillOptionIds = newIds;
        ClassSkillSelections = newSel;
    }

    /// <summary>
    /// Toggles strict equipment mode. When enabling strict mode, removes any
    /// currently selected items that are outside the allowed list.
    /// </summary>
    public void SetStrictEquipment(bool strict, IReadOnlyList<ClassDefinition> classes)
    {
        StrictEquipment = strict;
        if (strict)
        {
            var primaryClsId = ClassEntries.Count > 0 ? ClassEntries[0].ClassId : string.Empty;
            var allowedIds = classes.FirstOrDefault(c => c.Id == primaryClsId)?.StartingEquipmentIds ?? [];
            if (allowedIds.Count > 0)
            {
                var toRemove = SelectedEquipmentIds.Where(id => !allowedIds.Contains(id)).ToList();
                foreach (var id in toRemove)
                    SelectedEquipmentIds.Remove(id);
            }
        }
    }

    /// <summary>
    /// Returns the language IDs fixed by the selected race (and subrace), i.e. not subject
    /// to user choice.
    /// </summary>
    public List<string> GetFixedLanguageIds(IReadOnlyList<RaceDefinition> races) =>
        LanguageHelper.GetFixedLanguageIds(races, SelectedRaceId, SelectedSubraceId);

    /// <summary>
    /// Returns the number of extra language slots the character may fill (background +
    /// one per <c>trait:extra-language</c> in the selected race/subrace traits).
    /// Returns 0 if no background is selected.
    /// </summary>
    public int GetExtraLanguageSlots(
        IReadOnlyList<RaceDefinition> races,
        IReadOnlyList<BackgroundDefinition> backgrounds) =>
        LanguageHelper.GetExtraLanguageSlots(races, backgrounds, SelectedRaceId, SelectedSubraceId, SelectedBackgroundId);

    /// <summary>
    /// Returns the union of fixed race languages and the player's chosen extra languages,
    /// deduped, for display and export.
    /// </summary>
    public List<string> GetAllLanguageIds(
        IReadOnlyList<RaceDefinition> races,
        IReadOnlyList<BackgroundDefinition> backgrounds)
    {
        var result = new HashSet<string>(GetFixedLanguageIds(races));
        foreach (var id in ChosenExtraLanguageIds)
            result.Add(id);
        return [.. result];
    }

    /// <summary>
    /// Reconciles <see cref="ChosenExtraLanguageIds"/> after the race or background changes.
    /// Removes any chosen extras that are now in the fixed list, then trims if over the new
    /// slot limit (deterministically — removes from the end).
    /// </summary>
    public void ReconcileExtraLanguages(
        IReadOnlyList<RaceDefinition> races,
        IReadOnlyList<BackgroundDefinition> backgrounds)
    {
        var fixedIds = GetFixedLanguageIds(races);
        int slots = GetExtraLanguageSlots(races, backgrounds);
        ChosenExtraLanguageIds = LanguageHelper.Reconcile(ChosenExtraLanguageIds, fixedIds, slots);
    }
}

/// <summary>Represents a single class selection entry in the wizard (including multiclass entries).</summary>
public sealed class ClassEntry
{
    public string ClassId { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public string SubclassId { get; set; } = string.Empty;
}
