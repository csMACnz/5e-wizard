using System.Net.Http.Json;
using CharacterWizard.Shared.Models;

namespace CharacterWizard.Client.Services;

public sealed class DataService : IDataService
{
    private readonly HttpClient _http;
    private List<RaceDefinition>? _races;
    private List<ClassDefinition>? _classes;
    private List<BackgroundDefinition>? _backgrounds;
    private List<SpellDefinition>? _spells;
    private List<EquipmentItemDefinition>? _equipment;
    private List<ClassStartingEquipmentEntry>? _classStartingEquipment;
    private List<FeatDefinition>? _feats;
    private List<LanguageDefinition>? _languages;
    private NamesData? _names;
    private AbilitiesConfig? _abilitiesConfig;

    public DataService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<RaceDefinition>> GetRacesAsync()
    {
        if (_races is null)
        {
            var data = await _http.GetFromJsonAsync<RacesData>("data/races.json");
            _races = data?.Races ?? [];
        }

        return _races;
    }

    public async Task<IReadOnlyList<ClassDefinition>> GetClassesAsync()
    {
        if (_classes is null)
        {
            var data = await _http.GetFromJsonAsync<ClassesData>("data/classes.json");
            _classes = data?.Classes ?? [];
        }

        return _classes;
    }

    public async Task<IReadOnlyList<BackgroundDefinition>> GetBackgroundsAsync()
    {
        if (_backgrounds is null)
        {
            var data = await _http.GetFromJsonAsync<BackgroundsData>("data/backgrounds.json");
            _backgrounds = data?.Backgrounds ?? [];
        }

        return _backgrounds;
    }

    public async Task<IReadOnlyList<SpellDefinition>> GetSpellsAsync()
    {
        if (_spells is null)
        {
            var data = await _http.GetFromJsonAsync<SpellsData>("data/spells.json");
            _spells = data?.Spells ?? [];
        }

        return _spells;
    }

    public async Task<IReadOnlyList<EquipmentItemDefinition>> GetEquipmentAsync()
    {
        if (_equipment is null)
        {
            var data = await _http.GetFromJsonAsync<EquipmentData>("data/equipment.json");
            _equipment = data?.Equipment ?? [];
        }

        return _equipment;
    }

    public async Task<IReadOnlyList<ClassStartingEquipmentEntry>> GetClassStartingEquipmentAsync()
    {
        if (_classStartingEquipment is null)
        {
            var classes = await GetClassesAsync();
            _classStartingEquipment = classes
                .Where(c => c.StartingEquipment != null)
                .Select(c => new ClassStartingEquipmentEntry
                {
                    ClassId = c.Id,
                    StartingWealthRoll = c.StartingEquipment!.StartingWealthRoll,
                    FixedItems = c.StartingEquipment.FixedItems,
                    ChoiceGroups = c.StartingEquipment.ChoiceGroups
                })
                .ToList();
        }

        return _classStartingEquipment;
    }

    public async Task<IReadOnlyList<FeatDefinition>> GetFeatsAsync()
    {
        if (_feats is null)
        {
            var data = await _http.GetFromJsonAsync<FeatsData>("data/feats.json");
            _feats = data?.Feats ?? [];
        }

        return _feats;
    }

    public async Task<IReadOnlyList<LanguageDefinition>> GetLanguagesAsync()
    {
        if (_languages is null)
        {
            var data = await _http.GetFromJsonAsync<LanguagesData>("data/languages.json");
            _languages = data?.Languages ?? [];
        }

        return _languages;
    }

    public async Task<IReadOnlyList<string>> GetFullNamesAsync()
    {
        await EnsureNamesLoadedAsync();
        return _names!.Full.Count > 0 ? _names.Full : [];
    }

    public async Task<IReadOnlyList<string>> GetGivenNamesAsync()
    {
        await EnsureNamesLoadedAsync();
        return _names!.Given.Count > 0 ? _names.Given : [];
    }

    public async Task<IReadOnlyList<string>> GetSurnamesAsync()
    {
        await EnsureNamesLoadedAsync();
        return _names!.Surname.Count > 0 ? _names.Surname : [];
    }

    private async Task EnsureNamesLoadedAsync()
    {
        if (_names is null)
        {
            try
            {
                _names = await _http.GetFromJsonAsync<NamesData>("data/names.json");
            }
            catch
            {
                // Fall back to empty lists; callers will use their built-in fallback arrays.
            }

            _names ??= new NamesData();
        }
    }

    public async Task<AbilitiesConfig> GetAbilitiesConfigAsync()
    {
        if (_abilitiesConfig is null)
        {
            try
            {
                _abilitiesConfig = await _http.GetFromJsonAsync<AbilitiesConfig>("data/abilities.json");
            }
            catch
            {
                // Fall back to default config; validators will use their built-in defaults.
            }

            _abilitiesConfig ??= new AbilitiesConfig();
        }

        return _abilitiesConfig;
    }
}
