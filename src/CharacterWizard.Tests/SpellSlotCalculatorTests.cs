using CharacterWizard.Shared.Utilities;

namespace CharacterWizard.Tests;

public class SpellSlotCalculatorTests
{
    // ── Full caster ───────────────────────────────────────────────────────

    [Fact]
    public void FullCaster_Level1_Has2L1Slots()
    {
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(1, 1, "full"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(1, 2, "full"));
    }

    [Fact]
    public void FullCaster_Level5_HasExpectedSlots()
    {
        Assert.Equal(4, SpellSlotCalculator.GetMaxSlots(5, 1, "full"));
        Assert.Equal(3, SpellSlotCalculator.GetMaxSlots(5, 2, "full"));
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(5, 3, "full"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(5, 4, "full"));
    }

    [Fact]
    public void FullCaster_Level20_HasExpectedSlots()
    {
        Assert.Equal(4, SpellSlotCalculator.GetMaxSlots(20, 1, "full"));
        Assert.Equal(3, SpellSlotCalculator.GetMaxSlots(20, 2, "full"));
        Assert.Equal(3, SpellSlotCalculator.GetMaxSlots(20, 3, "full"));
        Assert.Equal(3, SpellSlotCalculator.GetMaxSlots(20, 4, "full"));
        Assert.Equal(3, SpellSlotCalculator.GetMaxSlots(20, 5, "full"));
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(20, 6, "full"));
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(20, 7, "full"));
        Assert.Equal(1, SpellSlotCalculator.GetMaxSlots(20, 8, "full"));
        Assert.Equal(1, SpellSlotCalculator.GetMaxSlots(20, 9, "full"));
    }

    // ── Half caster ───────────────────────────────────────────────────────

    [Fact]
    public void HalfCaster_Level1_HasNoSlots()
    {
        for (int sl = 1; sl <= 9; sl++)
            Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(1, sl, "half"));
    }

    [Fact]
    public void HalfCaster_Level2_Has2L1Slots()
    {
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(2, 1, "half"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(2, 2, "half"));
    }

    [Fact]
    public void HalfCaster_Level5_HasExpectedSlots()
    {
        Assert.Equal(4, SpellSlotCalculator.GetMaxSlots(5, 1, "half"));
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(5, 2, "half"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(5, 3, "half"));
    }

    // ── Pact caster ───────────────────────────────────────────────────────

    [Fact]
    public void PactCaster_Level1_Has1L1Slot()
    {
        Assert.Equal(1, SpellSlotCalculator.GetMaxSlots(1, 1, "pact"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(1, 2, "pact"));
    }

    [Fact]
    public void PactCaster_Level5_Has2L3Slots()
    {
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(5, 1, "pact"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(5, 2, "pact"));
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(5, 3, "pact"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(5, 4, "pact"));
    }

    [Fact]
    public void PactCaster_Level11_Has3L5Slots()
    {
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(11, 1, "pact"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(11, 4, "pact"));
        Assert.Equal(3, SpellSlotCalculator.GetMaxSlots(11, 5, "pact"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(11, 6, "pact"));
    }

    // ── Third caster ──────────────────────────────────────────────────────

    [Fact]
    public void ThirdCaster_Level1_HasNoSlots()
    {
        for (int sl = 1; sl <= 9; sl++)
            Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(1, sl, "third"));
    }

    [Fact]
    public void ThirdCaster_Level2_HasNoSlots()
    {
        for (int sl = 1; sl <= 9; sl++)
            Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(2, sl, "third"));
    }

    [Fact]
    public void ThirdCaster_Level3_Has2L1Slots()
    {
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(3, 1, "third"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(3, 2, "third"));
    }

    [Fact]
    public void ThirdCaster_Level7_Has4L1And2L2Slots()
    {
        Assert.Equal(4, SpellSlotCalculator.GetMaxSlots(7, 1, "third"));
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(7, 2, "third"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(7, 3, "third"));
    }

    // ── GetHighestSlotLevel ───────────────────────────────────────────────

    [Fact]
    public void GetHighestSlotLevel_HalfCaster_Level1_Returns0()
    {
        Assert.Equal(0, SpellSlotCalculator.GetHighestSlotLevel(1, "half"));
    }

    [Fact]
    public void GetHighestSlotLevel_HalfCaster_Level2_Returns1()
    {
        Assert.Equal(1, SpellSlotCalculator.GetHighestSlotLevel(2, "half"));
    }

    [Fact]
    public void GetHighestSlotLevel_PactCaster_Level5_Returns3()
    {
        Assert.Equal(3, SpellSlotCalculator.GetHighestSlotLevel(5, "pact"));
    }

    [Fact]
    public void GetHighestSlotLevel_ThirdCaster_Level1_Returns0()
    {
        Assert.Equal(0, SpellSlotCalculator.GetHighestSlotLevel(1, "third"));
    }

    [Fact]
    public void GetHighestSlotLevel_ThirdCaster_Level3_Returns1()
    {
        Assert.Equal(1, SpellSlotCalculator.GetHighestSlotLevel(3, "third"));
    }

    // ── Unknown casting type fallback ─────────────────────────────────────

    [Fact]
    public void UnknownCastingType_FallsBackToFullCaster()
    {
        // level 1 full caster has 2 L1 slots
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(1, 1, "unknown-type"));
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(1, 1, ""));
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(1, 1, "arcane"));
    }

    // ── Edge cases ────────────────────────────────────────────────────────

    [Fact]
    public void SlotLevelOutOfRange_Returns0()
    {
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(10, 0, "full"));
        Assert.Equal(0, SpellSlotCalculator.GetMaxSlots(10, 10, "full"));
    }

    [Fact]
    public void ClassLevelClamped_BelowMin()
    {
        // classLevel 0 should clamp to 1
        Assert.Equal(2, SpellSlotCalculator.GetMaxSlots(0, 1, "full"));
    }

    [Fact]
    public void ClassLevelClamped_AboveMax()
    {
        // classLevel 21 should clamp to 20
        Assert.Equal(SpellSlotCalculator.GetMaxSlots(20, 1, "full"), SpellSlotCalculator.GetMaxSlots(21, 1, "full"));
    }

    // ── GetEffectiveCasterLevel ───────────────────────────────────────────

    [Fact]
    public void GetEffectiveCasterLevel_FullCaster_ReturnsFullLevel()
    {
        Assert.Equal(1, SpellSlotCalculator.GetEffectiveCasterLevel(1, "full"));
        Assert.Equal(5, SpellSlotCalculator.GetEffectiveCasterLevel(5, "full"));
        Assert.Equal(20, SpellSlotCalculator.GetEffectiveCasterLevel(20, "full"));
    }

    [Fact]
    public void GetEffectiveCasterLevel_HalfCaster_ReturnsHalfFloor()
    {
        Assert.Equal(0, SpellSlotCalculator.GetEffectiveCasterLevel(1, "half"));  // 1/2 = 0
        Assert.Equal(1, SpellSlotCalculator.GetEffectiveCasterLevel(2, "half"));  // 2/2 = 1
        Assert.Equal(2, SpellSlotCalculator.GetEffectiveCasterLevel(5, "half"));  // 5/2 = 2
        Assert.Equal(10, SpellSlotCalculator.GetEffectiveCasterLevel(20, "half")); // 20/2 = 10
    }

    [Fact]
    public void GetEffectiveCasterLevel_ThirdCaster_ReturnsThirdFloor()
    {
        Assert.Equal(0, SpellSlotCalculator.GetEffectiveCasterLevel(1, "third"));  // 1/3 = 0
        Assert.Equal(0, SpellSlotCalculator.GetEffectiveCasterLevel(2, "third"));  // 2/3 = 0
        Assert.Equal(1, SpellSlotCalculator.GetEffectiveCasterLevel(3, "third"));  // 3/3 = 1
        Assert.Equal(2, SpellSlotCalculator.GetEffectiveCasterLevel(7, "third"));  // 7/3 = 2
        Assert.Equal(6, SpellSlotCalculator.GetEffectiveCasterLevel(20, "third")); // 20/3 = 6
    }

    [Fact]
    public void GetEffectiveCasterLevel_PactCaster_Returns0()
    {
        // Warlock pact slots do not combine with the multiclass table
        Assert.Equal(0, SpellSlotCalculator.GetEffectiveCasterLevel(1, "pact"));
        Assert.Equal(0, SpellSlotCalculator.GetEffectiveCasterLevel(10, "pact"));
        Assert.Equal(0, SpellSlotCalculator.GetEffectiveCasterLevel(20, "pact"));
    }

    [Fact]
    public void GetEffectiveCasterLevel_UnknownType_FallsBackToFull()
    {
        Assert.Equal(5, SpellSlotCalculator.GetEffectiveCasterLevel(5, "unknown"));
        Assert.Equal(3, SpellSlotCalculator.GetEffectiveCasterLevel(3, ""));
    }

    // ── GetMulticlassSlots ────────────────────────────────────────────────

    [Fact]
    public void GetMulticlassSlots_EmptyList_Returns0()
    {
        Assert.Equal(0, SpellSlotCalculator.GetMulticlassSlots([], 1));
        Assert.Equal(0, SpellSlotCalculator.GetMulticlassSlots([], 5));
    }

    [Fact]
    public void GetMulticlassSlots_SlotLevelOutOfRange_Returns0()
    {
        var casters = new[] { (5, "full") };
        Assert.Equal(0, SpellSlotCalculator.GetMulticlassSlots(casters, 0));
        Assert.Equal(0, SpellSlotCalculator.GetMulticlassSlots(casters, 10));
    }

    [Fact]
    public void GetMulticlassSlots_SingleFullCaster_MatchesSingleClassSlots()
    {
        // A single full-caster multiclass should match the standard full-caster table
        var casters = new[] { (5, "full") };
        for (int sl = 1; sl <= 9; sl++)
            Assert.Equal(SpellSlotCalculator.GetMaxSlots(5, sl, "full"),
                         SpellSlotCalculator.GetMulticlassSlots(casters, sl));
    }

    [Fact]
    public void GetMulticlassSlots_TwoFullCasters_CombinesLevels()
    {
        // Wizard 4 + Sorcerer 3 = effective level 7
        // Full-caster level 7 has: L1×4, L2×3, L3×3, L4×1
        var casters = new[] { (4, "full"), (3, "full") };
        Assert.Equal(4, SpellSlotCalculator.GetMulticlassSlots(casters, 1));
        Assert.Equal(3, SpellSlotCalculator.GetMulticlassSlots(casters, 2));
        Assert.Equal(3, SpellSlotCalculator.GetMulticlassSlots(casters, 3));
        Assert.Equal(1, SpellSlotCalculator.GetMulticlassSlots(casters, 4));
        Assert.Equal(0, SpellSlotCalculator.GetMulticlassSlots(casters, 5));
    }

    [Fact]
    public void GetMulticlassSlots_FullAndHalfCaster_CombinesCorrectly()
    {
        // Wizard 2 + Paladin 4 → effective: 2 + (4/2=2) = 4
        // Full-caster level 4: L1×4, L2×3
        var casters = new[] { (2, "full"), (4, "half") };
        Assert.Equal(4, SpellSlotCalculator.GetMulticlassSlots(casters, 1));
        Assert.Equal(3, SpellSlotCalculator.GetMulticlassSlots(casters, 2));
        Assert.Equal(0, SpellSlotCalculator.GetMulticlassSlots(casters, 3));
    }

    [Fact]
    public void GetMulticlassSlots_FullAndThirdCaster_CombinesCorrectly()
    {
        // Wizard 3 + Fighter(EK) 6 → effective: 3 + (6/3=2) = 5
        // Full-caster level 5: L1×4, L2×3, L3×2
        var casters = new[] { (3, "full"), (6, "third") };
        Assert.Equal(4, SpellSlotCalculator.GetMulticlassSlots(casters, 1));
        Assert.Equal(3, SpellSlotCalculator.GetMulticlassSlots(casters, 2));
        Assert.Equal(2, SpellSlotCalculator.GetMulticlassSlots(casters, 3));
        Assert.Equal(0, SpellSlotCalculator.GetMulticlassSlots(casters, 4));
    }

    [Fact]
    public void GetMulticlassSlots_PactCasterExcludedFromCombinedTable()
    {
        // Warlock contributes 0 to combined level — slots are separate Pact Magic slots
        // Wizard 3 + Warlock 5 → combined effective level = 3 + 0 = 3
        // Full-caster level 3: L1×4, L2×2
        var casters = new[] { (3, "full"), (5, "pact") };
        Assert.Equal(4, SpellSlotCalculator.GetMulticlassSlots(casters, 1));
        Assert.Equal(2, SpellSlotCalculator.GetMulticlassSlots(casters, 2));
        Assert.Equal(0, SpellSlotCalculator.GetMulticlassSlots(casters, 3));
    }

    [Fact]
    public void GetMulticlassSlots_OnlyNonCasters_Returns0()
    {
        // Non-casters (fighters, rogues without spellcasting subclass) contribute 0
        // but are not typically passed to GetMulticlassSlots; verify that a single third-caster
        // at level 2 (which contributes 0) results in 0 slots
        var casters = new[] { (2, "third") }; // 2/3 = 0 effective caster levels
        for (int sl = 1; sl <= 9; sl++)
            Assert.Equal(0, SpellSlotCalculator.GetMulticlassSlots(casters, sl));
    }

    [Fact]
    public void GetMulticlassSlots_HalfAndThirdCaster_CombinesCorrectly()
    {
        // Paladin 6 + Fighter(EK) 9 → effective: (6/2=3) + (9/3=3) = 6
        // Full-caster level 6: L1×4, L2×3, L3×3
        var casters = new[] { (6, "half"), (9, "third") };
        Assert.Equal(4, SpellSlotCalculator.GetMulticlassSlots(casters, 1));
        Assert.Equal(3, SpellSlotCalculator.GetMulticlassSlots(casters, 2));
        Assert.Equal(3, SpellSlotCalculator.GetMulticlassSlots(casters, 3));
        Assert.Equal(0, SpellSlotCalculator.GetMulticlassSlots(casters, 4));
    }
}
