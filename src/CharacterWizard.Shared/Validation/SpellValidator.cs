using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Utilities;

namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates the spell selections on a character against class rules and known spell data.
/// </summary>
public class SpellValidator
{
    private readonly IReadOnlyList<SpellDefinition> _spells;
    private readonly IReadOnlyList<ClassDefinition> _classes;

    public SpellValidator(IReadOnlyList<SpellDefinition> spells, IReadOnlyList<ClassDefinition> classes)
    {
        _spells = spells;
        _classes = classes;
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
        foreach (var cs in character.Spells)
        {
            if (!spellLookup.TryGetValue(cs.SpellId, out var spellDef))
            {
                result.Errors.Add($"ERR_SPELL_UNKNOWN: Spell '{cs.SpellId}' does not exist in the spell list.");
                continue;
            }

            // Spell must be on the class's spell list
            if (!spellDef.ClassIds.Contains(cs.ClassId))
            {
                result.Errors.Add($"ERR_SPELL_NOT_FOR_CLASS: Spell '{cs.SpellId}' is not available for class '{cs.ClassId}'.");
            }
        }

        if (!result.IsValid)
            return result;

        // Per-class spell count validation
        foreach (var classLevel in character.Levels)
        {
            var classDef = _classes.FirstOrDefault(c => c.Id == classLevel.ClassId);
            if (classDef?.Spellcasting is not { } sc)
                continue;

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
}
