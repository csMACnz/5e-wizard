using System.Text.Json;
using CharacterWizard.Shared.Models;

namespace CharacterWizard.Client.Services;

/// <summary>
/// Manages character sessions in browser local storage.
/// Each session is stored under the key "5ew_session_&lt;sessionId&gt;".
/// An index of all session IDs is maintained under "5ew_sessions".
/// </summary>
public sealed class CharacterSessionService(LocalStorageService localStorage)
{
    private const string IndexKey = "5ew_sessions";
    private const string SessionKeyPrefix = "5ew_session_";

    /// <summary>
    /// The storage format version this build can read and write.
    /// Sessions persisted with a different version are treated as unreadable
    /// and returned as <see langword="null"/> so the caller can skip them gracefully.
    /// </summary>
    public const int SupportedSchemaVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
    };

    public async Task SaveSessionAsync(CharacterSession session)
    {
        session.SchemaVersion = SupportedSchemaVersion;
        session.LastModifiedAt = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(session, SerializerOptions);
        await localStorage.SetItemAsync(SessionKeyPrefix + session.SessionId, json);

        await AddToIndexAsync(session.SessionId);
    }

    public async Task<CharacterSession?> LoadSessionAsync(string sessionId)
    {
        var json = await localStorage.GetItemAsync(SessionKeyPrefix + sessionId);
        if (json is null) return null;

        try
        {
            var session = JsonSerializer.Deserialize<CharacterSession>(json, SerializerOptions);
            if (session is null || session.SchemaVersion != SupportedSchemaVersion)
                return null;

            return session;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<CharacterSession>> LoadAllSessionsAsync()
    {
        var ids = await GetIndexAsync();
        var sessions = new List<CharacterSession>();

        foreach (var id in ids)
        {
            var session = await LoadSessionAsync(id);
            if (session is not null)
                sessions.Add(session);
        }

        return [.. sessions.OrderByDescending(s => s.LastModifiedAt)];
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        await localStorage.RemoveItemAsync(SessionKeyPrefix + sessionId);
        await RemoveFromIndexAsync(sessionId);
    }

    public async Task<bool> HasSessionsAsync()
    {
        var ids = await GetIndexAsync();
        return ids.Count > 0;
    }

    private async Task<List<string>> GetIndexAsync()
    {
        var json = await localStorage.GetItemAsync(IndexKey);
        if (json is null) return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, SerializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private async Task AddToIndexAsync(string sessionId)
    {
        var ids = await GetIndexAsync();
        if (!ids.Contains(sessionId))
            ids.Add(sessionId);

        await localStorage.SetItemAsync(IndexKey, JsonSerializer.Serialize(ids, SerializerOptions));
    }

    private async Task RemoveFromIndexAsync(string sessionId)
    {
        var ids = await GetIndexAsync();
        ids.Remove(sessionId);
        await localStorage.SetItemAsync(IndexKey, JsonSerializer.Serialize(ids, SerializerOptions));
    }
}
