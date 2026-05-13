using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Utilities;

/// <summary>
/// Shared small rule fragments for spell-step gating and warnings.
/// </summary>
public static class SpellSelectionRules
{
    public static readonly int[] MagicalSecretsGrantLevels = [10, 14, 18];
    public static readonly int[] MysticArcanumMinLevels = [11, 13, 15, 17];
    public static readonly int[] MysticArcanumSpellLevels = [6, 7, 8, 9];

    public static int GetWizardRequiredSpellbookCount(int classLevel) =>
        classLevel < 1 ? 0 : 6 + (2 * (classLevel - 1));

    public static bool HasRacialCantripTrait(
        IReadOnlyList<RaceDefinition> races,
        string raceId,
        string subraceId)
    {
        var race = races.FirstOrDefault(r => r.Id == raceId);
        bool hasCantripTrait = race?.TraitIds.Contains("trait:cantrip") == true;
        if (!hasCantripTrait && !string.IsNullOrEmpty(subraceId))
        {
            var sub = race?.Subraces.FirstOrDefault(s => s.Id == subraceId);
            hasCantripTrait = sub?.TraitIds.Contains("trait:cantrip") == true;
        }

        return hasCantripTrait;
    }
}
