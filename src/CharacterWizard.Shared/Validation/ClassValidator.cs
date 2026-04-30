using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates class selection and multiclass prerequisites per SRD rules.
/// </summary>
public class ClassValidator
{
    private readonly IReadOnlyList<ClassDefinition> _classes;

    public ClassValidator(IReadOnlyList<ClassDefinition> classes)
    {
        _classes = classes;
    }

    /// <summary>
    /// Validates the character's class levels and multiclass prerequisites.
    /// </summary>
    public ValidationResult Validate(Character character)
    {
        var result = new ValidationResult();

        // Validate each class ID exists
        foreach (var classLevel in character.Levels)
        {
            if (!_classes.Any(c => c.Id == classLevel.ClassId))
            {
                result.Errors.Add(
                    $"ERR_CLASS_UNKNOWN: Class '{classLevel.ClassId}' is not a recognized class ID.");
            }
        }

        if (result.Errors.Count > 0)
            return result;

        // Validate sum of class levels matches TotalLevel
        int sumLevels = character.Levels.Sum(cl => cl.Level);
        if (sumLevels != character.TotalLevel)
        {
            result.Errors.Add(
                $"ERR_CLASS_TOTAL_LEVEL: Sum of class levels ({sumLevels}) does not match " +
                $"Character.TotalLevel ({character.TotalLevel}).");
        }

        // Validate total level does not exceed 20
        if (sumLevels > 20)
        {
            result.Errors.Add(
                $"ERR_CLASS_LEVEL_EXCEEDS_MAX: Total level {sumLevels} exceeds the maximum of 20.");
        }

        // Validate multiclass prerequisites when more than one class is present
        if (character.Levels.Count > 1)
        {
            foreach (var classLevel in character.Levels)
            {
                var classDef = _classes.First(c => c.Id == classLevel.ClassId);
                foreach (var (ability, required) in classDef.MulticlassPrereqs)
                {
                    int actualScore = GetAbilityScore(character.AbilityScores, ability);
                    if (actualScore < required)
                    {
                        result.Errors.Add(
                            $"ERR_MULTICLASS_PREREQ: Class '{classLevel.ClassId}' requires {ability} >= {required}, " +
                            $"but the character's {ability} score is {actualScore}.");
                    }
                }
            }
        }

        return result;
    }

    private static int GetAbilityScore(AbilityScores scores, string ability) => ability switch
    {
        "STR" => scores.STR.Final,
        "DEX" => scores.DEX.Final,
        "CON" => scores.CON.Final,
        "INT" => scores.INT.Final,
        "WIS" => scores.WIS.Final,
        "CHA" => scores.CHA.Final,
        _ => 0,
    };
}
