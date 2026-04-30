using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

public class CharacterValidatorTests
{
    private static List<RaceDefinition> TestRaces =>
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
    ];

    private static List<ClassDefinition> TestClasses =>
    [
        new ClassDefinition
        {
            Id = "class:fighter",
            SkillChoices = new SkillChoices
            {
                Count = 2,
                Options =
                [
                    "skill:acrobatics", "skill:animal-handling", "skill:athletics",
                    "skill:history", "skill:insight", "skill:intimidation",
                    "skill:perception", "skill:survival",
                ],
            },
            MulticlassPrereqs = new Dictionary<string, int> { ["STR"] = 13 },
        },
        new ClassDefinition
        {
            Id = "class:wizard",
            SkillChoices = new SkillChoices
            {
                Count = 2,
                Options =
                [
                    "skill:arcana", "skill:history", "skill:insight",
                    "skill:investigation", "skill:medicine", "skill:religion",
                ],
            },
            MulticlassPrereqs = new Dictionary<string, int> { ["INT"] = 13 },
        },
    ];

    private static List<BackgroundDefinition> TestBackgrounds =>
    [
        new BackgroundDefinition
        {
            Id = "background:soldier",
            SkillProficiencies = ["skill:athletics", "skill:intimidation"],
        },
    ];

    [Fact]
    public void ValidLevel1HumanFighter_StandardArray_ReturnsValid()
    {
        var validator = new CharacterValidator(TestRaces, TestClasses, TestBackgrounds);
        var character = new Character
        {
            Name = "Valindra",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            RaceId = "race:human",
            BackgroundId = "background:soldier",
            GenerationMethod = GenerationMethod.StandardArray,
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15, RacialBonus = 1 },
                DEX = new AbilityBlock { Base = 14, RacialBonus = 1 },
                CON = new AbilityBlock { Base = 13, RacialBonus = 1 },
                INT = new AbilityBlock { Base = 12, RacialBonus = 1 },
                WIS = new AbilityBlock { Base = 10, RacialBonus = 1 },
                CHA = new AbilityBlock { Base = 8, RacialBonus = 1 },
            },
            Skills = new Dictionary<string, string>
            {
                ["skill:perception"] = "class",
                ["skill:survival"] = "class",
                ["skill:athletics"] = "background",
                ["skill:intimidation"] = "background",
            },
        };

        var result = validator.Validate(character);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void MulticlassPrereqNotMet_ReturnsErrorWithExpectedCode()
    {
        var validator = new CharacterValidator(TestRaces, TestClasses, TestBackgrounds);
        // Fighter + Wizard multiclass; INT = 8+1 = 9 which fails wizard's INT >= 13 prereq
        // Point buy: STR=15(9), DEX=14(7), CON=13(5), INT=8(0), WIS=12(4), CHA=10(2) = 27 ✓
        var character = new Character
        {
            Name = "Valdris",
            TotalLevel = 2,
            Levels =
            [
                new ClassLevel { ClassId = "class:fighter", Level = 1 },
                new ClassLevel { ClassId = "class:wizard", Level = 1 },
            ],
            RaceId = "race:human",
            BackgroundId = "background:soldier",
            GenerationMethod = GenerationMethod.PointBuy,
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15, RacialBonus = 1 }, // Final = 16, meets fighter STR >= 13
                DEX = new AbilityBlock { Base = 14, RacialBonus = 1 },
                CON = new AbilityBlock { Base = 13, RacialBonus = 1 },
                INT = new AbilityBlock { Base = 8, RacialBonus = 1 },  // Final = 9, fails wizard INT >= 13
                WIS = new AbilityBlock { Base = 12, RacialBonus = 1 },
                CHA = new AbilityBlock { Base = 10, RacialBonus = 1 },
            },
            Skills = new Dictionary<string, string>
            {
                ["skill:perception"] = "class",
                ["skill:survival"] = "class",
                ["skill:athletics"] = "background",
                ["skill:intimidation"] = "background",
            },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_MULTICLASS_PREREQ"));
    }

    [Fact]
    public void InvalidAbilityScores_SurfacesAggregatedErrors()
    {
        var validator = new CharacterValidator(TestRaces, TestClasses, TestBackgrounds);
        var character = new Character
        {
            Name = "Broken",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            RaceId = "race:unknown-race",  // invalid
            BackgroundId = "background:soldier",
            GenerationMethod = GenerationMethod.StandardArray,
            AbilityScores = new AbilityScores
            {
                // Invalid standard array (all 10s)
                STR = new AbilityBlock { Base = 10, RacialBonus = 1 },
                DEX = new AbilityBlock { Base = 10, RacialBonus = 1 },
                CON = new AbilityBlock { Base = 10, RacialBonus = 1 },
                INT = new AbilityBlock { Base = 10, RacialBonus = 1 },
                WIS = new AbilityBlock { Base = 10, RacialBonus = 1 },
                CHA = new AbilityBlock { Base = 10, RacialBonus = 1 },
            },
            Skills = new Dictionary<string, string>
            {
                ["skill:perception"] = "class",
                ["skill:survival"] = "class",
                ["skill:athletics"] = "background",
                ["skill:intimidation"] = "background",
            },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        // Should contain both a standard array error and a race error
        Assert.Contains(result.Errors, e => e.Contains("ERR_STDARRAY_INVALID"));
        Assert.Contains(result.Errors, e => e.Contains("ERR_RACE_UNKNOWN"));
        Assert.True(result.Errors.Count >= 2, "Expected at least two aggregated errors.");
    }
}
