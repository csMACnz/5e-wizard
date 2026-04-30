using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates race and subrace selection and racial ability score bonus application.
/// </summary>
public class RaceValidator
{
    private readonly IReadOnlyList<RaceDefinition> _races;

    public RaceValidator(IReadOnlyList<RaceDefinition> races)
    {
        _races = races;
    }

    /// <summary>
    /// Validates the character's race selection and ability score racial bonuses.
    /// </summary>
    public ValidationResult Validate(Character character)
    {
        var result = new ValidationResult();

        var race = _races.FirstOrDefault(r => r.Id == character.RaceId);
        if (race == null)
        {
            result.Errors.Add(
                $"ERR_RACE_UNKNOWN: Race '{character.RaceId}' is not a recognized race ID.");
            return result;
        }

        // Subrace required when the race has subraces defined
        if (race.Subraces.Count > 0 && string.IsNullOrEmpty(character.SubraceId))
        {
            result.Errors.Add(
                $"ERR_SUBRACE_REQUIRED: Race '{character.RaceId}' requires a subrace selection.");
        }

        // Build expected racial bonuses (race base + subrace if present)
        var expectedBonuses = new Dictionary<string, int>(race.AbilityBonuses);
        if (!string.IsNullOrEmpty(character.SubraceId))
        {
            var subrace = race.Subraces.FirstOrDefault(s => s.Id == character.SubraceId);
            if (subrace != null)
            {
                foreach (var (ability, bonus) in subrace.AbilityBonuses)
                {
                    expectedBonuses[ability] = expectedBonuses.TryGetValue(ability, out int existing)
                        ? existing + bonus
                        : bonus;
                }
            }
        }

        // Validate every ability's racial bonus matches the expected value
        var allAbilities = new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
        foreach (var ability in allAbilities)
        {
            int actualBonus = GetRacialBonus(character.AbilityScores, ability);
            expectedBonuses.TryGetValue(ability, out int expectedBonus);

            if (actualBonus != expectedBonus)
            {
                result.Errors.Add(
                    $"ERR_RACE_BONUS_MISMATCH: Expected racial bonus of {expectedBonus} to {ability}, " +
                    $"but found {actualBonus}.");
            }
        }

        return result;
    }

    private static int GetRacialBonus(AbilityScores scores, string ability) => ability switch
    {
        "STR" => scores.STR.RacialBonus,
        "DEX" => scores.DEX.RacialBonus,
        "CON" => scores.CON.RacialBonus,
        "INT" => scores.INT.RacialBonus,
        "WIS" => scores.WIS.RacialBonus,
        "CHA" => scores.CHA.RacialBonus,
        _ => 0,
    };
}
