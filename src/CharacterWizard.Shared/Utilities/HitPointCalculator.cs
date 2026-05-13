using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Utilities;

/// <summary>
/// Shared max-HP calculation used by rendering and export paths.
/// </summary>
public static class HitPointCalculator
{
    public static int CalculateMaxHp(Character character, IReadOnlyList<ClassDefinition> classes)
    {
        int conMod = AbilityHelper.GetModifier(character.AbilityScores.CON.Final);

        if (character.HitPointEntries.Count > 0)
        {
            int total = 0;
            foreach (var entry in character.HitPointEntries)
                total += Math.Max(1, entry.DieRollValue + conMod);
            return total;
        }

        int fallback = 0;
        foreach (var classLevel in character.Levels)
        {
            var cls = classes.FirstOrDefault(cl => cl.Id == classLevel.ClassId);
            int hitDie = cls?.HitDie ?? 8;
            int average = (hitDie / 2) + 1;
            if (classLevel.Level >= 1)
                fallback += Math.Max(1, hitDie + conMod);
            if (classLevel.Level > 1)
                fallback += (classLevel.Level - 1) * Math.Max(1, average + conMod);
        }
        return fallback;
    }
}
