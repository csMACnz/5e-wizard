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
}
