using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

public class SpellValidatorTests
{
    private static readonly List<SpellDefinition> TestSpells =
    [
        new SpellDefinition
        {
            Id = "spell:fire-bolt",
            DisplayName = "Fire Bolt",
            Level = 0,
            ClassIds = ["class:sorcerer", "class:wizard"],
        },
        new SpellDefinition
        {
            Id = "spell:light",
            DisplayName = "Light",
            Level = 0,
            ClassIds = ["class:bard", "class:cleric", "class:sorcerer", "class:wizard"],
        },
        new SpellDefinition
        {
            Id = "spell:magic-missile",
            DisplayName = "Magic Missile",
            Level = 1,
            ClassIds = ["class:sorcerer", "class:wizard"],
        },
        new SpellDefinition
        {
            Id = "spell:shield",
            DisplayName = "Shield",
            Level = 1,
            ClassIds = ["class:sorcerer", "class:wizard"],
        },
        new SpellDefinition
        {
            Id = "spell:sleep",
            DisplayName = "Sleep",
            Level = 1,
            ClassIds = ["class:bard", "class:sorcerer", "class:wizard"],
        },
        new SpellDefinition
        {
            Id = "spell:cure-wounds",
            DisplayName = "Cure Wounds",
            Level = 1,
            ClassIds = ["class:bard", "class:cleric"],
        },
    ];

    private static readonly List<ClassDefinition> TestClasses =
    [
        new ClassDefinition
        {
            Id = "class:wizard",
            DisplayName = "Wizard",
            HitDie = 6,
            SavingThrows = ["INT", "WIS"],
            SkillChoices = new SkillChoices { Count = 2, Options = ["skill:arcana"] },
            Spellcasting = new SpellcastingInfo
            {
                CastingType = "full",
                SpellcastingAbility = "INT",
                PrepareSpells = true,
                CantripsKnownByLevel = [3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5],
                SpellsKnownByLevel = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
            },
        },
        new ClassDefinition
        {
            Id = "class:sorcerer",
            DisplayName = "Sorcerer",
            HitDie = 6,
            SavingThrows = ["CON", "CHA"],
            SkillChoices = new SkillChoices { Count = 2, Options = ["skill:arcana"] },
            Spellcasting = new SpellcastingInfo
            {
                CastingType = "full",
                SpellcastingAbility = "CHA",
                PrepareSpells = false,
                CantripsKnownByLevel = [4, 4, 4, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6],
                SpellsKnownByLevel = [2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 12, 13, 13, 14, 14, 15, 15, 15, 15],
            },
        },
        new ClassDefinition
        {
            Id = "class:fighter",
            DisplayName = "Fighter",
            HitDie = 10,
            SavingThrows = ["STR", "CON"],
            SkillChoices = new SkillChoices { Count = 2, Options = ["skill:athletics"] },
        },
    ];

    [Fact]
    public void EmptySpells_IsValid()
    {
        var character = new Character { Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }] };
        var result = new SpellValidator(TestSpells, TestClasses).Validate(character);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidCantrip_ForWizard_IsValid()
    {
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "class:wizard" },
            ],
        };
        var result = new SpellValidator(TestSpells, TestClasses).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void DuplicateSpell_IsInvalid()
    {
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "class:wizard" },
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "class:wizard" },
            ],
        };
        var result = new SpellValidator(TestSpells, TestClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_DUPLICATE"));
    }

    [Fact]
    public void UnknownSpell_IsInvalid()
    {
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:does-not-exist", ClassId = "class:wizard" },
            ],
        };
        var result = new SpellValidator(TestSpells, TestClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_UNKNOWN"));
    }

    [Fact]
    public void SpellNotOnClassList_IsInvalid()
    {
        // cure-wounds is not on the wizard spell list
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:cure-wounds", ClassId = "class:wizard" },
            ],
        };
        var result = new SpellValidator(TestSpells, TestClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_NOT_FOR_CLASS"));
    }

    [Fact]
    public void TooManyCantrips_ForSorcerer_IsInvalid()
    {
        // Sorcerer level 1 allows 4 cantrips; selecting 5 should be invalid.
        // Build a dedicated spell list with 5 sorcerer cantrips to exceed the limit.
        var spellsWithMany = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:c1", DisplayName = "C1", Level = 0, ClassIds = ["class:sorcerer"] },
            new SpellDefinition { Id = "spell:c2", DisplayName = "C2", Level = 0, ClassIds = ["class:sorcerer"] },
            new SpellDefinition { Id = "spell:c3", DisplayName = "C3", Level = 0, ClassIds = ["class:sorcerer"] },
            new SpellDefinition { Id = "spell:c4", DisplayName = "C4", Level = 0, ClassIds = ["class:sorcerer"] },
            new SpellDefinition { Id = "spell:c5", DisplayName = "C5", Level = 0, ClassIds = ["class:sorcerer"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:sorcerer", Level = 1 }],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:c1", ClassId = "class:sorcerer" },
                new CharacterSpell { SpellId = "spell:c2", ClassId = "class:sorcerer" },
                new CharacterSpell { SpellId = "spell:c3", ClassId = "class:sorcerer" },
                new CharacterSpell { SpellId = "spell:c4", ClassId = "class:sorcerer" },
                new CharacterSpell { SpellId = "spell:c5", ClassId = "class:sorcerer" },
            ],
        };
        var result = new SpellValidator(spellsWithMany, TestClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_CANTRIP_COUNT"));
    }

    [Fact]
    public void TooManyKnownSpells_ForSorcerer_IsInvalid()
    {
        // Sorcerer level 1 allows 2 known spells; selecting 3 is invalid
        var spellsForTest = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:s1", DisplayName = "S1", Level = 1, ClassIds = ["class:sorcerer"] },
            new SpellDefinition { Id = "spell:s2", DisplayName = "S2", Level = 1, ClassIds = ["class:sorcerer"] },
            new SpellDefinition { Id = "spell:s3", DisplayName = "S3", Level = 1, ClassIds = ["class:sorcerer"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:sorcerer", Level = 1 }],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:s1", ClassId = "class:sorcerer" },
                new CharacterSpell { SpellId = "spell:s2", ClassId = "class:sorcerer" },
                new CharacterSpell { SpellId = "spell:s3", ClassId = "class:sorcerer" },
            ],
        };
        var result = new SpellValidator(spellsForTest, TestClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_KNOWN_COUNT"));
    }

    [Fact]
    public void NonCaster_WithNoSpells_IsValid()
    {
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 5 }],
        };
        var result = new SpellValidator(TestSpells, TestClasses).Validate(character);
        Assert.True(result.IsValid);
    }
}
