using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Utilities;

namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates the spell selections on a character against class rules and known spell data.
/// </summary>
public class SpellValidator
{
    private const string SpellListIdPrefix = "spelllist:";

    private readonly IReadOnlyList<SpellDefinition> _spells;
    private readonly IReadOnlyList<ClassDefinition> _classes;

    // Flat lookup: subclassId → (parent ClassDefinition, SubclassDefinition)
    private readonly Dictionary<string, (ClassDefinition Parent, SubclassDefinition Sub)> _subclassLookup;

    public SpellValidator(IReadOnlyList<SpellDefinition> spells, IReadOnlyList<ClassDefinition> classes)
    {
        _spells = spells;
        _classes = classes;

        _subclassLookup = [];
        foreach (var cls in _classes)
            foreach (var sub in cls.SubclassOptions)
                _subclassLookup[sub.Id] = (cls, sub);
    }

    public ValidationResult Validate(Character character)
    {
        var result = new ValidationResult();

        if (character.Spells.Count == 0)
            return result;

        // No duplicate spell IDs
        var seenIds = new HashSet<string>();
        foreach (var cs in character.Spells)
        {
            if (!seenIds.Add(cs.SpellId))
            {
                result.Errors.Add($"ERR_SPELL_DUPLICATE: Spell '{cs.SpellId}' is selected more than once.");
                return result;
            }
        }

        // Each spell must exist in the known spell data
        var spellLookup = _spells.ToDictionary(s => s.Id);
        var classIds = new HashSet<string>(_classes.Select(c => c.Id));

        foreach (var cs in character.Spells)
        {
            if (!spellLookup.TryGetValue(cs.SpellId, out var spellDef))
            {
                result.Errors.Add($"ERR_SPELL_UNKNOWN: Spell '{cs.SpellId}' does not exist in the spell list.");
                continue;
            }

            // Resolve the effective class ID for the spell membership check.
            // cs.ClassId may be a class ID, a subclass ID (AT/EK), or a race/subrace ID
            // (racial cantrips). Only validate class membership when we can resolve a class ID.
            string? effectiveClassId = ResolveEffectiveClassId(cs.ClassId, classIds);

            if (effectiveClassId != null && !spellDef.ClassIds.Contains(effectiveClassId))
            {
                result.Errors.Add($"ERR_SPELL_NOT_FOR_CLASS: Spell '{cs.SpellId}' is not available for class '{cs.ClassId}'.");
            }
        }

        if (!result.IsValid)
            return result;

        // Per-class spell count validation (class-level spellcasting)
        foreach (var classLevel in character.Levels)
        {
            var classDef = _classes.FirstOrDefault(c => c.Id == classLevel.ClassId);
            if (classDef?.Spellcasting is not { } sc)
            {
                // No class-level spellcasting — check for subclass-based spellcasting (e.g., AT/EK)
                if (!string.IsNullOrEmpty(classLevel.SubclassId) &&
                    _subclassLookup.TryGetValue(classLevel.SubclassId, out var subInfo) &&
                    subInfo.Sub.Spellcasting is { } subSc)
                {
                    ValidateSubclassSpellcasting(character, classLevel, subSc, spellLookup, result);
                }
                continue;
            }

            int level = Math.Clamp(classLevel.Level, 1, 20);
            var classSpells = character.Spells.Where(s => s.ClassId == classLevel.ClassId).ToList();
            var cantrips = classSpells.Where(s => spellLookup.TryGetValue(s.SpellId, out var sd) && sd.Level == 0).ToList();
            var leveled = classSpells.Where(s => spellLookup.TryGetValue(s.SpellId, out var sd) && sd.Level > 0).ToList();
            int highestSlot = SpellSlotCalculator.GetHighestSlotLevel(level, sc.CastingType);

            // Spell level restriction: no selected spell may exceed the highest castable slot level
            foreach (var classSpell in leveled)
            {
                if (spellLookup.TryGetValue(classSpell.SpellId, out var spellDef) && spellDef.Level > highestSlot)
                {
                    result.Errors.Add($"ERR_SPELL_LEVEL_TOO_HIGH: Spell '{classSpell.SpellId}' (level {spellDef.Level}) cannot be selected for class '{classLevel.ClassId}' at level {level} (max castable spell level: {highestSlot}).");
                }
            }

            // Cantrip count check (for classes that have cantrips)
            if (sc.CantripsKnownByLevel.Count >= level)
            {
                int expectedCantrips = sc.CantripsKnownByLevel[level - 1];
                if (expectedCantrips > 0 && cantrips.Count > expectedCantrips)
                {
                    result.Errors.Add($"ERR_SPELL_CANTRIP_COUNT: Class '{classLevel.ClassId}' at level {level} allows {expectedCantrips} cantrip(s) but {cantrips.Count} selected.");
                }
            }

            // Known spells count check (for non-prepare casters)
            if (!sc.PrepareSpells && sc.SpellsKnownByLevel.Count >= level)
            {
                int maxKnown = sc.SpellsKnownByLevel[level - 1];
                if (leveled.Count > maxKnown)
                {
                    result.Errors.Add($"ERR_SPELL_KNOWN_COUNT: Class '{classLevel.ClassId}' at level {level} allows {maxKnown} leveled spell(s) but {leveled.Count} selected.");
                }
            }

            // Wizard spellbook check: spells of any castable level (ClassId = "class:wizard", level > 0)
            // At level 1: 6 spells; each additional level adds 2 more (6 + 2*(level-1) total minimum).
            if (classLevel.ClassId == "class:wizard" && level >= 1)
            {
                int requiredSpellbookCount = 6 + 2 * (level - 1);
                var wizardSpellbookSpells = character.Spells
                    .Where(s => s.ClassId == "class:wizard" && spellLookup.TryGetValue(s.SpellId, out var sd) && sd.Level >= 1 && sd.Level <= highestSlot)
                    .ToList();
                if (wizardSpellbookSpells.Count < requiredSpellbookCount)
                {
                    result.Errors.Add($"ERR_SPELL_WIZARD_SPELLBOOK_COUNT: Wizard must have at least {requiredSpellbookCount} spells of castable level in the spellbook but only {wizardSpellbookSpells.Count} selected.");
                }
            }
        }

        return result;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the effective class ID to use for a spell's class-membership check.
    /// Returns null when the ClassId is a race/subrace ID (racial cantrip) — those are not
    /// validated against any class spell list.
    /// </summary>
    private string? ResolveEffectiveClassId(string classId, HashSet<string> classIds)
    {
        // Direct class ID match
        if (classIds.Contains(classId))
            return classId;

        // Subclass ID — resolve via the subclass's SpellListId
        if (_subclassLookup.TryGetValue(classId, out var subInfo))
        {
            var sc = subInfo.Sub.Spellcasting;
            if (sc?.SpellListId != null && sc.SpellListId.StartsWith(SpellListIdPrefix))
                return "class:" + sc.SpellListId[SpellListIdPrefix.Length..];

            // Subclass found but no SpellListId — fall back to parent class ID
            return subInfo.Parent.Id;
        }

        // Race/subrace ID or other non-class source — skip the class membership check
        return null;
    }

    /// <summary>
    /// Validates spells stored under a subclass ID (e.g., Arcane Trickster, Eldritch Knight)
    /// against the subclass's <see cref="SpellcastingInfo"/>.
    /// </summary>
    private static void ValidateSubclassSpellcasting(
        Character character,
        ClassLevel classLevel,
        SpellcastingInfo sc,
        Dictionary<string, SpellDefinition> spellLookup,
        ValidationResult result)
    {
        int level = Math.Clamp(classLevel.Level, 1, 20);
        var subclassSpells = character.Spells.Where(s => s.ClassId == classLevel.SubclassId).ToList();
        var cantrips = subclassSpells.Where(s => spellLookup.TryGetValue(s.SpellId, out var sd) && sd.Level == 0).ToList();
        var leveled = subclassSpells.Where(s => spellLookup.TryGetValue(s.SpellId, out var sd) && sd.Level > 0).ToList();
        int highestSlot = SpellSlotCalculator.GetHighestSlotLevel(level, sc.CastingType);

        // Spell level restriction
        foreach (var classSpell in leveled)
        {
            if (spellLookup.TryGetValue(classSpell.SpellId, out var spellDef) && spellDef.Level > highestSlot)
            {
                result.Errors.Add($"ERR_SPELL_LEVEL_TOO_HIGH: Spell '{classSpell.SpellId}' (level {spellDef.Level}) cannot be selected for subclass '{classLevel.SubclassId}' at level {level} (max castable spell level: {highestSlot}).");
            }
        }

        // Cantrip count check
        if (sc.CantripsKnownByLevel.Count >= level)
        {
            int expectedCantrips = sc.CantripsKnownByLevel[level - 1];
            if (expectedCantrips > 0 && cantrips.Count > expectedCantrips)
            {
                result.Errors.Add($"ERR_SPELL_CANTRIP_COUNT: Subclass '{classLevel.SubclassId}' at level {level} allows {expectedCantrips} cantrip(s) but {cantrips.Count} selected.");
            }
        }

        // Known spells count check (for non-prepare casters)
        if (!sc.PrepareSpells && sc.SpellsKnownByLevel.Count >= level)
        {
            int maxKnown = sc.SpellsKnownByLevel[level - 1];
            if (leveled.Count > maxKnown)
            {
                result.Errors.Add($"ERR_SPELL_KNOWN_COUNT: Subclass '{classLevel.SubclassId}' at level {level} allows {maxKnown} leveled spell(s) but {leveled.Count} selected.");
            }
        }
    }
}
