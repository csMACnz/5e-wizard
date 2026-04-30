using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

public class ProficiencyValidatorTests
{
    private static List<ClassDefinition> CreateTestClasses() =>
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
            Id = "class:bard",
            SkillChoices = new SkillChoices
            {
                Count = 3,
                Options = ["skill:any"],
            },
            MulticlassPrereqs = new Dictionary<string, int> { ["CHA"] = 13 },
        },
    ];

    private static List<BackgroundDefinition> CreateTestBackgrounds() =>
    [
        new BackgroundDefinition
        {
            Id = "background:soldier",
            SkillProficiencies = ["skill:athletics", "skill:intimidation"],
        },
    ];

    [Fact]
    public void ValidSelections_ReturnsValid()
    {
        var validator = new ProficiencyValidator(CreateTestClasses(), CreateTestBackgrounds());
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            BackgroundId = "background:soldier",
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
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void TooManyClassSkills_ReturnsError()
    {
        var validator = new ProficiencyValidator(CreateTestClasses(), CreateTestBackgrounds());
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            BackgroundId = "background:soldier",
            Skills = new Dictionary<string, string>
            {
                ["skill:perception"] = "class",
                ["skill:survival"] = "class",
                ["skill:acrobatics"] = "class", // 3 class skills but only 2 allowed
                ["skill:athletics"] = "background",
                ["skill:intimidation"] = "background",
            },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SKILL_COUNT"));
    }

    [Fact]
    public void TooFewClassSkills_ReturnsError()
    {
        var validator = new ProficiencyValidator(CreateTestClasses(), CreateTestBackgrounds());
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            BackgroundId = "background:soldier",
            Skills = new Dictionary<string, string>
            {
                ["skill:perception"] = "class", // only 1 class skill, need 2
                ["skill:athletics"] = "background",
                ["skill:intimidation"] = "background",
            },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SKILL_COUNT"));
    }

    [Fact]
    public void DisallowedClassSkill_ReturnsError()
    {
        var validator = new ProficiencyValidator(CreateTestClasses(), CreateTestBackgrounds());
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            BackgroundId = "background:soldier",
            Skills = new Dictionary<string, string>
            {
                ["skill:perception"] = "class",
                ["skill:arcana"] = "class", // arcana is not in fighter's allowed list
                ["skill:athletics"] = "background",
                ["skill:intimidation"] = "background",
            },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SKILL_NOT_ALLOWED"));
    }

    [Fact]
    public void DuplicateSkillFromBackground_ReturnsError()
    {
        var validator = new ProficiencyValidator(CreateTestClasses(), CreateTestBackgrounds());
        // skill:athletics is in background:soldier grants AND chosen as class skill
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            BackgroundId = "background:soldier",
            Skills = new Dictionary<string, string>
            {
                ["skill:athletics"] = "class",  // also granted by background:soldier — duplicate!
                ["skill:perception"] = "class",
                ["skill:intimidation"] = "background",
            },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SKILL_DUPLICATE"));
    }

    [Fact]
    public void AnySkillClass_AcceptsArbitrarySkills_ReturnsValid()
    {
        var validator = new ProficiencyValidator(CreateTestClasses(), CreateTestBackgrounds());
        // Bard allows skill:any — any 3 skills should be accepted
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:bard", Level = 1 }],
            BackgroundId = "background:soldier",
            Skills = new Dictionary<string, string>
            {
                ["skill:arcana"] = "class",
                ["skill:history"] = "class",
                ["skill:deception"] = "class",
                ["skill:athletics"] = "background",
                ["skill:intimidation"] = "background",
            },
        };

        var result = validator.Validate(character);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
    }
}
