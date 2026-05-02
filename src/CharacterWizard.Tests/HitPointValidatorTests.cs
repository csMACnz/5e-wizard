using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

/// <summary>
/// Tests for <see cref="HitPointValidator"/>.
/// Validates that ERR_HP_ROLL_OUT_OF_RANGE is produced for out-of-range manual entries
/// and that valid entries produce no errors.
/// </summary>
public class HitPointValidatorTests
{
    private static readonly List<ClassDefinition> TestClasses =
    [
        new ClassDefinition
        {
            Id = "class:fighter",
            DisplayName = "Fighter",
            HitDie = 10,
            SavingThrows = ["STR", "CON"],
            SkillChoices = new SkillChoices { Count = 2, Options = [] },
            MulticlassPrereqs = new Dictionary<string, int>(),
            SubclassLevel = 3,
        },
        new ClassDefinition
        {
            Id = "class:wizard",
            DisplayName = "Wizard",
            HitDie = 6,
            SavingThrows = ["INT", "WIS"],
            SkillChoices = new SkillChoices { Count = 2, Options = [] },
            MulticlassPrereqs = new Dictionary<string, int>(),
            SubclassLevel = 2,
        },
    ];

    private static HitPointValidator CreateValidator() => new(TestClasses);

    // ── Valid entries ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyEntries_NoErrors()
    {
        var character = new Character { HitPointEntries = [] };
        var result = CreateValidator().Validate(character);
        Assert.Empty(result.Errors);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Validate_FighterD10_ValidRange_NoErrors(int dieRollValue)
    {
        var character = new Character
        {
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 2, Method = "manual", DieRollValue = dieRollValue },
            ],
        };

        var result = CreateValidator().Validate(character);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public void Validate_WizardD6_ValidRange_NoErrors(int dieRollValue)
    {
        var character = new Character
        {
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:wizard", ClassLevel = 2, Method = "manual", DieRollValue = dieRollValue },
            ],
        };

        var result = CreateValidator().Validate(character);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_AverageMethod_ValidValues_NoErrors()
    {
        var character = new Character
        {
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 1, Method = "average", DieRollValue = 10 },
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 2, Method = "average", DieRollValue = 6 },
            ],
        };

        var result = CreateValidator().Validate(character);
        Assert.Empty(result.Errors);
    }

    // ── Invalid entries ────────────────────────────────────────────────────

    [Fact]
    public void Validate_DieRollValueZero_ProducesError()
    {
        var character = new Character
        {
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 2, Method = "manual", DieRollValue = 0 },
            ],
        };

        var result = CreateValidator().Validate(character);
        Assert.Contains(result.Errors, e => e.Contains("ERR_HP_ROLL_OUT_OF_RANGE"));
    }

    [Fact]
    public void Validate_DieRollValueExceedsHitDie_ProducesError()
    {
        // Fighter d10: value of 11 is out of range
        var character = new Character
        {
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 2, Method = "manual", DieRollValue = 11 },
            ],
        };

        var result = CreateValidator().Validate(character);
        Assert.Contains(result.Errors, e => e.Contains("ERR_HP_ROLL_OUT_OF_RANGE"));
        Assert.Contains(result.Errors, e => e.Contains("Fighter") && e.Contains("level 2"));
    }

    [Fact]
    public void Validate_WizardD6_ValueSeven_ProducesError()
    {
        var character = new Character
        {
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:wizard", ClassLevel = 3, Method = "manual", DieRollValue = 7 },
            ],
        };

        var result = CreateValidator().Validate(character);
        Assert.Contains(result.Errors, e => e.Contains("ERR_HP_ROLL_OUT_OF_RANGE"));
        Assert.Contains(result.Errors, e => e.Contains("Wizard") && e.Contains("level 3"));
    }

    [Fact]
    public void Validate_MultipleEntries_OneInvalid_ProducesOneError()
    {
        var character = new Character
        {
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 1, Method = "average", DieRollValue = 10 },
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 2, Method = "manual", DieRollValue = 5 },
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 3, Method = "manual", DieRollValue = 11 }, // invalid
            ],
        };

        var result = CreateValidator().Validate(character);
        Assert.Single(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("ERR_HP_ROLL_OUT_OF_RANGE"));
    }

    [Fact]
    public void Validate_MultipleEntries_AllInvalid_ProducesMultipleErrors()
    {
        var character = new Character
        {
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 2, Method = "manual", DieRollValue = 0 },
                new HitPointEntry { ClassId = "class:wizard", ClassLevel = 2, Method = "manual", DieRollValue = 7 },
            ],
        };

        var result = CreateValidator().Validate(character);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, e => Assert.Contains("ERR_HP_ROLL_OUT_OF_RANGE", e));
    }

    [Fact]
    public void Validate_UnknownClass_FallsBackToD8()
    {
        // Unknown class has a fallback hitDie of 8; value of 9 should be out of range
        var character = new Character
        {
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:unknown", ClassLevel = 2, Method = "manual", DieRollValue = 9 },
            ],
        };

        var result = CreateValidator().Validate(character);
        Assert.Contains(result.Errors, e => e.Contains("ERR_HP_ROLL_OUT_OF_RANGE"));
    }

    [Fact]
    public void Validate_NegativeValue_ProducesError()
    {
        var character = new Character
        {
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 2, Method = "manual", DieRollValue = -1 },
            ],
        };

        var result = CreateValidator().Validate(character);
        Assert.Contains(result.Errors, e => e.Contains("ERR_HP_ROLL_OUT_OF_RANGE"));
    }

    // ── Integration with CharacterValidator ───────────────────────────────

    [Fact]
    public void CharacterValidator_ValidHpEntries_NoHpErrors()
    {
        var character = new Character
        {
            RaceId = "race:human",
            BackgroundId = "bg:soldier",
            GenerationMethod = GenerationMethod.Roll,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3 }],
            TotalLevel = 3,
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15 },
                DEX = new AbilityBlock { Base = 14 },
                CON = new AbilityBlock { Base = 13 },
                INT = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
                CHA = new AbilityBlock { Base = 8 },
            },
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 1, Method = "average", DieRollValue = 10 },
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 2, Method = "average", DieRollValue = 6 },
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 3, Method = "manual", DieRollValue = 8 },
            ],
        };

        var validator = new CharacterValidator([], TestClasses, []);
        var result = validator.Validate(character);
        Assert.DoesNotContain(result.Errors, e => e.Contains("ERR_HP_ROLL_OUT_OF_RANGE"));
    }

    [Fact]
    public void CharacterValidator_InvalidHpEntry_ProducesError()
    {
        var character = new Character
        {
            RaceId = "race:human",
            BackgroundId = "bg:soldier",
            GenerationMethod = GenerationMethod.Roll,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 2 }],
            TotalLevel = 2,
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15 },
                DEX = new AbilityBlock { Base = 14 },
                CON = new AbilityBlock { Base = 13 },
                INT = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
                CHA = new AbilityBlock { Base = 8 },
            },
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 1, Method = "average", DieRollValue = 10 },
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 2, Method = "manual", DieRollValue = 11 }, // invalid: >10
            ],
        };

        var validator = new CharacterValidator([], TestClasses, []);
        var result = validator.Validate(character);
        Assert.Contains(result.Errors, e => e.Contains("ERR_HP_ROLL_OUT_OF_RANGE"));
    }
}
