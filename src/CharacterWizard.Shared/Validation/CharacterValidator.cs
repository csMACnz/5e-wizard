using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Composes all step validators into a single full-run validation pass.
/// </summary>
public class CharacterValidator
{
    private readonly IReadOnlyList<RaceDefinition> _races;
    private readonly IReadOnlyList<ClassDefinition> _classes;
    private readonly IReadOnlyList<BackgroundDefinition> _backgrounds;

    public CharacterValidator(
        IReadOnlyList<RaceDefinition> races,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<BackgroundDefinition> backgrounds)
    {
        _races = races;
        _classes = classes;
        _backgrounds = backgrounds;
    }

    /// <summary>
    /// Runs all validators in sequence and returns an aggregated result.
    /// Dispatches to the correct ability-generation validator based on GenerationMethod.
    /// </summary>
    public ValidationResult Validate(Character character)
    {
        var result = new ValidationResult();

        // Ability generation validation — dispatch based on method
        int[] baseScores =
        [
            character.AbilityScores.STR.Base,
            character.AbilityScores.DEX.Base,
            character.AbilityScores.CON.Base,
            character.AbilityScores.INT.Base,
            character.AbilityScores.WIS.Base,
            character.AbilityScores.CHA.Base,
        ];

        var abilityResult = character.GenerationMethod switch
        {
            GenerationMethod.StandardArray => StandardArrayValidator.Validate(baseScores),
            GenerationMethod.PointBuy => PointBuyValidator.Validate(baseScores),
            GenerationMethod.Roll => RollValidator.Validate(baseScores),
            _ => new ValidationResult(),
        };

        result.Errors.AddRange(abilityResult.Errors);
        result.Warnings.AddRange(abilityResult.Warnings);

        // Race validation
        var raceResult = new RaceValidator(_races).Validate(character);
        result.Errors.AddRange(raceResult.Errors);
        result.Warnings.AddRange(raceResult.Warnings);

        // Class validation
        var classResult = new ClassValidator(_classes).Validate(character);
        result.Errors.AddRange(classResult.Errors);
        result.Warnings.AddRange(classResult.Warnings);

        // Proficiency validation
        var profResult = new ProficiencyValidator(_classes, _backgrounds).Validate(character);
        result.Errors.AddRange(profResult.Errors);
        result.Warnings.AddRange(profResult.Warnings);

        return result;
    }
}
