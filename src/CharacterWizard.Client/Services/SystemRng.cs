using CharacterWizard.Shared.Utilities;

namespace CharacterWizard.Client.Services;

/// <summary>
/// <see cref="IRng"/> implementation backed by a <see cref="System.Random"/> instance.
/// </summary>
internal sealed class SystemRng(Random random) : IRng
{
    public int Next(int maxValue) => random.Next(maxValue);
    public int Next(int minValue, int maxValue) => random.Next(minValue, maxValue);
}
