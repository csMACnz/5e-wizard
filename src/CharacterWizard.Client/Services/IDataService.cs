using CharacterWizard.Shared.Models;

namespace CharacterWizard.Client.Services;

public interface IDataService
{
    Task<IReadOnlyList<RaceDefinition>> GetRacesAsync();
    Task<IReadOnlyList<ClassDefinition>> GetClassesAsync();
    Task<IReadOnlyList<BackgroundDefinition>> GetBackgroundsAsync();
    Task<IReadOnlyList<SpellDefinition>> GetSpellsAsync();
    Task<IReadOnlyList<EquipmentItemDefinition>> GetEquipmentAsync();
}
