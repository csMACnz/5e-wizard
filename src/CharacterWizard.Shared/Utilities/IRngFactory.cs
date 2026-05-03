namespace CharacterWizard.Shared.Utilities;

/// <summary>
/// Factory that creates a fresh <see cref="IRng"/> instance for each user-initiated
/// randomisation action. Creating one instance per action keeps randomisation outcomes
/// independent and makes the system deterministic under test.
/// </summary>
public interface IRngFactory
{
    /// <summary>Creates and returns a new, independent <see cref="IRng"/> instance.</summary>
    IRng Create();
}
