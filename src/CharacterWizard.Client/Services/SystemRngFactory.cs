using CharacterWizard.Shared.Utilities;

namespace CharacterWizard.Client.Services;

/// <summary>
/// <see cref="IRngFactory"/> that creates a new <see cref="SystemRng"/> (backed by a freshly
/// constructed <see cref="System.Random"/>) for every call to <see cref="Create"/>.
/// Each returned instance is independent, so separate randomisation actions cannot
/// interfere with one another.
/// </summary>
public sealed class SystemRngFactory : IRngFactory
{
    public IRng Create() => new SystemRng(new Random());
}
