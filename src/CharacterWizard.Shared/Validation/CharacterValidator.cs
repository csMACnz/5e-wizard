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
    private readonly IReadOnlyList<SpellDefinition> _spells;
    private readonly IReadOnlyList<EquipmentItemDefinition> _equipment;
    private readonly IReadOnlyList<FeatDefinition> _feats;

    public CharacterValidator(
        IReadOnlyList<RaceDefinition> races,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<BackgroundDefinition> backgrounds,
        IReadOnlyList<SpellDefinition>? spells = null,
        IReadOnlyList<EquipmentItemDefinition>? equipment = null,
        IReadOnlyList<FeatDefinition>? feats = null)
    {
        _races = races;
        _classes = classes;
        _backgrounds = backgrounds;
        _spells = spells ?? [];
        _equipment = equipment ?? [];
        _feats = feats ?? [];
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

        // Spell validation
        if (_spells.Count > 0 && character.Spells.Count > 0)
        {
            var spellResult = new SpellValidator(_spells, _classes).Validate(character);
            result.Errors.AddRange(spellResult.Errors);
            result.Warnings.AddRange(spellResult.Warnings);
        }

        // Equipment validation
        if (_equipment.Count > 0 && character.Equipment.Count > 0)
        {
            var equipResult = new EquipmentValidator(_equipment).Validate(character);
            result.Errors.AddRange(equipResult.Errors);
            result.Warnings.AddRange(equipResult.Warnings);
        }

        // Level feature / ASI choice validation
        if (_feats.Count > 0)
        {
            var featResult = new LevelFeatureValidator(_classes, _feats).Validate(character);
            result.Errors.AddRange(featResult.Errors);
            result.Warnings.AddRange(featResult.Warnings);
        }

        // Hit point entry validation
        if (character.HitPointEntries.Count > 0)
        {
            var hpResult = new HitPointValidator(_classes).Validate(character);
            result.Errors.AddRange(hpResult.Errors);
            result.Warnings.AddRange(hpResult.Warnings);
        }

        return result;
    }
}
