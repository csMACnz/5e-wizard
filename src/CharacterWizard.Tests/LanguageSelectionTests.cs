using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Utilities;

namespace CharacterWizard.Tests;

/// <summary>
/// Tests for the language selection feature on the Background wizard step.
/// Covers LanguageHelper utilities and background-step language gating logic.
/// </summary>
public class LanguageSelectionTests
{
    // ── Shared seed data ────────────────────────────────────────────────

    private static readonly List<RaceDefinition> TestRaces =
    [
        new RaceDefinition
        {
            Id = "race:human",
            DisplayName = "Human",
            LanguageIds = ["lang:common"],
            TraitIds = ["trait:extra-language"],
            Subraces = [],
        },
        new RaceDefinition
        {
            Id = "race:dwarf",
            DisplayName = "Dwarf",
            LanguageIds = ["lang:common", "lang:dwarvish"],
            TraitIds = [],
            Subraces =
            [
                new SubraceDefinition
                {
                    Id = "subrace:hill-dwarf",
                    DisplayName = "Hill Dwarf",
                    LanguageIds = [],
                    TraitIds = [],
                },
            ],
        },
        new RaceDefinition
        {
            Id = "race:elf",
            DisplayName = "Elf",
            LanguageIds = ["lang:common", "lang:elvish"],
            TraitIds = [],
            Subraces =
            [
                new SubraceDefinition
                {
                    Id = "subrace:high-elf",
                    DisplayName = "High Elf",
                    LanguageIds = [],
                    TraitIds = ["trait:extra-language"],
                },
            ],
        },
    ];

    private static readonly List<BackgroundDefinition> TestBackgrounds =
    [
        new BackgroundDefinition
        {
            Id = "background:acolyte",
            DisplayName = "Acolyte",
            LanguageCount = 2,
            SkillProficiencies = ["skill:insight", "skill:religion"],
        },
        new BackgroundDefinition
        {
            Id = "background:soldier",
            DisplayName = "Soldier",
            LanguageCount = 0,
            SkillProficiencies = ["skill:athletics", "skill:intimidation"],
        },
        new BackgroundDefinition
        {
            Id = "background:noble",
            DisplayName = "Noble",
            LanguageCount = 1,
            SkillProficiencies = ["skill:history", "skill:persuasion"],
        },
    ];

    // ── GetFixedLanguageIds ──────────────────────────────────────────────

    [Fact]
    public void GetFixedLanguageIds_NoRaceSelected_ReturnsEmpty()
    {
        var fixedIds = LanguageHelper.GetFixedLanguageIds(TestRaces, string.Empty, string.Empty);
        Assert.Empty(fixedIds);
    }

    [Fact]
    public void GetFixedLanguageIds_DwarfNoSubrace_ReturnsCommonAndDwarvish()
    {
        var fixedIds = LanguageHelper.GetFixedLanguageIds(TestRaces, "race:dwarf", string.Empty);
        Assert.Contains("lang:common", fixedIds);
        Assert.Contains("lang:dwarvish", fixedIds);
        Assert.Equal(2, fixedIds.Count);
    }

    [Fact]
    public void GetFixedLanguageIds_ElfHighElfSubrace_ReturnsCommonAndElvish()
    {
        var fixedIds = LanguageHelper.GetFixedLanguageIds(TestRaces, "race:elf", "subrace:high-elf");
        Assert.Contains("lang:common", fixedIds);
        Assert.Contains("lang:elvish", fixedIds);
        Assert.Equal(2, fixedIds.Count);
    }

    // ── GetExtraLanguageSlots ────────────────────────────────────────────

    [Fact]
    public void GetExtraLanguageSlots_NoBackgroundSelected_ReturnsZero()
    {
        int slots = LanguageHelper.GetExtraLanguageSlots(
            TestRaces, TestBackgrounds, "race:human", string.Empty, string.Empty);
        Assert.Equal(0, slots);
    }

    [Fact]
    public void GetExtraLanguageSlots_SoldierBackground_ZeroSlots()
    {
        int slots = LanguageHelper.GetExtraLanguageSlots(
            TestRaces, TestBackgrounds, "race:dwarf", string.Empty, "background:soldier");
        Assert.Equal(0, slots);
    }

    [Fact]
    public void GetExtraLanguageSlots_AcolyteBackground_TwoSlots()
    {
        int slots = LanguageHelper.GetExtraLanguageSlots(
            TestRaces, TestBackgrounds, "race:dwarf", string.Empty, "background:acolyte");
        Assert.Equal(2, slots);
    }

