using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Utilities;

/// <summary>
/// Shared helpers for ability-score and racial-bonus operations.
/// </summary>
public static class AbilityHelper
{
    public static readonly string[] AbilityOrder = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];

    public static int GetModifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    public static AbilityBlock GetAbilityBlock(Character character, string ability) =>
        GetAbilityBlock(character.AbilityScores, ability);

    public static AbilityBlock GetAbilityBlock(AbilityScores scores, string ability) => ability switch
    {
        "STR" => scores.STR,
        "DEX" => scores.DEX,
        "CON" => scores.CON,
        "INT" => scores.INT,
        "WIS" => scores.WIS,
        "CHA" => scores.CHA,
        _ => new AbilityBlock(),
    };

    public static int GetFinalScore(Character character, string ability) =>
        GetAbilityBlock(character, ability).Final;

    public static Dictionary<string, int> GetCombinedRacialBonuses(RaceDefinition race, string subraceId)
    {
        var bonuses = new Dictionary<string, int>(race.AbilityBonuses);
        if (!string.IsNullOrEmpty(subraceId))
        {
            var sub = race.Subraces.FirstOrDefault(s => s.Id == subraceId);
            if (sub != null)
                foreach (var (ab, v) in sub.AbilityBonuses)
                    bonuses[ab] = bonuses.TryGetValue(ab, out int e) ? e + v : v;
        }

        return bonuses;
    }

    public static void AddOtherBonus(AbilityScores scores, string ability, int amount)
    {
        switch (ability)
        {
            case "STR": scores.STR.OtherBonus += amount; break;
            case "DEX": scores.DEX.OtherBonus += amount; break;
            case "CON": scores.CON.OtherBonus += amount; break;
            case "INT": scores.INT.OtherBonus += amount; break;
            case "WIS": scores.WIS.OtherBonus += amount; break;
            case "CHA": scores.CHA.OtherBonus += amount; break;
        }
    }
}
