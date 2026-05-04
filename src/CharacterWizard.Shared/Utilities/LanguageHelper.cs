using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Utilities;

/// <summary>
/// Stateless helpers for computing language-related values from race, subrace, and
/// background definitions.  Kept in the Shared library so it can be used by both
/// the wizard UI (WizardContext) and unit tests.
/// </summary>
public static class LanguageHelper
{
    /// <summary>
    /// Returns the language IDs that are automatically granted by the selected race
    /// (and subrace), i.e. fixed languages that do not consume extra-language slots.
    /// </summary>
    public static List<string> GetFixedLanguageIds(
        IReadOnlyList<RaceDefinition> races,
        string raceId,
        string subraceId)
    {
        var result = new HashSet<string>();
        var race = races.FirstOrDefault(r => r.Id == raceId);
        if (race == null) return [];
        foreach (var id in race.LanguageIds) result.Add(id);
        if (!string.IsNullOrEmpty(subraceId))
        {
            var sub = race.Subraces.FirstOrDefault(s => s.Id == subraceId);
            if (sub != null)
                foreach (var id in sub.LanguageIds) result.Add(id);
        }

        return [.. result];
    }

    /// <summary>
    /// Returns the total number of extra language slots available to the character.
    /// Computed as <c>background.languageCount + count(trait:extra-language in race + subrace traits)</c>.
    /// Returns 0 if no background is selected.
    /// </summary>
    public static int GetExtraLanguageSlots(
        IReadOnlyList<RaceDefinition> races,
        IReadOnlyList<BackgroundDefinition> backgrounds,
        string raceId,
        string subraceId,
        string backgroundId)
    {
        if (string.IsNullOrEmpty(backgroundId)) return 0;

        var bg = backgrounds.FirstOrDefault(b => b.Id == backgroundId);
        int slots = bg?.LanguageCount ?? 0;

        var race = races.FirstOrDefault(r => r.Id == raceId);
        if (race != null)
        {
            slots += race.TraitIds.Count(t => t == "trait:extra-language");
            if (!string.IsNullOrEmpty(subraceId))
            {
                var sub = race.Subraces.FirstOrDefault(s => s.Id == subraceId);
                if (sub != null)
                    slots += sub.TraitIds.Count(t => t == "trait:extra-language");
            }
        }

        return slots;
    }

    /// <summary>
    /// Reconciles a list of chosen extra language IDs against the current fixed languages
    /// and slot count, returning a cleaned list.
    /// <list type="bullet">
    ///   <item>Removes entries that match any fixed language ID.</item>
    ///   <item>Removes duplicate entries.</item>
    ///   <item>Trims the list to <paramref name="slots"/> entries (keeping the first N).</item>
    /// </list>
    /// </summary>
    public static List<string> Reconcile(
        IReadOnlyCollection<string> chosen,
        IReadOnlyCollection<string> fixedIds,
        int slots)
    {
        var result = chosen
            .Where(id => !fixedIds.Contains(id))
            .Distinct()
            .ToList();
        if (result.Count > slots)
            result = result[..slots];
        return result;
    }
}