    [Fact]
    public void GetExtraLanguageSlots_HumanWithSoldierBackground_OneSlotFromTrait()
    {
        int slots = LanguageHelper.GetExtraLanguageSlots(
            TestRaces, TestBackgrounds, "race:human", string.Empty, "background:soldier");
        // Soldier gives 0 languages + human trait:extra-language gives +1
        Assert.Equal(1, slots);
    }

    [Fact]
    public void GetExtraLanguageSlots_HumanWithAcolyteBackground_ThreeSlots()
    {
        int slots = LanguageHelper.GetExtraLanguageSlots(
            TestRaces, TestBackgrounds, "race:human", string.Empty, "background:acolyte");
        // Acolyte: 2 + human trait:extra-language: 1 = 3
        Assert.Equal(3, slots);
    }

    [Fact]
    public void GetExtraLanguageSlots_HighElfWithNobleBackground_TwoSlots()
    {
        int slots = LanguageHelper.GetExtraLanguageSlots(
            TestRaces, TestBackgrounds, "race:elf", "subrace:high-elf", "background:noble");
        // Noble: 1 + high-elf trait:extra-language: 1 = 2
        Assert.Equal(2, slots);
    }

    // ── Reconcile ────────────────────────────────────────────────────────

    [Fact]
    public void Reconcile_RemovesExcessChoices()
    {
        // 3 chosen but only 2 slots
        var chosen = new List<string> { "lang:elvish", "lang:gnomish", "lang:goblin" };
        var fixedIds = new List<string> { "lang:common", "lang:dwarvish" };

        var result = LanguageHelper.Reconcile(chosen, fixedIds, 2);

        // Should trim to 2 (keep first 2)
        Assert.Equal(2, result.Count);
        Assert.Equal("lang:elvish", result[0]);
        Assert.Equal("lang:gnomish", result[1]);
    }

    [Fact]
    public void Reconcile_RemovesChoicesThatClashWithFixed()
    {
        var chosen = new List<string> { "lang:dwarvish", "lang:elvish" };
        var fixedIds = new List<string> { "lang:common", "lang:dwarvish" };

        var result = LanguageHelper.Reconcile(chosen, fixedIds, 2);

        Assert.DoesNotContain("lang:dwarvish", result);
        Assert.Contains("lang:elvish", result);
    }

    [Fact]
    public void Reconcile_RemovesDuplicates()
    {
        // 1 slot; duplicate chosen
        var chosen = new List<string> { "lang:elvish", "lang:elvish" };
        var fixedIds = new List<string> { "lang:common" };

        var result = LanguageHelper.Reconcile(chosen, fixedIds, 1);

        Assert.Single(result);
        Assert.Equal("lang:elvish", result[0]);
    }

    [Fact]
    public void Reconcile_WithZeroSlots_ReturnsEmpty()
    {
        var chosen = new List<string> { "lang:elvish" };
        var fixedIds = new List<string> { "lang:common" };

        var result = LanguageHelper.Reconcile(chosen, fixedIds, 0);

        Assert.Empty(result);
    }

    [Fact]
    public void Reconcile_WithExactCount_RetainsAll()
    {
        var chosen = new List<string> { "lang:elvish", "lang:gnomish" };
        var fixedIds = new List<string> { "lang:common" };

        var result = LanguageHelper.Reconcile(chosen, fixedIds, 2);

        Assert.Equal(2, result.Count);
    }

    // ── Slot calculation — deficit / excess check ─────────────────────────

    [Fact]
    public void Step5_WithExtraSlots_NotEnoughChosen_ProducesDeficit()
    {
        // human + acolyte = 3 slots; chosen = 0
        int slots = LanguageHelper.GetExtraLanguageSlots(
            TestRaces, TestBackgrounds, "race:human", string.Empty, "background:acolyte");
        int chosen = 0;
        int deficit = slots - chosen;
        Assert.Equal(3, deficit);
        // Validator produces ERR_LANGUAGE_PICKS_INCOMPLETE when deficit > 0
        Assert.True(deficit > 0);
    }

    [Fact]
    public void Step5_ExactlyEnoughChosen_NoDeficit()
    {
        // noble: 1 slot; dwarf has no extra-language trait
        int slots = LanguageHelper.GetExtraLanguageSlots(
            TestRaces, TestBackgrounds, "race:dwarf", string.Empty, "background:noble");
        int chosen = 1;
        int deficit = slots - chosen;
        Assert.Equal(0, deficit);
    }

    [Fact]
    public void Step5_NoExtraLanguageSlots_AlwaysZeroDeficit()
    {
        // Soldier + Dwarf = 0 language slots
        int slots = LanguageHelper.GetExtraLanguageSlots(
            TestRaces, TestBackgrounds, "race:dwarf", string.Empty, "background:soldier");
        Assert.Equal(0, slots);
    }
}
