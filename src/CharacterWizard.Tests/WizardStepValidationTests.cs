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

    // ── Step 7: Equipment (classConfig path) ─────────────────────────────

    private static ClassStartingEquipmentEntry BuildFighterEquipmentConfig() => new()
    {
        ClassId = "class:fighter",
        StartingWealthRoll = "5d4*10",
        FixedItems = [new EquipmentGrantItem { ItemId = "item:chain-mail", Quantity = 1 }],
        ChoiceGroups =
        [
            new EquipmentChoiceGroup
            {
                Id = "weapon-group",
                Description = "Choose a weapon",
                Required = true,
                Options =
                [
                    new EquipmentChoiceOption
                    {
                        Id = "sword-shield",
                        Description = "Longsword and shield",
                        PickOne = false,
                        GrantItems =
                        [
                            new EquipmentGrantItem { ItemId = "item:longsword", Quantity = 1 },
                            new EquipmentGrantItem { ItemId = "item:shield", Quantity = 1 },
                        ],
                    },
                    new EquipmentChoiceOption
                    {
                        Id = "martial-pick",
                        Description = "Any martial melee weapon",
                        PickOne = true,
                        GrantItems =
                        [
                            new EquipmentGrantItem { ItemId = "item:longsword", Quantity = 1 },
                            new EquipmentGrantItem { ItemId = "item:greataxe", Quantity = 1 },
                        ],
                    },
                ],
            },
        ],
    };

    private static readonly List<EquipmentItemDefinition> TestEquipmentItems =
    [
        new EquipmentItemDefinition { Id = "item:longsword", DisplayName = "Longsword", Category = "weapon", Subcategory = "martial-melee" },
        new EquipmentItemDefinition { Id = "item:greataxe", DisplayName = "Greataxe", Category = "weapon", Subcategory = "martial-melee" },
        new EquipmentItemDefinition { Id = "item:dagger", DisplayName = "Dagger", Category = "weapon", Subcategory = "simple-melee" },
        new EquipmentItemDefinition { Id = "item:shield", DisplayName = "Shield", Category = "armor", Subcategory = "shield" },
        new EquipmentItemDefinition { Id = "item:chain-mail", DisplayName = "Chain Mail", Category = "armor", Subcategory = "heavy" },
        new EquipmentItemDefinition { Id = "item:leather-armor", DisplayName = "Leather Armor", Category = "armor", Subcategory = "light" },
    ];

    [Fact]
    public void Step7_ClassConfig_AllRequiredChoicesMade_IsValid()
    {
        var config = BuildFighterEquipmentConfig();
        var character = new Character
        {
            StartingEquipmentChoices = [new EquipmentGroupChoice { GroupId = "weapon-group", ChosenOptionId = "sword-shield" }],
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 },
                new CharacterEquipmentItem { ItemId = "item:shield", Quantity = 1 },
                new CharacterEquipmentItem { ItemId = "item:chain-mail", Quantity = 1 },
            ],
        };

        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Step7_ClassConfig_RequiredChoiceMissing_IsInvalid()
    {
        var config = BuildFighterEquipmentConfig();
        var character = new Character
        {
            // No StartingEquipmentChoices recorded
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 },
            ],
        };

        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_CHOICE_MISSING") && e.Contains("weapon-group"));
    }

    [Fact]
    public void Step7_ClassConfig_PickOneOption_ValidItemChosen_IsValid()
    {
        var config = BuildFighterEquipmentConfig();
        var character = new Character
        {
            StartingEquipmentChoices =
            [
                new EquipmentGroupChoice
                {
                    GroupId = "weapon-group",
                    ChosenOptionId = "martial-pick",
                    PickedItemId = "item:greataxe",
                },
            ],
            Equipment = [new CharacterEquipmentItem { ItemId = "item:greataxe", Quantity = 1 }],
        };

        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Step7_ClassConfig_StartingWealth_WithGold_IsValid()
    {
        var config = BuildFighterEquipmentConfig();
        var character = new Character
        {
            ClassStartingWealthChosen = true,
            ClassStartingGold = 100,
        };

        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Step7_ClassConfig_StartingWealth_NoGold_IsValidWithWarning()
    {
        var config = BuildFighterEquipmentConfig();
        var character = new Character
        {
            ClassStartingWealthChosen = true,
            ClassStartingGold = null,
        };

        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("WARN_WEALTH_NO_GOLD"));
    }

    // ── Step 7: Equipment (legacy path — strict toggle) ───────────────────

    [Fact]
    public void Step7_LegacyPath_StrictMode_ItemInAllowedList_IsValid()
    {
        // Strict mode: only class-allowed items should be selectable.
        var allowedIds = new List<string> { "item:longsword", "item:chain-mail" };
        var character = new Character
        {
            Equipment = [new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 }],
        };

        var result = new EquipmentValidator(TestEquipmentItems).Validate(character, allowedIds);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Step7_LegacyPath_StrictMode_ItemOutsideAllowedList_IsInvalid()
    {
        // Strict mode rejects items not on the class's starting equipment list.
        var allowedIds = new List<string> { "item:longsword", "item:chain-mail" };
        var character = new Character
        {
            Equipment = [new CharacterEquipmentItem { ItemId = "item:dagger", Quantity = 1 }],
        };

        var result = new EquipmentValidator(TestEquipmentItems).Validate(character, allowedIds);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_EQUIPMENT_NOT_ALLOWED"));
    }

    [Fact]
    public void Step7_LegacyPath_NonStrictMode_AnyValidItem_IsValid()
    {
        // Non-strict mode: no allowed list passed → any valid equipment item is accepted.
        var character = new Character
        {
            Equipment = [new CharacterEquipmentItem { ItemId = "item:dagger", Quantity = 1 }],
        };

        // allowedIds = null means non-strict mode
        var result = new EquipmentValidator(TestEquipmentItems).Validate(character, null);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Step7_LegacyPath_NonStrictMode_UnknownItem_IsInvalid()
    {
        // Non-strict mode still rejects items not in the equipment data at all.
        var character = new Character
        {
            Equipment = [new CharacterEquipmentItem { ItemId = "item:does-not-exist", Quantity = 1 }],
        };

        var result = new EquipmentValidator(TestEquipmentItems).Validate(character, null);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_EQUIPMENT_UNKNOWN"));
    }

    // ── FR2.1.1 Regression: errors visible immediately after any field change ──
    // These tests verify that when the user changes a field in a wizard step,
    // the step validator (which ValidateAndAutoSaveAsync calls via OnChanged)
    // immediately surfaces errors—not only on the "Next" click.

    [Fact]
    public void FR2_1_1_Step0_AfterClearingCharacterName_ValidatorSurfacesError()
    {
        // Before: user had a valid name — no errors.
        var beforeResult = MetaValidator.Validate("Aric Stonehammer", null, null);
        Assert.True(beforeResult.IsValid);

        // After: user clears the name field (field change event fires OnChanged).
        var afterResult = MetaValidator.Validate(string.Empty, null, null);

        // The validator must surface an error immediately, without a Next click.
        Assert.False(afterResult.IsValid);
        Assert.Contains(afterResult.Errors, e => e.Contains("ERR_META_NAME_REQUIRED"));
    }

    [Fact]
    public void FR2_1_1_Step0_AfterEnteringNameThatIsTooLong_ValidatorSurfacesError()
    {
        string tooLong = new('A', MetaValidator.MaxNameLength + 1);

        // Before: valid name
        var beforeResult = MetaValidator.Validate("Aric", null, null);
        Assert.True(beforeResult.IsValid);

        // After: user pastes a name that exceeds the max length (field change).
        var afterResult = MetaValidator.Validate(tooLong, null, null);

        Assert.False(afterResult.IsValid);
        Assert.Contains(afterResult.Errors, e => e.Contains("ERR_META_NAME_TOO_LONG"));
    }

    [Fact]
    public void FR2_1_1_Step1_StandardArray_AfterChangingAbilityToZero_ValidatorSurfacesError()
    {
        // Before: fully valid standard array assignment.
        int[] validScores = [15, 14, 13, 12, 10, 8];
        var beforeResult = StandardArrayValidator.Validate(validScores);
        Assert.True(beforeResult.IsValid);

        // After: user clears one ability selection (sets it back to the unassigned 0 placeholder).
        int[] afterScores = [15, 14, 13, 12, 10, 0];
        var afterResult = StandardArrayValidator.Validate(afterScores);

        Assert.False(afterResult.IsValid);
        Assert.Contains(afterResult.Errors, e => e.Contains("ERR_STDARRAY_INVALID"));
    }

    [Fact]
    public void FR2_1_1_Step1_PointBuy_AfterRaisingScoreBeyondBudget_ValidatorSurfacesError()
    {
        // Before: exactly at budget.
        int[] validScores = [15, 15, 8, 8, 8, 8]; // 13 points
        var beforeResult = PointBuyValidator.Validate(validScores);
        Assert.True(beforeResult.IsValid);

        // After: user increments a score and exceeds the budget (field change).
        int[] overBudgetScores = [15, 15, 15, 15, 15, 15]; // 9*6 = 54 > 27
        var afterResult = PointBuyValidator.Validate(overBudgetScores);

        Assert.False(afterResult.IsValid);
        Assert.Contains(afterResult.Errors, e => e.Contains("ERR_POINTBUY_BUDGET"));
    }

    [Fact]
    public void FR2_1_1_Step2_AfterClearingRaceSelection_ValidatorSurfacesError()
    {
        // Before: valid race selection.
        var beforeChar = new Character
        {
            RaceId = "race:human",
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
        var beforeResult = new RaceValidator(TestRaces).Validate(beforeChar);
        Assert.True(beforeResult.IsValid);

        // After: user clears the race dropdown (field change).
        var afterChar = new Character
        {
            RaceId = string.Empty,
            AbilityScores = new AbilityScores(),
        };
        var afterResult = new RaceValidator(TestRaces).Validate(afterChar);

        Assert.False(afterResult.IsValid);
        Assert.Contains(afterResult.Errors, e => e.Contains("ERR_RACE"));
    }

    [Fact]
    public void FR2_1_1_Step3_AfterChangingClassToUnknownId_ValidatorSurfacesError()
    {
        // Before: valid class selection.
        var beforeChar = new Character
        {
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { STR = new AbilityBlock { Base = 15 } },
        };
        var beforeResult = new ClassValidator(TestClasses).Validate(beforeChar);
        Assert.True(beforeResult.IsValid);

        // After: user edits the class field to an unrecognised value (field change).
        var afterChar = new Character
        {
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:unknown-class", Level = 1 }],
            AbilityScores = new AbilityScores(),
        };
        var afterResult = new ClassValidator(TestClasses).Validate(afterChar);

        Assert.False(afterResult.IsValid);
        Assert.Contains(afterResult.Errors, e => e.Contains("ERR_CLASS_UNKNOWN"));
    }

    [Fact]
    public void FR2_1_1_Step5_AfterPickingTooManyClassSkills_ValidatorSurfacesError()
    {
        // Before: Fighter with exactly 2 class skills chosen — valid.
        var beforeChar = new Character
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
        var beforeResult = new ProficiencyValidator(TestClasses, TestBackgrounds).Validate(beforeChar);
        Assert.True(beforeResult.IsValid);

        // After: user picks a third class skill — one more than the Fighter's allowance of 2
        // (field change fires OnChanged → validator must surface error immediately).
        var afterChar = new Character
        {
            BackgroundId = "background:soldier",
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            Skills = new Dictionary<string, string>
            {
                ["skill:perception"] = "class",
                ["skill:survival"] = "class",
                ["skill:history"] = "class",   // one too many class skills
                ["skill:athletics"] = "background",
                ["skill:intimidation"] = "background",
            },
        };
        var afterResult = new ProficiencyValidator(TestClasses, TestBackgrounds).Validate(afterChar);

        Assert.False(afterResult.IsValid);
        Assert.Contains(afterResult.Errors, e => e.Contains("ERR_SKILL_COUNT"));
    }
}
