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
        new SpellDefinition { Id = "spell:detect-magic", DisplayName = "Detect Magic", Level = 1, ClassIds = ["class:wizard"] },
        new SpellDefinition { Id = "spell:burning-hands", DisplayName = "Burning Hands", Level = 1, ClassIds = ["class:wizard"] },
        new SpellDefinition { Id = "spell:color-spray", DisplayName = "Color Spray", Level = 1, ClassIds = ["class:wizard"] },
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
        new ClassDefinition
        {
            Id = "class:ranger",
            DisplayName = "Ranger",
            HitDie = 10,
            SavingThrows = ["STR", "DEX"],
            SkillChoices = new SkillChoices { Count = 3, Options = ["skill:athletics"] },
            Spellcasting = new SpellcastingInfo
            {
                CastingType = "half",
                SpellcastingAbility = "WIS",
                PrepareSpells = false,
                CantripsKnownByLevel = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
                SpellsKnownByLevel = [0, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11],
            },
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
                new CharacterSpell { SpellId = "spell:magic-missile", ClassId = "class:wizard" },
                new CharacterSpell { SpellId = "spell:shield", ClassId = "class:wizard" },
                new CharacterSpell { SpellId = "spell:sleep", ClassId = "class:wizard" },
                new CharacterSpell { SpellId = "spell:detect-magic", ClassId = "class:wizard" },
                new CharacterSpell { SpellId = "spell:burning-hands", ClassId = "class:wizard" },
                new CharacterSpell { SpellId = "spell:color-spray", ClassId = "class:wizard" },
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

    [Fact]
    public void Wizard_Level1_WithOnly3Level1Spells_HasSpellbookCountError()
    {
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:wb1", DisplayName = "WB1", Level = 1, ClassIds = ["class:wizard"] },
            new SpellDefinition { Id = "spell:wb2", DisplayName = "WB2", Level = 1, ClassIds = ["class:wizard"] },
            new SpellDefinition { Id = "spell:wb3", DisplayName = "WB3", Level = 1, ClassIds = ["class:wizard"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:wb1", ClassId = "class:wizard" },
                new CharacterSpell { SpellId = "spell:wb2", ClassId = "class:wizard" },
                new CharacterSpell { SpellId = "spell:wb3", ClassId = "class:wizard" },
            ],
        };
        var result = new SpellValidator(spells, TestClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_WIZARD_SPELLBOOK_COUNT"));
    }

    [Fact]
    public void Wizard_Level3_NeedsAtLeast10Spells_HasSpellbookCountError()
    {
        // Level 3 wizard: 6 + 2*(3-1) = 10 spells required (can be level 1 or 2)
        var spells = Enumerable.Range(1, 9)
            .Select(i => new SpellDefinition { Id = $"spell:w{i}", DisplayName = $"W{i}", Level = 1, ClassIds = ["class:wizard"] })
            .ToList();
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 3 }],
            Spells = spells.Select(s => new CharacterSpell { SpellId = s.Id, ClassId = "class:wizard" }).ToList(),
        };
        var result = new SpellValidator(spells, TestClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_WIZARD_SPELLBOOK_COUNT"));
    }

    [Fact]
    public void Wizard_Level3_With10Spells_IsValid()
    {
        // Level 3 wizard: needs exactly 10 spells; can include level-2 spells
        var spells = Enumerable.Range(1, 8)
            .Select(i => new SpellDefinition { Id = $"spell:w{i}", DisplayName = $"W{i}", Level = 1, ClassIds = ["class:wizard"] })
            .Concat(
            [
                new SpellDefinition { Id = "spell:w9", DisplayName = "W9", Level = 2, ClassIds = ["class:wizard"] },
                new SpellDefinition { Id = "spell:w10", DisplayName = "W10", Level = 2, ClassIds = ["class:wizard"] },
            ])
            .ToList();
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 3 }],
            Spells = spells.Select(s => new CharacterSpell { SpellId = s.Id, ClassId = "class:wizard" }).ToList(),
        };
        var result = new SpellValidator(spells, TestClasses).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Sorcerer_Level1_SelectingLevel2Spell_IsInvalid()
    {
        // Level 1 sorcerer can only cast level 1 spells (highest slot = 1)
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:level2spell", DisplayName = "Level2Spell", Level = 2, ClassIds = ["class:sorcerer"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:sorcerer", Level = 1 }],
            Spells = [new CharacterSpell { SpellId = "spell:level2spell", ClassId = "class:sorcerer" }],
        };
        var result = new SpellValidator(spells, TestClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_LEVEL_TOO_HIGH"));
    }

    [Fact]
    public void Sorcerer_Level5_SelectingLevel3Spell_IsValid()
    {
        // Level 5 sorcerer has level-3 slots, can select level-3 spells; needs 6 known
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:s1", DisplayName = "S1", Level = 1, ClassIds = ["class:sorcerer"] },
            new SpellDefinition { Id = "spell:s2", DisplayName = "S2", Level = 1, ClassIds = ["class:sorcerer"] },
            new SpellDefinition { Id = "spell:s3", DisplayName = "S3", Level = 1, ClassIds = ["class:sorcerer"] },
            new SpellDefinition { Id = "spell:s4", DisplayName = "S4", Level = 2, ClassIds = ["class:sorcerer"] },
            new SpellDefinition { Id = "spell:s5", DisplayName = "S5", Level = 2, ClassIds = ["class:sorcerer"] },
            new SpellDefinition { Id = "spell:s6", DisplayName = "S6", Level = 3, ClassIds = ["class:sorcerer"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:sorcerer", Level = 5 }],
            Spells = spells.Select(s => new CharacterSpell { SpellId = s.Id, ClassId = "class:sorcerer" }).ToList(),
        };
        var result = new SpellValidator(spells, TestClasses).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Ranger_Level1_SelectingASpell_IsInvalid()
    {
        // Ranger at level 1 has no spell slots (half-caster, no slots until level 2)
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:hunter-mark", DisplayName = "Hunter's Mark", Level = 1, ClassIds = ["class:ranger"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:ranger", Level = 1 }],
            Spells = [new CharacterSpell { SpellId = "spell:hunter-mark", ClassId = "class:ranger" }],
        };
        var result = new SpellValidator(spells, TestClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_LEVEL_TOO_HIGH"));
    }

    [Fact]
    public void Ranger_Level2_SelectingOneSpell_IsValid()
    {
        // Ranger at level 2 has level-1 spell slots and may know 2 spells
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:hunter-mark", DisplayName = "Hunter's Mark", Level = 1, ClassIds = ["class:ranger"] },
            new SpellDefinition { Id = "spell:cure-wounds-ranger", DisplayName = "Cure Wounds", Level = 1, ClassIds = ["class:ranger"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:ranger", Level = 2 }],
            Spells = spells.Select(s => new CharacterSpell { SpellId = s.Id, ClassId = "class:ranger" }).ToList(),
        };
        var result = new SpellValidator(spells, TestClasses).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void KnownSpellsCaster_AtLevelWithZeroAllowed_SelectingSpell_IsInvalid()
    {
        // Ranger level 1 has 0 spells known (spellsKnownByLevel[0] = 0) and no spell slots
        // Selecting a spell should be invalid due to spell level restriction
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:ranger-s1", DisplayName = "RS1", Level = 1, ClassIds = ["class:ranger"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:ranger", Level = 1 }],
            Spells = [new CharacterSpell { SpellId = "spell:ranger-s1", ClassId = "class:ranger" }],
        };
        var result = new SpellValidator(spells, TestClasses).Validate(character);
        Assert.False(result.IsValid);
        // Ranger level 1: highest slot = 0, so any leveled spell triggers ERR_SPELL_LEVEL_TOO_HIGH
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_LEVEL_TOO_HIGH"));
    }

    // ── Pact Magic (Warlock) ──────────────────────────────────────────────

    private static readonly List<ClassDefinition> WarlockClasses =
    [
        new ClassDefinition
        {
            Id = "class:warlock",
            DisplayName = "Warlock",
            HitDie = 8,
            SavingThrows = ["WIS", "CHA"],
            SkillChoices = new SkillChoices { Count = 2, Options = ["skill:arcana"] },
            Spellcasting = new SpellcastingInfo
            {
                CastingType = "pact",
                SpellcastingAbility = "CHA",
                PrepareSpells = false,
                CantripsKnownByLevel = [2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4],
                // Warlock known spells: 2 at L1, increasing by 1 per level
                SpellsKnownByLevel = [2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15],
            },
        },
    ];

    [Fact]
    public void Warlock_Level1_WithOneLevel1Spell_IsValid()
    {
        // Warlock L1 has 1 pact slot at L1, so selecting a L1 spell is valid
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:hex", DisplayName = "Hex", Level = 1, ClassIds = ["class:warlock"] },
            new SpellDefinition { Id = "spell:eldritch-blast", DisplayName = "Eldritch Blast", Level = 0, ClassIds = ["class:warlock"] },
            new SpellDefinition { Id = "spell:chill-touch", DisplayName = "Chill Touch", Level = 0, ClassIds = ["class:warlock"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:warlock", Level = 1 }],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:eldritch-blast", ClassId = "class:warlock" },
                new CharacterSpell { SpellId = "spell:chill-touch", ClassId = "class:warlock" },
                new CharacterSpell { SpellId = "spell:hex", ClassId = "class:warlock" },
            ],
        };
        var result = new SpellValidator(spells, WarlockClasses).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Warlock_Level3_WithLevel2Spell_IsValid()
    {
        // Warlock L3 has 2 pact slots at slot level L2; selecting a L2 spell is valid
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:s1", DisplayName = "S1", Level = 1, ClassIds = ["class:warlock"] },
            new SpellDefinition { Id = "spell:s2", DisplayName = "S2", Level = 2, ClassIds = ["class:warlock"] },
            new SpellDefinition { Id = "spell:s3", DisplayName = "S3", Level = 2, ClassIds = ["class:warlock"] },
            new SpellDefinition { Id = "spell:s4", DisplayName = "S4", Level = 1, ClassIds = ["class:warlock"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:warlock", Level = 3 }],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:s1", ClassId = "class:warlock" },
                new CharacterSpell { SpellId = "spell:s2", ClassId = "class:warlock" },
                new CharacterSpell { SpellId = "spell:s3", ClassId = "class:warlock" },
                new CharacterSpell { SpellId = "spell:s4", ClassId = "class:warlock" },
            ],
        };
        var result = new SpellValidator(spells, WarlockClasses).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Warlock_Level3_WithLevel3Spell_IsInvalid()
    {
        // Warlock L3 pact slots are L2 — selecting a L3 spell exceeds highest slot level
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:counterspell", DisplayName = "Counterspell", Level = 3, ClassIds = ["class:warlock"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:warlock", Level = 3 }],
            Spells = [new CharacterSpell { SpellId = "spell:counterspell", ClassId = "class:warlock" }],
        };
        var result = new SpellValidator(spells, WarlockClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_LEVEL_TOO_HIGH"));
    }

    // ── Third-caster subclass spellcasting (Arcane Trickster / Eldritch Knight) ──

    private static readonly List<ClassDefinition> ThirdCasterClasses =
    [
        new ClassDefinition
        {
            Id = "class:fighter",
            DisplayName = "Fighter",
            HitDie = 10,
            SavingThrows = ["STR", "CON"],
            SkillChoices = new SkillChoices { Count = 2, Options = ["skill:athletics"] },
            SubclassOptions =
            [
                new SubclassDefinition
                {
                    Id = "subclass:fighter:eldritch-knight",
                    DisplayName = "Eldritch Knight",
                    Spellcasting = new SpellcastingInfo
                    {
                        CastingType = "third",
                        SpellcastingAbility = "INT",
                        PrepareSpells = false,
                        SpellListId = "spelllist:wizard",
                        // AT/EK cantrips: 0 until L3, 2 at L3-7, 3 at L8-14, 4 at L15+
                        CantripsKnownByLevel = [0, 0, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4],
                        // AT/EK known spells: 0 at L1-2, 3 at L3, increasing
                        SpellsKnownByLevel = [0, 0, 3, 4, 4, 4, 5, 6, 6, 7, 8, 8, 9, 10, 10, 11, 11, 11, 12, 13],
                    },
                },
            ],
        },
        new ClassDefinition
        {
            Id = "class:rogue",
            DisplayName = "Rogue",
            HitDie = 8,
            SavingThrows = ["DEX", "INT"],
            SkillChoices = new SkillChoices { Count = 4, Options = ["skill:acrobatics"] },
            SubclassOptions =
            [
                new SubclassDefinition
                {
                    Id = "subclass:rogue:arcane-trickster",
                    DisplayName = "Arcane Trickster",
                    Spellcasting = new SpellcastingInfo
                    {
                        CastingType = "third",
                        SpellcastingAbility = "INT",
                        PrepareSpells = false,
                        SpellListId = "spelllist:wizard",
                        CantripsKnownByLevel = [0, 0, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4],
                        SpellsKnownByLevel = [0, 0, 3, 4, 4, 4, 5, 6, 6, 7, 8, 8, 9, 10, 10, 11, 11, 11, 12, 13],
                    },
                },
            ],
        },
    ];

    [Fact]
    public void ThirdCaster_Level3_WithLevel1Spell_IsValid()
    {
        // Fighter(EK) L3 with third-caster spellcasting: has 2 L1 slots, L1 spells are valid
        // Spells stored with the subclass ID as ClassId
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:magic-missile", DisplayName = "Magic Missile", Level = 1, ClassIds = ["class:wizard"] },
            new SpellDefinition { Id = "spell:shield", DisplayName = "Shield", Level = 1, ClassIds = ["class:wizard"] },
            new SpellDefinition { Id = "spell:fire-bolt", DisplayName = "Fire Bolt", Level = 0, ClassIds = ["class:wizard"] },
            new SpellDefinition { Id = "spell:prestidigitation", DisplayName = "Prestidigitation", Level = 0, ClassIds = ["class:wizard"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3, SubclassId = "subclass:fighter:eldritch-knight" }],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "subclass:fighter:eldritch-knight" },
                new CharacterSpell { SpellId = "spell:prestidigitation", ClassId = "subclass:fighter:eldritch-knight" },
                new CharacterSpell { SpellId = "spell:magic-missile", ClassId = "subclass:fighter:eldritch-knight" },
                new CharacterSpell { SpellId = "spell:shield", ClassId = "subclass:fighter:eldritch-knight" },
            ],
        };
        var result = new SpellValidator(spells, ThirdCasterClasses).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void ThirdCaster_Level2_WithLevel1Spell_IsInvalid()
    {
        // Fighter at L2 without any spellcasting subclass unlock — spell should be invalid
        // (third-caster gets no slots until L3)
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:magic-missile", DisplayName = "Magic Missile", Level = 1, ClassIds = ["class:wizard"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 2, SubclassId = "subclass:fighter:eldritch-knight" }],
            Spells = [new CharacterSpell { SpellId = "spell:magic-missile", ClassId = "subclass:fighter:eldritch-knight" }],
        };
        var result = new SpellValidator(spells, ThirdCasterClasses).Validate(character);
        Assert.False(result.IsValid);
        // At level 2 the third-caster highest slot is 0, so level-1 spell is too high
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_LEVEL_TOO_HIGH"));
    }

    [Fact]
    public void ThirdCaster_Level3_WithLevel2Spell_IsInvalid()
    {
        // Fighter(EK) L3 has only L1 slots (third-caster), so a L2 spell is too high
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:misty-step", DisplayName = "Misty Step", Level = 2, ClassIds = ["class:wizard"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3, SubclassId = "subclass:fighter:eldritch-knight" }],
            Spells = [new CharacterSpell { SpellId = "spell:misty-step", ClassId = "subclass:fighter:eldritch-knight" }],
        };
        var result = new SpellValidator(spells, ThirdCasterClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_LEVEL_TOO_HIGH"));
    }

    [Fact]
    public void ThirdCaster_SpellNotOnSpellList_IsInvalid()
    {
        // EK uses the wizard spell list; a Druid spell should be flagged as not for class
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:druidcraft", DisplayName = "Druidcraft", Level = 0, ClassIds = ["class:druid"] },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3, SubclassId = "subclass:fighter:eldritch-knight" }],
            Spells = [new CharacterSpell { SpellId = "spell:druidcraft", ClassId = "subclass:fighter:eldritch-knight" }],
        };
        var result = new SpellValidator(spells, ThirdCasterClasses).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_NOT_FOR_CLASS"));
    }

    [Fact]
    public void RacialCantrip_WithRaceSourceId_IsNotFlaggedAsInvalidClass()
    {
        // A racial cantrip stored with ClassId = raceId should not trigger ERR_SPELL_NOT_FOR_CLASS
        var spells = new List<SpellDefinition>
        {
            new SpellDefinition { Id = "spell:minor-illusion", DisplayName = "Minor Illusion", Level = 0, ClassIds = ["class:wizard"] },
        };
        var classes = new List<ClassDefinition>
        {
            new ClassDefinition { Id = "class:fighter", DisplayName = "Fighter", HitDie = 10,
                SavingThrows = [], SkillChoices = new SkillChoices { Count = 2, Options = [] } },
        };
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            // Racial cantrip — ClassId is the race/subrace ID, not a class
            Spells = [new CharacterSpell { SpellId = "spell:minor-illusion", ClassId = "race:elf:high-elf" }],
        };
        var result = new SpellValidator(spells, classes).Validate(character);
        // No ERR_SPELL_NOT_FOR_CLASS — racial cantrip bypasses class membership check
        Assert.DoesNotContain(result.Errors, e => e.Contains("ERR_SPELL_NOT_FOR_CLASS"));
    }
}
