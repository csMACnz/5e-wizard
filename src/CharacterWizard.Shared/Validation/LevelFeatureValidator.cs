using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates that the character's ASI choices match the ASI opportunities granted by
/// their class levels, and that any general feat selections are valid.
/// </summary>
public class LevelFeatureValidator
{
    private readonly IReadOnlyList<ClassDefinition> _classes;
    private readonly IReadOnlyList<FeatDefinition> _feats;

    private const string AsiSentinel = "feat:asi";

    public LevelFeatureValidator(
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<FeatDefinition> feats)
    {
        _classes = classes;
        _feats = feats;
    }

    /// <summary>
    /// Validates ASI choices on the character.
    /// Issues a warning for each uncompleted ASI opportunity and an error for invalid feat picks.
    /// </summary>
    public ValidationResult Validate(Character character)
    {
        var result = new ValidationResult();

        foreach (var classLevel in character.Levels)
        {
            var classDef = _classes.FirstOrDefault(c => c.Id == classLevel.ClassId);
            if (classDef == null) continue;

            // Collect all levels where feat:asi appears and are within the character's current level
            foreach (var (levelKey, featIds) in classDef.FeaturesByLevel)
            {
                if (!int.TryParse(levelKey, out int grantLevel)) continue;
                if (grantLevel > classLevel.Level) continue;
                if (!featIds.Contains(AsiSentinel)) continue;

                var choice = character.AsiChoices.FirstOrDefault(
                    a => a.ClassId == classLevel.ClassId && a.ClassLevel == grantLevel);

                if (choice == null)
                {
                    var clsDisplay = classDef.DisplayName.Length > 0 ? classDef.DisplayName : classLevel.ClassId;
                    result.Warnings.Add(
                        $"WARN_ASI_INCOMPLETE: ASI choice at {clsDisplay} level {grantLevel} has not been made.");
                    continue;
                }

                // Validate feat choice
                if (choice.FeatId != null)
                {
                    var featDef = _feats.FirstOrDefault(f => f.Id == choice.FeatId);
                    if (featDef == null || featDef.Type != "general")
                    {
                        result.Errors.Add(
                            $"ERR_ASI_INVALID_FEAT: '{choice.FeatId}' is not a valid general feat " +
                            $"for the ASI at {classDef.DisplayName} level {grantLevel}.");
                    }
                }
            }
        }

        return result;
    }
}
