using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates the per-level hit point entries on a character.
/// Issues ERR_HP_ROLL_OUT_OF_RANGE when a manually-entered die-roll value is outside [1, hitDie].
/// </summary>
public class HitPointValidator
{
    private readonly IReadOnlyList<ClassDefinition> _classes;

    public HitPointValidator(IReadOnlyList<ClassDefinition> classes)
    {
        _classes = classes;
    }

    public ValidationResult Validate(Character character)
    {
        var result = new ValidationResult();

        foreach (var entry in character.HitPointEntries)
        {
            var cls = _classes.FirstOrDefault(c => c.Id == entry.ClassId);
            int hitDie = cls?.HitDie ?? 8;

            if (entry.DieRollValue < 1 || entry.DieRollValue > hitDie)
            {
                string clsDisplay = cls?.DisplayName ?? entry.ClassId;
                result.Errors.Add(
                    $"ERR_HP_ROLL_OUT_OF_RANGE: Hit points for {clsDisplay} level {entry.ClassLevel} " +
                    $"must be between 1 and {hitDie} (got {entry.DieRollValue}).");
            }
        }

        return result;
    }
}
