namespace CharacterWizard.Shared.Utilities;

/// <summary>
/// Provides spell slot counts for all D&amp;D 5e caster archetypes.
/// Tables are indexed [classLevel-1, slotLevel-1].
/// </summary>
public static class SpellSlotCalculator
{
    // Full caster table (Bard/Cleric/Druid/Sorcerer/Wizard)
    private static readonly int[,] FullCasterSlots =
    {
        // L1  L2  L3  L4  L5  L6  L7  L8  L9
        {  2,   0,  0,  0,  0,  0,  0,  0,  0 }, // level 1
        {  3,   0,  0,  0,  0,  0,  0,  0,  0 }, // 2
        {  4,   2,  0,  0,  0,  0,  0,  0,  0 }, // 3
        {  4,   3,  0,  0,  0,  0,  0,  0,  0 }, // 4
        {  4,   3,  2,  0,  0,  0,  0,  0,  0 }, // 5
        {  4,   3,  3,  0,  0,  0,  0,  0,  0 }, // 6
        {  4,   3,  3,  1,  0,  0,  0,  0,  0 }, // 7
        {  4,   3,  3,  2,  0,  0,  0,  0,  0 }, // 8
        {  4,   3,  3,  3,  1,  0,  0,  0,  0 }, // 9
        {  4,   3,  3,  3,  2,  0,  0,  0,  0 }, // 10
        {  4,   3,  3,  3,  2,  1,  0,  0,  0 }, // 11
        {  4,   3,  3,  3,  2,  1,  0,  0,  0 }, // 12
        {  4,   3,  3,  3,  2,  1,  1,  0,  0 }, // 13
        {  4,   3,  3,  3,  2,  1,  1,  0,  0 }, // 14
        {  4,   3,  3,  3,  2,  1,  1,  1,  0 }, // 15
        {  4,   3,  3,  3,  2,  1,  1,  1,  0 }, // 16
        {  4,   3,  3,  3,  2,  1,  1,  1,  1 }, // 17
        {  4,   3,  3,  3,  3,  1,  1,  1,  1 }, // 18
        {  4,   3,  3,  3,  3,  2,  1,  1,  1 }, // 19
        {  4,   3,  3,  3,  3,  2,  2,  1,  1 }, // 20
    };

    // Half caster table (Paladin/Ranger — level 1 has no slots)
    private static readonly int[,] HalfCasterSlots =
    {
        // L1  L2  L3  L4  L5  L6  L7  L8  L9
        {  0,   0,  0,  0,  0,  0,  0,  0,  0 }, // level 1
        {  2,   0,  0,  0,  0,  0,  0,  0,  0 }, // 2
        {  3,   0,  0,  0,  0,  0,  0,  0,  0 }, // 3
        {  3,   0,  0,  0,  0,  0,  0,  0,  0 }, // 4
        {  4,   2,  0,  0,  0,  0,  0,  0,  0 }, // 5
        {  4,   2,  0,  0,  0,  0,  0,  0,  0 }, // 6
        {  4,   3,  0,  0,  0,  0,  0,  0,  0 }, // 7
        {  4,   3,  0,  0,  0,  0,  0,  0,  0 }, // 8
        {  4,   3,  2,  0,  0,  0,  0,  0,  0 }, // 9
        {  4,   3,  2,  0,  0,  0,  0,  0,  0 }, // 10
        {  4,   3,  3,  0,  0,  0,  0,  0,  0 }, // 11
        {  4,   3,  3,  0,  0,  0,  0,  0,  0 }, // 12
        {  4,   3,  3,  1,  0,  0,  0,  0,  0 }, // 13
        {  4,   3,  3,  1,  0,  0,  0,  0,  0 }, // 14
        {  4,   3,  3,  2,  0,  0,  0,  0,  0 }, // 15
        {  4,   3,  3,  2,  0,  0,  0,  0,  0 }, // 16
        {  4,   3,  3,  3,  1,  0,  0,  0,  0 }, // 17
        {  4,   3,  3,  3,  1,  0,  0,  0,  0 }, // 18
        {  4,   3,  3,  3,  2,  0,  0,  0,  0 }, // 19
        {  4,   3,  3,  3,  2,  0,  0,  0,  0 }, // 20
    };

