using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

/// <summary>
/// Tests for MetaValidator: character name, player name, and campaign name validation.
/// </summary>
public class MetaValidatorTests
{
    // ── Character name — required ─────────────────────────────────────────

    [Fact]
    public void CharacterName_Null_IsInvalid()
    {
        var result = MetaValidator.Validate(null);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_META_NAME_REQUIRED"));
    }

    [Fact]
    public void CharacterName_Empty_IsInvalid()
    {
        var result = MetaValidator.Validate(string.Empty);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_META_NAME_REQUIRED"));
    }

    [Fact]
    public void CharacterName_Whitespace_IsInvalid()
    {
        var result = MetaValidator.Validate("   ");
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_META_NAME_REQUIRED"));
    }

    // ── Character name — length ───────────────────────────────────────────

    [Fact]
    public void CharacterName_ExactlyMaxLength_IsValid()
    {
        var name = new string('a', MetaValidator.MaxNameLength);
        var result = MetaValidator.Validate(name);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void CharacterName_OneOverMaxLength_IsInvalid()
    {
        var name = new string('a', MetaValidator.MaxNameLength + 1);
        var result = MetaValidator.Validate(name);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_META_NAME_TOO_LONG"));
    }

    [Fact]
    public void CharacterName_Normal_IsValid()
    {
        var result = MetaValidator.Validate("Aric Stonehammer");
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    // ── Player name — optional, length-gated ─────────────────────────────

    [Fact]
    public void PlayerName_Null_IsValid()
    {
        var result = MetaValidator.Validate("Valid Name", playerName: null);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void PlayerName_Empty_IsValid()
    {
        // Empty string is treated as "not provided"
        var result = MetaValidator.Validate("Valid Name", playerName: string.Empty);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void PlayerName_ExactlyMaxLength_IsValid()
    {
        var playerName = new string('b', MetaValidator.MaxPlayerNameLength);
        var result = MetaValidator.Validate("Valid Name", playerName: playerName);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void PlayerName_OneOverMaxLength_IsInvalid()
    {
        var playerName = new string('b', MetaValidator.MaxPlayerNameLength + 1);
        var result = MetaValidator.Validate("Valid Name", playerName: playerName);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_META_PLAYER_NAME_TOO_LONG"));
    }

    // ── Campaign name — optional, length-gated ────────────────────────────

    [Fact]
    public void CampaignName_Null_IsValid()
    {
        var result = MetaValidator.Validate("Valid Name", campaignName: null);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void CampaignName_Empty_IsValid()
    {
        var result = MetaValidator.Validate("Valid Name", campaignName: string.Empty);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void CampaignName_ExactlyMaxLength_IsValid()
    {
        var campaignName = new string('c', MetaValidator.MaxCampaignNameLength);
        var result = MetaValidator.Validate("Valid Name", campaignName: campaignName);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void CampaignName_OneOverMaxLength_IsInvalid()
    {
        var campaignName = new string('c', MetaValidator.MaxCampaignNameLength + 1);
        var result = MetaValidator.Validate("Valid Name", campaignName: campaignName);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_META_CAMPAIGN_NAME_TOO_LONG"));
    }

    // ── Multiple violations ───────────────────────────────────────────────

    [Fact]
    public void AllFieldsTooLong_ReportsAllErrors()
    {
        var name = new string('a', MetaValidator.MaxNameLength + 1);
        var playerName = new string('b', MetaValidator.MaxPlayerNameLength + 1);
        var campaignName = new string('c', MetaValidator.MaxCampaignNameLength + 1);

        var result = MetaValidator.Validate(name, playerName, campaignName);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_META_NAME_TOO_LONG"));
        Assert.Contains(result.Errors, e => e.Contains("ERR_META_PLAYER_NAME_TOO_LONG"));
        Assert.Contains(result.Errors, e => e.Contains("ERR_META_CAMPAIGN_NAME_TOO_LONG"));
    }
}
