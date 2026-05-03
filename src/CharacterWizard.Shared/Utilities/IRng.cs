namespace CharacterWizard.Shared.Utilities;

/// <summary>
/// Abstraction over a random number generator.
/// Implementations must be action-scoped: each user-initiated randomisation
/// action should use its own independent instance so that actions cannot
/// affect one another's outcomes.
/// </summary>
public interface IRng
{
    /// <summary>Returns a non-negative random integer less than <paramref name="maxValue"/>.</summary>
    int Next(int maxValue);

    /// <summary>Returns a random integer in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>).</summary>
    int Next(int minValue, int maxValue);
}
