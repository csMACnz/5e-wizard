using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates skill proficiency selections against class and background allowances.
/// </summary>
public class ProficiencyValidator
{
    private readonly IReadOnlyList<ClassDefinition> _classes;
    private readonly IReadOnlyList<BackgroundDefinition> _backgrounds;

    public ProficiencyValidator(
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<BackgroundDefinition> backgrounds)
    {
        _classes = classes;
        _backgrounds = backgrounds;
    }

    /// <summary>
    /// Validates the character's skill proficiency selections.
    /// Skills in the character's Skills dictionary with value "class" are treated as class choices;
    /// skills with value "background" are treated as background grants.
    /// </summary>
    public ValidationResult Validate(Character character)
    {
        var result = new ValidationResult();

        if (character.Levels.Count == 0)
            return result;

        var primaryClassId = character.Levels[0].ClassId;
        var classDef = _classes.FirstOrDefault(c => c.Id == primaryClassId);
        if (classDef == null)
            return result; // ClassValidator handles unknown class errors

        var backgroundDef = _backgrounds.FirstOrDefault(b => b.Id == character.BackgroundId);

        // Class-chosen skills are those explicitly tagged with source "class"
        var classChosenSkills = character.Skills
            .Where(kv => kv.Value == "class")
            .Select(kv => kv.Key)
            .ToList();

        // Validate class skill count matches the class definition
        if (classChosenSkills.Count != classDef.SkillChoices.Count)
        {
            result.Errors.Add(
                $"ERR_SKILL_COUNT: Expected {classDef.SkillChoices.Count} class skill choice(s) for " +
                $"'{primaryClassId}', but found {classChosenSkills.Count}.");
        }

        // Validate class skills are from the allowed list (unless the class allows any skill)
        if (!classDef.SkillChoices.Options.Contains("skill:any"))
        {
            foreach (var skill in classChosenSkills)
            {
                if (!classDef.SkillChoices.Options.Contains(skill))
                {
                    result.Errors.Add(
                        $"ERR_SKILL_NOT_ALLOWED: Skill '{skill}' is not in the allowed list for " +
                        $"class '{primaryClassId}'.");
                }
            }
        }

        // Validate no duplicate skills between class choices and background grants
        if (backgroundDef != null)
        {
            foreach (var skill in classChosenSkills)
            {
                if (backgroundDef.SkillProficiencies.Contains(skill))
                {
                    result.Errors.Add(
                        $"ERR_SKILL_DUPLICATE: Skill '{skill}' is already granted by background " +
                        $"'{character.BackgroundId}' and cannot also be chosen as a class skill.");
                }
            }
        }

        return result;
    }
}
