namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates character meta fields: character name, player name, and campaign name.
/// </summary>
public static class MetaValidator
{
    /// <summary>Maximum allowed length for a character name.</summary>
    public const int MaxNameLength = 100;

    /// <summary>Maximum allowed length for a player name.</summary>
    public const int MaxPlayerNameLength = 100;

    /// <summary>Maximum allowed length for a campaign name.</summary>
    public const int MaxCampaignNameLength = 100;

    /// <summary>
    /// Validates the character name and optional player/campaign name fields.
    /// </summary>
    /// <param name="name">The character name (required).</param>
    /// <param name="playerName">The player name (optional; null means not provided).</param>
    /// <param name="campaignName">The campaign name (optional; null means not provided).</param>
    public static ValidationResult Validate(string? name, string? playerName = null, string? campaignName = null)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(name))
        {
            result.Errors.Add("ERR_META_NAME_REQUIRED: Character name is required.");
        }
        else if (name.Length > MaxNameLength)
        {
            result.Errors.Add(
                $"ERR_META_NAME_TOO_LONG: Character name must be {MaxNameLength} characters or fewer " +
                $"(got {name.Length}).");
        }

        if (!string.IsNullOrEmpty(playerName) && playerName.Length > MaxPlayerNameLength)
        {
            result.Errors.Add(
                $"ERR_META_PLAYER_NAME_TOO_LONG: Player name must be {MaxPlayerNameLength} characters or fewer " +
                $"(got {playerName.Length}).");
        }

        if (!string.IsNullOrEmpty(campaignName) && campaignName.Length > MaxCampaignNameLength)
        {
            result.Errors.Add(
                $"ERR_META_CAMPAIGN_NAME_TOO_LONG: Campaign name must be {MaxCampaignNameLength} characters or fewer " +
                $"(got {campaignName.Length}).");
        }

        return result;
    }
}
