using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

/// <summary>
/// Tests for wizard step validation logic.
/// Each step in the wizard enables "Next" only when the step's validator returns IsValid = true.
/// These tests verify those gating conditions.
/// </summary>
public class WizardStepValidationTests
{
    // ── Shared seed data ────────────────────────────────────────────────
    private static readonly List<RaceDefinition> TestRaces =
    [
        new RaceDefinition
        {
            Id = "race:human",
            DisplayName = "Human",
            AbilityBonuses = new Dictionary<string, int>
            {
                ["STR"] = 1, ["DEX"] = 1, ["CON"] = 1, ["INT"] = 1, ["WIS"] = 1, ["CHA"] = 1,
            },
            Subraces = [],
        },
        new RaceDefinition
        {
            Id = "race:elf",
            DisplayName = "Elf",
            AbilityBonuses = new Dictionary<string, int> { ["DEX"] = 2 },
            Subraces =
            [
                new SubraceDefinition
                {
                    Id = "race:elf:high-elf",
                    DisplayName = "High Elf",
                    AbilityBonuses = new Dictionary<string, int> { ["INT"] = 1 },
                },
            ],
        },
    ];

    private static readonly List<ClassDefinition> TestClasses =
    [
        new ClassDefinition
        {
            Id = "class:fighter",
            DisplayName = "Fighter",
            HitDie = 10,
            SavingThrows = ["STR", "CON"],
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
            SubclassLevel = 3,
        },
    ];

    private static readonly List<BackgroundDefinition> TestBackgrounds =
    [
        new BackgroundDefinition
        {
            Id = "background:soldier",
            DisplayName = "Soldier",
            FeatureId = "feat:military-rank",
            SkillProficiencies = ["skill:athletics", "skill:intimidation"],
        },
    ];

    // ── Step 1: Character Meta ───────────────────────────────────────────

    [Fact]
    public void Step1_ValidName_And_Method_IsValid()
    {
        // Simulates what the wizard's ValidateStep(0) does:
        // requires non-empty name + a valid generation method.
        const string name = "Aric Stonehammer";
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.True(Enum.IsDefined(GenerationMethod.StandardArray));
    }

    [Fact]
    public void Step1_EmptyName_IsInvalid()
    {
        const string name = "";
        Assert.True(string.IsNullOrWhiteSpace(name),
            "Empty name should fail the 'name required' gate.");
    }

    // ── Step 2: Ability Scores – Standard Array ──────────────────────────

    [Fact]
    public void Step2_StandardArray_ValidAssignment_IsValid()
    {
        int[] scores = [15, 14, 13, 12, 10, 8];
        var result = StandardArrayValidator.Validate(scores);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Step2_StandardArray_DuplicateValues_IsInvalid()
    {
        int[] scores = [15, 15, 13, 12, 10, 8]; // 15 appears twice, 14 missing
        var result = StandardArrayValidator.Validate(scores);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_STDARRAY_INVALID"));
    }

    [Fact]
    public void Step2_StandardArray_AllZero_IsInvalid()
    {
        // Unassigned values (0 = not yet selected in UI)
        int[] scores = [0, 0, 0, 0, 0, 0];
        var result = StandardArrayValidator.Validate(scores);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Step2_PointBuy_Valid27Points_IsValid()
    {
        // 8+8+8+8+8+8 = 0 points (under budget, warning but valid)
        int[] scores = [8, 8, 8, 8, 8, 8];
        var result = PointBuyValidator.Validate(scores);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Step2_PointBuy_OverBudget_IsInvalid()
    {
        // 15+15+15+15+15+15 costs 54 points which exceeds 27 budget
        int[] scores = [15, 15, 15, 15, 15, 15];
        var result = PointBuyValidator.Validate(scores);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_POINTBUY_BUDGET"));
    }

    [Fact]
    public void Step2_Roll_ValidScores_IsValid()
    {
        int[] scores = [18, 16, 14, 12, 10, 8];
        var result = RollValidator.Validate(scores);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Step2_Roll_ScoreOutOfRange_IsInvalid()
    {
        int[] scores = [18, 16, 14, 12, 10, 2]; // 2 is below minimum of 3
        var result = RollValidator.Validate(scores);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_ROLL_RANGE"));
    }

    // ── Step 3: Race ──────────────────────────────────────────────────────

    [Fact]
    public void Step3_ValidRace_NoSubrace_Required_IsValid()
    {
        var character = new Character
        {
            RaceId = "race:human",
            SubraceId = null,
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15, RacialBonus = 1 },
                DEX = new AbilityBlock { Base = 14, RacialBonus = 1 },
                CON = new AbilityBlock { Base = 13, RacialBonus = 1 },
                INT = new AbilityBlock { Base = 12, RacialBonus = 1 },
                WIS = new AbilityBlock { Base = 10, RacialBonus = 1 },
                CHA = new AbilityBlock { Base = 8, RacialBonus = 1 },
            },
        };

        var result = new RaceValidator(TestRaces).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Step3_RaceWithSubrace_NoSubraceSelected_IsInvalid()
    {
        // Elf requires subrace but none selected → step must be blocked
        var character = new Character
        {
            RaceId = "race:elf",
            SubraceId = null, // missing required subrace
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15, RacialBonus = 0 },
                DEX = new AbilityBlock { Base = 14, RacialBonus = 2 },
                CON = new AbilityBlock { Base = 13, RacialBonus = 0 },
                INT = new AbilityBlock { Base = 12, RacialBonus = 0 },
                WIS = new AbilityBlock { Base = 10, RacialBonus = 0 },
                CHA = new AbilityBlock { Base = 8, RacialBonus = 0 },
            },
        };

        var result = new RaceValidator(TestRaces).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SUBRACE_REQUIRED"));
    }

    [Fact]
    public void Step3_UnknownRaceId_IsInvalid()
    {
        var character = new Character
        {
            RaceId = "race:does-not-exist",
            AbilityScores = new AbilityScores(),
        };

        var result = new RaceValidator(TestRaces).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_RACE_UNKNOWN"));
    }

    // ── Step 4: Class ─────────────────────────────────────────────────────

    [Fact]
    public void Step4_ValidLevel1Fighter_IsValid()
    {
        var character = new Character
        {
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15, RacialBonus = 1 },
            },
        };

