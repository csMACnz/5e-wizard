using System.Net.Http.Json;
using CharacterWizard.Shared.Models;

namespace CharacterWizard.Client.Services;

public sealed class DataService : IDataService
{
    private readonly HttpClient _http;
    private List<RaceDefinition>? _races;
    private List<ClassDefinition>? _classes;
    private List<BackgroundDefinition>? _backgrounds;

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
}
