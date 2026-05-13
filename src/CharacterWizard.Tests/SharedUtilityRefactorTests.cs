using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Utilities;

namespace CharacterWizard.Tests;

public class SharedUtilityRefactorTests
{
    private sealed class FixedRng(params int[] values) : IRng
    {
        private int _index;

        public int Next(int maxValue)
        {
            int v = values[_index % values.Length] % maxValue;
            _index++;
            return v < 0 ? 0 : v;
        }

        public int Next(int minValue, int maxValue)
        {
            int range = maxValue - minValue;
            int v = minValue + (values[_index % values.Length] % range);
            _index++;
            return v < minValue ? minValue : v;
        }
    }

    [Fact]
    public void AbilityHelper_Modifier_ReturnsExpected()
    {
        Assert.Equal(-1, AbilityHelper.GetModifier(8));
        Assert.Equal(0, AbilityHelper.GetModifier(10));
        Assert.Equal(1, AbilityHelper.GetModifier(12));
    }

    [Fact]
    public void AbilityHelper_GetCombinedRacialBonuses_MergesRaceAndSubrace()
    {
        var race = new RaceDefinition
        {
            Id = "race:elf",
            AbilityBonuses = new Dictionary<string, int> { ["DEX"] = 2 },
            Subraces =
            [
                new SubraceDefinition
                {
                    Id = "sub:high-elf",
                    AbilityBonuses = new Dictionary<string, int> { ["INT"] = 1, ["DEX"] = 1 },
                },
            ],
        };

        var bonuses = AbilityHelper.GetCombinedRacialBonuses(race, "sub:high-elf");
        Assert.Equal(3, bonuses["DEX"]);
        Assert.Equal(1, bonuses["INT"]);
    }

    [Fact]
    public void DiceHelper_TryRollExpression_ParsesAndRolls()
    {
        IRng rng = new FixedRng(2, 5); // d6 rolls 3 and 6 (min-inclusive range)
        bool ok = DiceHelper.TryRollExpression(rng, "2d6 * 10", out int total);

        Assert.True(ok);
        Assert.Equal(90, total);
    }

    [Fact]
    public void DiceHelper_TryRollExpression_Invalid_ReturnsFalse()
    {
        IRng rng = new FixedRng(1);
        bool ok = DiceHelper.TryRollExpression(rng, "abc", out int total);

        Assert.False(ok);
        Assert.Equal(0, total);
    }

    [Fact]
    public void HitPointCalculator_UsesFallbackWhenEntriesMissing()
    {
        var classes = new List<ClassDefinition>
        {
            new() { Id = "class:fighter", HitDie = 10 },
        };
        var c = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
        };

        // lvl1: 10+2=12, lvl2-3: (6+2)=8 each => 28
        int maxHp = HitPointCalculator.CalculateMaxHp(c, classes);
        Assert.Equal(28, maxHp);
    }

    [Fact]
    public void SpellSelectionRules_WizardSpellbookCount_IsFormulaBased()
    {
        Assert.Equal(6, SpellSelectionRules.GetWizardRequiredSpellbookCount(1));
        Assert.Equal(8, SpellSelectionRules.GetWizardRequiredSpellbookCount(2));
        Assert.Equal(14, SpellSelectionRules.GetWizardRequiredSpellbookCount(5));
    }

    [Fact]
    public void SpellSelectionRules_HasRacialCantripTrait_ChecksSubrace()
    {
        var races = new List<RaceDefinition>
        {
            new()
            {
                Id = "race:elf",
                TraitIds = [],
                Subraces =
                [
                    new SubraceDefinition
                    {
                        Id = "sub:high-elf",
                        TraitIds = ["trait:cantrip"],
                    },
                ],
            },
        };

        bool hasCantrip = SpellSelectionRules.HasRacialCantripTrait(races, "race:elf", "sub:high-elf");
        Assert.True(hasCantrip);
    }

    [Fact]
    public void SkillCatalog_HasRoundTripFightClubMappings()
    {
        Assert.Equal(100, SkillCatalog.FightClubNumberBySkillId["skill:acrobatics"]);
        Assert.Equal("skill:acrobatics", SkillCatalog.FightClubSkillIdByNumber[100]);
        Assert.Equal("Survival", SkillCatalog.SkillLabel("skill:survival"));
    }
}