    // Pact Magic table (Warlock). Each row: count of pact slots at the pact slot level.
    // All entries in a row are 0 except the column matching the pact slot level.
    private static readonly int[,] PactCasterSlots =
    {
        // L1  L2  L3  L4  L5  L6  L7  L8  L9
        {  1,   0,  0,  0,  0,  0,  0,  0,  0 }, // level 1:  1 slot @ L1
        {  2,   0,  0,  0,  0,  0,  0,  0,  0 }, // level 2:  2 slots @ L1
        {  0,   2,  0,  0,  0,  0,  0,  0,  0 }, // level 3:  2 slots @ L2
        {  0,   2,  0,  0,  0,  0,  0,  0,  0 }, // level 4:  2 slots @ L2
        {  0,   0,  2,  0,  0,  0,  0,  0,  0 }, // level 5:  2 slots @ L3
        {  0,   0,  2,  0,  0,  0,  0,  0,  0 }, // level 6:  2 slots @ L3
        {  0,   0,  0,  2,  0,  0,  0,  0,  0 }, // level 7:  2 slots @ L4
        {  0,   0,  0,  2,  0,  0,  0,  0,  0 }, // level 8:  2 slots @ L4
        {  0,   0,  0,  0,  2,  0,  0,  0,  0 }, // level 9:  2 slots @ L5
        {  0,   0,  0,  0,  2,  0,  0,  0,  0 }, // level 10: 2 slots @ L5
        {  0,   0,  0,  0,  3,  0,  0,  0,  0 }, // level 11: 3 slots @ L5
        {  0,   0,  0,  0,  3,  0,  0,  0,  0 }, // level 12: 3 slots @ L5
        {  0,   0,  0,  0,  3,  0,  0,  0,  0 }, // level 13: 3 slots @ L5
        {  0,   0,  0,  0,  3,  0,  0,  0,  0 }, // level 14: 3 slots @ L5
        {  0,   0,  0,  0,  3,  0,  0,  0,  0 }, // level 15: 3 slots @ L5
        {  0,   0,  0,  0,  3,  0,  0,  0,  0 }, // level 16: 3 slots @ L5
        {  0,   0,  0,  0,  4,  0,  0,  0,  0 }, // level 17: 4 slots @ L5
        {  0,   0,  0,  0,  4,  0,  0,  0,  0 }, // level 18: 4 slots @ L5
        {  0,   0,  0,  0,  4,  0,  0,  0,  0 }, // level 19: 4 slots @ L5
        {  0,   0,  0,  0,  4,  0,  0,  0,  0 }, // level 20: 4 slots @ L5
    };

    // Third caster table (Arcane Trickster / Eldritch Knight — no slots until level 3)
    private static readonly int[,] ThirdCasterSlots =
    {
        // L1  L2  L3  L4  L5  L6  L7  L8  L9
        {  0,   0,  0,  0,  0,  0,  0,  0,  0 }, // level 1
        {  0,   0,  0,  0,  0,  0,  0,  0,  0 }, // level 2
        {  2,   0,  0,  0,  0,  0,  0,  0,  0 }, // level 3
        {  3,   0,  0,  0,  0,  0,  0,  0,  0 }, // level 4
        {  3,   0,  0,  0,  0,  0,  0,  0,  0 }, // level 5
        {  3,   0,  0,  0,  0,  0,  0,  0,  0 }, // level 6
        {  4,   2,  0,  0,  0,  0,  0,  0,  0 }, // level 7
        {  4,   2,  0,  0,  0,  0,  0,  0,  0 }, // level 8
        {  4,   2,  0,  0,  0,  0,  0,  0,  0 }, // level 9
        {  4,   3,  0,  0,  0,  0,  0,  0,  0 }, // level 10
        {  4,   3,  0,  0,  0,  0,  0,  0,  0 }, // level 11
        {  4,   3,  0,  0,  0,  0,  0,  0,  0 }, // level 12
        {  4,   3,  2,  0,  0,  0,  0,  0,  0 }, // level 13
        {  4,   3,  2,  0,  0,  0,  0,  0,  0 }, // level 14
        {  4,   3,  2,  0,  0,  0,  0,  0,  0 }, // level 15
        {  4,   3,  3,  0,  0,  0,  0,  0,  0 }, // level 16
        {  4,   3,  3,  0,  0,  0,  0,  0,  0 }, // level 17
        {  4,   3,  3,  0,  0,  0,  0,  0,  0 }, // level 18
        {  4,   3,  3,  1,  0,  0,  0,  0,  0 }, // level 19
        {  4,   3,  3,  1,  0,  0,  0,  0,  0 }, // level 20
    };

    /// <summary>
    /// Returns the number of spell slots of <paramref name="slotLevel"/> available to a caster
    /// of type <paramref name="castingType"/> at class level <paramref name="classLevel"/>.
    /// Unknown casting types fall back to the full-caster table.
    /// </summary>
    public static int GetMaxSlots(int classLevel, int slotLevel, string castingType)
    {
        if (slotLevel < 1 || slotLevel > 9) return 0;
        int lvlIdx = Math.Clamp(classLevel, 1, 20) - 1;
        int slotIdx = slotLevel - 1;
        return castingType.ToLowerInvariant() switch
        {
            "half" => HalfCasterSlots[lvlIdx, slotIdx],
            "pact" => PactCasterSlots[lvlIdx, slotIdx],
            "third" => ThirdCasterSlots[lvlIdx, slotIdx],
            _ => FullCasterSlots[lvlIdx, slotIdx],
        };
    }

    /// <summary>
    /// Returns the highest spell slot level that has at least one slot available for the
    /// given class level and casting type. Returns 0 if no slots are available.
    /// </summary>
    public static int GetHighestSlotLevel(int classLevel, string castingType)
    {
        for (int slotLevel = 9; slotLevel >= 1; slotLevel--)
            if (GetMaxSlots(classLevel, slotLevel, castingType) > 0)
                return slotLevel;
        return 0;
    }
}
