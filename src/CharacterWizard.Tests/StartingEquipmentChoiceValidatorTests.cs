using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

/// <summary>
/// Tests for StartingEquipmentChoiceValidator covering choice groups, starting wealth,
/// and various permutations of valid/invalid selections.
/// </summary>
public class StartingEquipmentChoiceValidatorTests
{
    // ─── Test data helpers ────────────────────────────────────────────────

    /// <summary>
    /// Builds a minimal config for a fighter-like class with:
    ///   Group 1 (required): option A = [longsword+shield] fixed; option B = [longsword|shortsword] pickOne
    ///   Group 2 (required): option A = [dungeoneer-pack] fixed; option B = [explorers-pack] fixed
    /// Fixed items: none
    /// Wealth roll: "5d4*10"
    /// </summary>
    private static ClassStartingEquipmentEntry BuildTestConfig() =>
        new()
        {
            ClassId = "class:test",
            StartingWealthRoll = "5d4*10",
            FixedItemIds = [],
            ChoiceGroups =
            [
                new EquipmentChoiceGroup
                {
                    Id = "weapon-group",
                    Description = "Choose weapon",
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
                            Id = "martial-choice",
                            Description = "Any martial melee weapon",
                            PickOne = true,
                            GrantItems =
                            [
                                new EquipmentGrantItem { ItemId = "item:longsword", Quantity = 1 },
                                new EquipmentGrantItem { ItemId = "item:shortsword", Quantity = 1 },
                            ],
                        },
                    ],
                },
                new EquipmentChoiceGroup
                {
                    Id = "pack-group",
                    Description = "Choose a pack",
                    Required = true,
                    Options =
                    [
                        new EquipmentChoiceOption
                        {
                            Id = "dungeoneers",
                            Description = "Dungeoneer's pack",
                            PickOne = false,
                            GrantItems = [new EquipmentGrantItem { ItemId = "item:dungeoneer-pack", Quantity = 1 }],
                        },
                        new EquipmentChoiceOption
                        {
                            Id = "explorers",
                            Description = "Explorer's pack",
                            PickOne = false,
                            GrantItems = [new EquipmentGrantItem { ItemId = "item:explorers-pack", Quantity = 1 }],
                        },
                    ],
                },
            ],
        };

    private static Character BuildCharacterWith(
        List<string> itemIds,
        List<EquipmentGroupChoice>? choices = null,
        bool wealthChosen = false,
        int? startingGold = null)
    {
        var c = new Character
        {
            ClassStartingWealthChosen = wealthChosen,
            ClassStartingGold = startingGold,
            StartingEquipmentChoices = choices ?? [],
        };
        foreach (var id in itemIds)
            c.Equipment.Add(new CharacterEquipmentItem { ItemId = id, Quantity = 1 });
        return c;
    }

    // ─── Null config tests ────────────────────────────────────────────────

    [Fact]
    public void NullConfig_AlwaysValid()
    {
        var validator = new StartingEquipmentChoiceValidator(null);
        var character = BuildCharacterWith(["item:longsword"]);
        var result = validator.Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    // ─── Starting wealth path tests ───────────────────────────────────────

    [Fact]
    public void StartingWealth_NoEquipment_NoGoldWarning()
    {
        var config = BuildTestConfig();
        var character = BuildCharacterWith([], wealthChosen: true, startingGold: null);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("WARN_WEALTH_NO_GOLD"));
    }

    [Fact]
    public void StartingWealth_WithGoldAmount_Valid()
    {
        var config = BuildTestConfig();
        var character = BuildCharacterWith([], wealthChosen: true, startingGold: 50);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void StartingWealth_WithClassEquipment_IsInvalid()
    {
        var config = BuildTestConfig();
        var character = BuildCharacterWith(["item:longsword"], wealthChosen: true, startingGold: 50);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_WEALTH_WITH_EQUIPMENT"));
    }

    [Fact]
    public void StartingWealth_NegativeGold_IsInvalid()
    {
        var config = BuildTestConfig();
        var character = BuildCharacterWith([], wealthChosen: true, startingGold: -10);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_WEALTH_NEGATIVE"));
    }

    [Fact]
    public void StartingWealth_MultipleClassItems_AllReported()
    {
        var config = BuildTestConfig();
        var character = BuildCharacterWith(
            ["item:longsword", "item:dungeoneer-pack"],
            wealthChosen: true,
            startingGold: 50);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_WEALTH_WITH_EQUIPMENT"));
        Assert.Contains("item:longsword", result.Errors[0]);
    }

    // ─── Choice group path: missing/invalid choices ───────────────────────

    [Fact]
    public void NoChoices_RequiredGroup_IsInvalid()
    {
        var config = BuildTestConfig();
        var character = BuildCharacterWith(["item:longsword", "item:shield", "item:dungeoneer-pack"]);
        // No StartingEquipmentChoices recorded
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_CHOICE_MISSING"));
    }

    [Fact]
    public void InvalidOptionId_IsInvalid()
    {
        var config = BuildTestConfig();
        var choices = new List<EquipmentGroupChoice>
        {
            new() { GroupId = "weapon-group", ChosenOptionId = "nonexistent-option" },
            new() { GroupId = "pack-group", ChosenOptionId = "dungeoneers" },
        };
        var character = BuildCharacterWith(["item:longsword", "item:dungeoneer-pack"], choices);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_CHOICE_INVALID_OPTION"));
    }

    [Fact]
    public void OnlyOneGroupChosen_OtherMissing_IsInvalid()
    {
        var config = BuildTestConfig();
        var choices = new List<EquipmentGroupChoice>
        {
            new() { GroupId = "weapon-group", ChosenOptionId = "sword-shield" },
            // pack-group missing
        };
        var character = BuildCharacterWith(["item:longsword", "item:shield"], choices);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_CHOICE_MISSING") && e.Contains("pack-group"));
    }

    // ─── Choice group path: fixed option (pickOne=false) ─────────────────

    [Fact]
    public void FixedOption_AllItemsPresent_IsValid()
    {
        var config = BuildTestConfig();
        var choices = new List<EquipmentGroupChoice>
        {
            new() { GroupId = "weapon-group", ChosenOptionId = "sword-shield" },
            new() { GroupId = "pack-group", ChosenOptionId = "dungeoneers" },
        };
        var character = BuildCharacterWith(
            ["item:longsword", "item:shield", "item:dungeoneer-pack"],
            choices);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void FixedOption_MissingItem_IsInvalid()
    {
        var config = BuildTestConfig();
        var choices = new List<EquipmentGroupChoice>
        {
            new() { GroupId = "weapon-group", ChosenOptionId = "sword-shield" },
            new() { GroupId = "pack-group", ChosenOptionId = "dungeoneers" },
        };
        // shield is missing from equipment
        var character = BuildCharacterWith(["item:longsword", "item:dungeoneer-pack"], choices);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_FIXED_ITEM_MISSING") && e.Contains("item:shield"));
    }

    [Fact]
    public void FixedOption_AlternativePack_IsValid()
    {
        var config = BuildTestConfig();
        var choices = new List<EquipmentGroupChoice>
        {
            new() { GroupId = "weapon-group", ChosenOptionId = "sword-shield" },
            new() { GroupId = "pack-group", ChosenOptionId = "explorers" },
        };
        var character = BuildCharacterWith(
            ["item:longsword", "item:shield", "item:explorers-pack"],
            choices);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    // ─── Choice group path: pick-one option (pickOne=true) ───────────────

    [Fact]
    public void PickOneOption_ValidItemChosen_IsValid()
    {
        var config = BuildTestConfig();
        var choices = new List<EquipmentGroupChoice>
        {
            new() { GroupId = "weapon-group", ChosenOptionId = "martial-choice", PickedItemId = "item:shortsword" },
            new() { GroupId = "pack-group", ChosenOptionId = "dungeoneers" },
        };
        var character = BuildCharacterWith(["item:shortsword", "item:dungeoneer-pack"], choices);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void PickOneOption_NoPickedItem_IsInvalid()
    {
        var config = BuildTestConfig();
        var choices = new List<EquipmentGroupChoice>
        {
            new() { GroupId = "weapon-group", ChosenOptionId = "martial-choice" },  // PickedItemId = null
            new() { GroupId = "pack-group", ChosenOptionId = "dungeoneers" },
        };
        var character = BuildCharacterWith(["item:shortsword", "item:dungeoneer-pack"], choices);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_PICK_ONE_MISSING"));
    }

    [Fact]
    public void PickOneOption_InvalidItemPicked_IsInvalid()
    {
        var config = BuildTestConfig();
        var choices = new List<EquipmentGroupChoice>
        {
            new() { GroupId = "weapon-group", ChosenOptionId = "martial-choice", PickedItemId = "item:greataxe" },
            new() { GroupId = "pack-group", ChosenOptionId = "dungeoneers" },
        };
        // item:greataxe is NOT in the martial-choice grant list (only longsword, shortsword)
        var character = BuildCharacterWith(["item:greataxe", "item:dungeoneer-pack"], choices);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_PICK_ONE_INVALID"));
    }

    [Fact]
    public void PickOneOption_ValidItemPickedButNotInEquipment_IsInvalid()
    {
        var config = BuildTestConfig();
        var choices = new List<EquipmentGroupChoice>
        {
            // PickedItemId says shortsword but character's equipment has longsword
            new() { GroupId = "weapon-group", ChosenOptionId = "martial-choice", PickedItemId = "item:shortsword" },
            new() { GroupId = "pack-group", ChosenOptionId = "dungeoneers" },
        };
        var character = BuildCharacterWith(["item:longsword", "item:dungeoneer-pack"], choices);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_PICK_ONE_NOT_IN_EQUIPMENT"));
    }

    // ─── Optional group tests ─────────────────────────────────────────────

    [Fact]
    public void OptionalGroup_NoChoiceMade_IsValid()
    {
        var config = new ClassStartingEquipmentEntry
        {
            ClassId = "class:test",
            StartingWealthRoll = "2d4*10",
            FixedItemIds = [],
            ChoiceGroups =
            [
                new EquipmentChoiceGroup
                {
                    Id = "optional-group",
                    Description = "Optional weapon",
                    Required = false,
                    Options =
                    [
                        new EquipmentChoiceOption
                        {
                            Id = "dagger",
                            Description = "A dagger",
                            PickOne = false,
                            GrantItems = [new EquipmentGrantItem { ItemId = "item:dagger", Quantity = 1 }],
                        },
                    ],
                },
            ],
        };

        var character = BuildCharacterWith([]);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    // ─── Empty choice groups config ───────────────────────────────────────

    [Fact]
    public void EmptyChoiceGroups_IsValid()
    {
        var config = new ClassStartingEquipmentEntry
        {
            ClassId = "class:test",
            StartingWealthRoll = "2d4*10",
            FixedItemIds = [],
            ChoiceGroups = [],
        };
        var character = BuildCharacterWith(["item:longsword"]);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    // ─── Multiclass (primary class only) ─────────────────────────────────

    [Fact]
    public void PrimaryClass_ValidChoices_IsValid()
    {
        // Validate only for primary class config; secondary class equipment is ignored.
        var primaryConfig = BuildTestConfig();
        var choices = new List<EquipmentGroupChoice>
        {
            new() { GroupId = "weapon-group", ChosenOptionId = "sword-shield" },
            new() { GroupId = "pack-group", ChosenOptionId = "dungeoneers" },
        };
        // Character has extra items from secondary class — not validated here
        var character = BuildCharacterWith(
            ["item:longsword", "item:shield", "item:dungeoneer-pack", "item:dagger"],
            choices);
        var result = new StartingEquipmentChoiceValidator(primaryConfig).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    // ─── Error code uniqueness ────────────────────────────────────────────

    [Fact]
    public void MultipleErrors_AllReported()
    {
        var config = BuildTestConfig();
        // No choices, no equipment - should get 2 ERR_CHOICE_MISSING errors
        var character = BuildCharacterWith([]);
        var result = new StartingEquipmentChoiceValidator(config).Validate(character);
        Assert.False(result.IsValid);
        var missingErrors = result.Errors.Where(e => e.Contains("ERR_CHOICE_MISSING")).ToList();
        Assert.Equal(2, missingErrors.Count);
    }
}