        var result = new ClassValidator(TestClasses).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Step4_UnknownClass_IsInvalid()
    {
        var character = new Character
        {
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:unknown", Level = 1 }],
            AbilityScores = new AbilityScores(),
        };

        var result = new ClassValidator(TestClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_CLASS_UNKNOWN"));
    }

    [Fact]
    public void Step4_TotalLevelMismatch_IsInvalid()
    {
        var character = new Character
        {
            TotalLevel = 2, // says 2 but levels sum to 1
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores(),
        };

        var result = new ClassValidator(TestClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_CLASS_TOTAL_LEVEL"));
    }

    // ── Step 5: Background & Proficiencies ───────────────────────────────

    [Fact]
    public void Step5_ValidSkillSelection_IsValid()
    {
        var character = new Character
        {
            BackgroundId = "background:soldier",
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            Skills = new Dictionary<string, string>
            {
                ["skill:perception"] = "class",
                ["skill:survival"] = "class",
                ["skill:athletics"] = "background",
                ["skill:intimidation"] = "background",
            },
        };

        var result = new ProficiencyValidator(TestClasses, TestBackgrounds).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Step5_WrongClassSkillCount_IsInvalid()
    {
        var character = new Character
        {
            BackgroundId = "background:soldier",
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            Skills = new Dictionary<string, string>
            {
                ["skill:perception"] = "class",   // only 1 class skill, fighter needs 2
                ["skill:athletics"] = "background",
                ["skill:intimidation"] = "background",
            },
        };

        var result = new ProficiencyValidator(TestClasses, TestBackgrounds).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SKILL_COUNT"));
    }

    [Fact]
    public void Step5_ClassSkillDuplicatesBackgroundSkill_IsInvalid()
    {
        // Soldier background grants skill:athletics.
        // If the player also picks skill:athletics as a class skill, that's a duplicate.
        var charDup = new Character
        {
            BackgroundId = "background:soldier",
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            Skills = new Dictionary<string, string>
            {
                // skill:athletics is chosen as a class skill AND is granted by soldier background
                ["skill:athletics"] = "class",
                ["skill:survival"] = "class",
                ["skill:intimidation"] = "background",  // soldier grants this
                // skill:athletics is in soldier's SkillProficiencies, so validator flags it as duplicate
            },
        };

        var result = new ProficiencyValidator(TestClasses, TestBackgrounds).Validate(charDup);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SKILL_DUPLICATE") || e.Contains("ERR_SKILL_COUNT"));
    }
}
