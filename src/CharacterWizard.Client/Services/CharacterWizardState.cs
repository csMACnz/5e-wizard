using CharacterWizard.Shared.Models;

namespace CharacterWizard.Client.Services;

/// <summary>
/// Scoped DI service that persists in-progress character data across wizard steps.
/// </summary>
public sealed class CharacterWizardState
{
    public string SessionId { get; set; } = string.Empty;
    public Character Character { get; set; } = new();
    public int ActiveStep { get; set; } = 0;

    public event Action? OnChange;

    public void NotifyStateChanged() => OnChange?.Invoke();
}
