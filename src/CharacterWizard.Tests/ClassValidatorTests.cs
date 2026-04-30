using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

public class ClassValidatorTests
{
    private static List<ClassDefinition> CreateTestClasses() =>
    [
        new ClassDefinition
        {
            Id = "class:fighter",
            SkillChoices = new SkillChoices
            {
                Count = 2,
                Options = ["skill:acrobatics", "skill:athletics", "skill:perception", "skill:survival"],
            },
            MulticlassPrereqs = new Dictionary<string, int> { ["STR"] = 13 },
        },
        new ClassDefinition
        {
            Id = "class:wizard",
            SkillChoices = new SkillChoices
            {
                Count = 2,
                Options = ["skill:arcana", "skill:history", "skill:insight", "skill:investigation"],
            },
            MulticlassPrereqs = new Dictionary<string, int> { ["INT"] = 13 },
        },
    ];

    [Fact]
    public void SingleClass_ValidLevel_ReturnsValid()
    {
        var validator = new ClassValidator(CreateTestClasses());
        var character = new Character
        {
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15 },
            },
        };

        var result = validator.Validate(character);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void UnknownClass_ReturnsError()
    {
        var validator = new ClassValidator(CreateTestClasses());
        var character = new Character
        {
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:unknown", Level = 1 }],
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_CLASS_UNKNOWN"));
    }

    [Fact]
    public void TotalLevelMismatch_ReturnsError()
    {
        var validator = new ClassValidator(CreateTestClasses());
        var character = new Character
        {
            TotalLevel = 5, // reported as 5 but only 1 class level
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_CLASS_TOTAL_LEVEL"));
    }

    [Fact]
    public void TotalLevelExceeds20_ReturnsError()
    {
        var validator = new ClassValidator(CreateTestClasses());
        var character = new Character
        {
            TotalLevel = 21,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 21 }],
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_CLASS_LEVEL_EXCEEDS_MAX"));
    }

    [Fact]
    public void Multiclass_MissingPrereq_ReturnsError()
    {
        var validator = new ClassValidator(CreateTestClasses());
        var character = new Character
        {
            TotalLevel = 2,
            Levels =
            [
                new ClassLevel { ClassId = "class:fighter", Level = 1 },
                new ClassLevel { ClassId = "class:wizard", Level = 1 },
            ],
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15 }, // fighter prereq met (>= 13)
                INT = new AbilityBlock { Base = 8 },  // wizard prereq NOT met (< 13)
            },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_MULTICLASS_PREREQ"));
    }

    private static List<ClassDefinition> CreateTestClassesWithSubclasses() =>
    [
        new ClassDefinition
        {
            Id = "class:fighter",
            SkillChoices = new SkillChoices
            {
                Count = 2,
                Options = ["skill:acrobatics", "skill:athletics", "skill:perception", "skill:survival"],
            },
            MulticlassPrereqs = new Dictionary<string, int> { ["STR"] = 13 },
            SubclassLevel = 3,
            SubclassLabel = "Martial Archetype",
            SubclassOptions =
            [
                new SubclassDefinition { Id = "subclass:fighter:champion", DisplayName = "Champion" },
                new SubclassDefinition { Id = "subclass:fighter:battle-master", DisplayName = "Battle Master" },
            ],
        },
        new ClassDefinition
        {
            Id = "class:wizard",
            SkillChoices = new SkillChoices
            {
                Count = 2,
                Options = ["skill:arcana", "skill:history", "skill:insight", "skill:investigation"],
            },
            MulticlassPrereqs = new Dictionary<string, int> { ["INT"] = 13 },
            SubclassLevel = 2,
            SubclassLabel = "Arcane Tradition",
            SubclassOptions =
            [
                new SubclassDefinition { Id = "subclass:wizard:evocation", DisplayName = "School of Evocation" },
            ],
        },
    ];

    [Fact]
    public void SubclassRequired_AtSubclassLevel_NoSubclassSelected_ReturnsError()
    {
        var validator = new ClassValidator(CreateTestClassesWithSubclasses());
        var character = new Character
        {
            TotalLevel = 3,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3, SubclassId = null }],
            AbilityScores = new AbilityScores { STR = new AbilityBlock { Base = 15 } },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SUBCLASS_REQUIRED"));
    }

    [Fact]
    public void SubclassRequired_BelowSubclassLevel_NoSubclassNeeded_IsValid()
    {
        var validator = new ClassValidator(CreateTestClassesWithSubclasses());
        var character = new Character
        {
            TotalLevel = 2,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 2, SubclassId = null }],
            AbilityScores = new AbilityScores { STR = new AbilityBlock { Base = 15 } },
        };

        var result = validator.Validate(character);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void SubclassRequired_ValidSubclassSelected_IsValid()
    {
        var validator = new ClassValidator(CreateTestClassesWithSubclasses());
        var character = new Character
        {
            TotalLevel = 3,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3, SubclassId = "subclass:fighter:champion" }],
            AbilityScores = new AbilityScores { STR = new AbilityBlock { Base = 15 } },
        };

        var result = validator.Validate(character);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void SubclassRequired_UnknownSubclassSelected_ReturnsError()
    {
        var validator = new ClassValidator(CreateTestClassesWithSubclasses());
        var character = new Character
        {
            TotalLevel = 3,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3, SubclassId = "subclass:fighter:unknown" }],
            AbilityScores = new AbilityScores { STR = new AbilityBlock { Base = 15 } },
        };

        var result = validator.Validate(character);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SUBCLASS_UNKNOWN"));
    }
}
