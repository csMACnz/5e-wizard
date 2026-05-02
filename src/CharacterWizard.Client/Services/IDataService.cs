using CharacterWizard.Shared.Models;

namespace CharacterWizard.Client.Services;

public interface IDataService
{
    Task<IReadOnlyList<RaceDefinition>> GetRacesAsync();
    Task<IReadOnlyList<ClassDefinition>> GetClassesAsync();
    Task<IReadOnlyList<BackgroundDefinition>> GetBackgroundsAsync();
    Task<IReadOnlyList<SpellDefinition>> GetSpellsAsync();
    Task<IReadOnlyList<EquipmentItemDefinition>> GetEquipmentAsync();
    Task<IReadOnlyList<ClassStartingEquipmentEntry>> GetClassStartingEquipmentAsync();
    Task<IReadOnlyList<string>> GetFullNamesAsync();
    Task<IReadOnlyList<string>> GetGivenNamesAsync();
    Task<IReadOnlyList<string>> GetSurnamesAsync();
    Task<AbilitiesConfig> GetAbilitiesConfigAsync();
}
