using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

public class RaceValidatorTests
{
    private static List<RaceDefinition> CreateTestRaces() =>
    [
        new RaceDefinition
        {
            Id = "race:human",
            AbilityBonuses = new Dictionary<string, int>
            {
                ["STR"] = 1, ["DEX"] = 1, ["CON"] = 1, ["INT"] = 1, ["WIS"] = 1, ["CHA"] = 1,
            },
            Subraces = [],
        },
        new RaceDefinition
        {
            Id = "race:dwarf",
            AbilityBonuses = new Dictionary<string, int> { ["CON"] = 2 },
            Subraces =
            [
                new SubraceDefinition
                {
                    Id = "subrace:hill-dwarf",
                    AbilityBonuses = new Dictionary<string, int> { ["WIS"] = 1 },
                },
            ],
        },
    ];

    [Fact]
    public void ValidRace_Human_NoSubrace_ReturnsValid()
    {
        var validator = new RaceValidator(CreateTestRaces());
        var character = new Character
        {
            RaceId = "race:human",
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { RacialBonus = 1 },
                DEX = new AbilityBlock { RacialBonus = 1 },
                CON = new AbilityBlock { RacialBonus = 1 },
                INT = new AbilityBlock { RacialBonus = 1 },
                WIS = new AbilityBlock { RacialBonus = 1 },
                CHA = new AbilityBlock { RacialBonus = 1 },
            },
        };

        var result = validator.Validate(character);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidRace_DwarfWithSubrace_ReturnsValid()
    {
        var validator = new RaceValidator(CreateTestRaces());
        var character = new Character
        {
            RaceId = "race:dwarf",
            SubraceId = "subrace:hill-dwarf",
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { RacialBonus = 2 },
                WIS = new AbilityBlock { RacialBonus = 1 },
            },
        };

        var result = validator.Validate(character);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void UnknownRaceId_ReturnsError()
    {
        var validator = new RaceValidator(CreateTestRaces());
        var character = new Character { RaceId = "race:unknown" };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_RACE_UNKNOWN"));
    }

    [Fact]
    public void MissingRequiredSubrace_ReturnsError()
    {
        var validator = new RaceValidator(CreateTestRaces());
        var character = new Character
        {
            RaceId = "race:dwarf",
            SubraceId = null,
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { RacialBonus = 2 },
            },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SUBRACE_REQUIRED"));
    }

    [Fact]
    public void BonusMismatch_TooHigh_ReturnsError()
    {
        var validator = new RaceValidator(CreateTestRaces());
        // Human has all +1; give STR a +2 instead
        var character = new Character
        {
            RaceId = "race:human",
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { RacialBonus = 2 }, // should be 1
                DEX = new AbilityBlock { RacialBonus = 1 },
                CON = new AbilityBlock { RacialBonus = 1 },
                INT = new AbilityBlock { RacialBonus = 1 },
                WIS = new AbilityBlock { RacialBonus = 1 },
                CHA = new AbilityBlock { RacialBonus = 1 },
            },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_RACE_BONUS_MISMATCH"));
    }

    [Fact]
    public void BonusMismatch_Missing_ReturnsError()
    {
        var validator = new RaceValidator(CreateTestRaces());
        // Human has all +1; leave STR at 0
        var character = new Character
        {
            RaceId = "race:human",
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { RacialBonus = 0 }, // should be 1
                DEX = new AbilityBlock { RacialBonus = 1 },
                CON = new AbilityBlock { RacialBonus = 1 },
                INT = new AbilityBlock { RacialBonus = 1 },
                WIS = new AbilityBlock { RacialBonus = 1 },
                CHA = new AbilityBlock { RacialBonus = 1 },
            },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_RACE_BONUS_MISMATCH"));
    }
}
