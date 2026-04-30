using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CharacterWizard.Client.Services;

public sealed class BuildInfo
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = "dev";

    [JsonPropertyName("commitSha")]
    public string CommitSha { get; init; } = "local";

    [JsonPropertyName("buildDate")]
    public string BuildDate { get; init; } = "local";

    /// <summary>Returns a short (7-char) commit SHA if longer, otherwise the raw value.</summary>
    public string ShortSha => CommitSha.Length > 7 ? CommitSha[..7] : CommitSha;
}

public sealed class BuildInfoService
{
    private readonly HttpClient _http;
    private BuildInfo? _info;

    public BuildInfoService(HttpClient http)
    {
        _http = http;
    }

    public async Task<BuildInfo> GetAsync()
    {
        if (_info is null)
        {
            try
            {
                _info = await _http.GetFromJsonAsync<BuildInfo>("build-info.json")
                        ?? new BuildInfo();
            }
            catch
            {
                _info = new BuildInfo();
            }
        }

        return _info;
    }
}
