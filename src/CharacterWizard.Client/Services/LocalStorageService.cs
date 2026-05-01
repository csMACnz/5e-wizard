using Microsoft.JSInterop;

namespace CharacterWizard.Client.Services;

/// <summary>
/// Low-level service that wraps browser localStorage via JS interop.
/// </summary>
public sealed class LocalStorageService(IJSRuntime js)
{
    public async Task<string?> GetItemAsync(string key)
    {
        return await js.InvokeAsync<string?>("localStorageHelper.getItem", key);
    }

    public async Task SetItemAsync(string key, string value)
    {
        await js.InvokeVoidAsync("localStorageHelper.setItem", key, value);
    }

    public async Task RemoveItemAsync(string key)
    {
        await js.InvokeVoidAsync("localStorageHelper.removeItem", key);
    }
}
