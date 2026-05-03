using System.Text.Json;
using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

/// <summary>
/// End-to-end tests for CharacterValidator — exercises the full validation pipeline
/// including ability generation, race, class, proficiencies, spells, and equipment.
/// </summary>
public class CharacterValidatorEndToEndTests
{
    // ── Seed data ────────────────────────────────────────────────────────

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
        new ClassDefinition
        {
            Id = "class:wizard",
            DisplayName = "Wizard",
            HitDie = 6,
            SavingThrows = ["INT", "WIS"],
            SkillChoices = new SkillChoices
            {
                Count = 2,
                Options =
                [
                    "skill:arcana", "skill:history", "skill:insight",
                    "skill:investigation", "skill:medicine", "skill:religion",
                ],
            },
            MulticlassPrereqs = new Dictionary<string, int> { ["INT"] = 13 },
            SubclassLevel = 2,
            Spellcasting = new SpellcastingInfo
            {
                CastingType = "full",
                SpellcastingAbility = "INT",
                PrepareSpells = true,
                CantripsKnownByLevel = Enumerable.Range(1, 20).Select(_ => 3).ToList(),
            },
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
            StartingEquipmentIds = ["item:longsword"],
        },
        new BackgroundDefinition
        {
            Id = "background:sage",
            DisplayName = "Sage",
            FeatureId = "feat:researcher",
            SkillProficiencies = ["skill:arcana", "skill:history"],
            StartingEquipmentIds = ["item:dagger"],
        },
    ];

    private static readonly List<SpellDefinition> TestSpells =
    [
        new SpellDefinition
        {
            Id = "spell:fire-bolt", DisplayName = "Fire Bolt",
            Level = 0, ClassIds = ["class:wizard"],
        },
        new SpellDefinition
        {
            Id = "spell:magic-missile", DisplayName = "Magic Missile",
            Level = 1, ClassIds = ["class:wizard"],
        },
        new SpellDefinition { Id = "spell:shield", DisplayName = "Shield", Level = 1, ClassIds = ["class:wizard"] },
        new SpellDefinition { Id = "spell:sleep", DisplayName = "Sleep", Level = 1, ClassIds = ["class:wizard"] },
        new SpellDefinition { Id = "spell:detect-magic", DisplayName = "Detect Magic", Level = 1, ClassIds = ["class:wizard"] },
        new SpellDefinition { Id = "spell:burning-hands", DisplayName = "Burning Hands", Level = 1, ClassIds = ["class:wizard"] },
        new SpellDefinition { Id = "spell:color-spray", DisplayName = "Color Spray", Level = 1, ClassIds = ["class:wizard"] },
    ];

    private static readonly List<EquipmentItemDefinition> TestEquipment =
    [
        new EquipmentItemDefinition
        {
            Id = "item:longsword", DisplayName = "Longsword",
            Category = "weapon", Subcategory = "martial-melee",
        },
        new EquipmentItemDefinition
        {
            Id = "item:dagger", DisplayName = "Dagger",
            Category = "weapon", Subcategory = "simple-melee",
        },
    ];

    // ── Helper to build a fully-valid fighter character ───────────────────

    private static Character BuildValidFighter() => new()
    {
        Name = "Aric",
        RaceId = "race:human",
        TotalLevel = 1,
        Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
        BackgroundId = "background:soldier",
        GenerationMethod = GenerationMethod.StandardArray,
        AbilityScores = new AbilityScores
        {
            STR = new AbilityBlock { Base = 15, RacialBonus = 1 },
            DEX = new AbilityBlock { Base = 14, RacialBonus = 1 },
            CON = new AbilityBlock { Base = 13, RacialBonus = 1 },
            INT = new AbilityBlock { Base = 12, RacialBonus = 1 },
            WIS = new AbilityBlock { Base = 10, RacialBonus = 1 },
            CHA = new AbilityBlock { Base = 8, RacialBonus = 1 },
        },
        Skills = new Dictionary<string, string>
        {
            ["skill:perception"] = "class",
            ["skill:survival"] = "class",
            ["skill:athletics"] = "background",
            ["skill:intimidation"] = "background",
        },
    };

    private static CharacterValidator BuildValidator() =>
        new(TestRaces, TestClasses, TestBackgrounds, TestSpells, TestEquipment);

    // ── Full-pipeline happy-path ──────────────────────────────────────────

    [Fact]
    public void ValidFighter_StandardArray_PassesFullValidation()
    {
        var character = BuildValidFighter();
        var result = BuildValidator().Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void ValidFighter_PointBuy_PassesFullValidation()
    {
        var character = BuildValidFighter();
        character.GenerationMethod = GenerationMethod.PointBuy;
        // Use valid point-buy scores (uses exactly 27 points)
        character.AbilityScores = new AbilityScores
        {
            STR = new AbilityBlock { Base = 15, RacialBonus = 1 },
            DEX = new AbilityBlock { Base = 14, RacialBonus = 1 },
            CON = new AbilityBlock { Base = 13, RacialBonus = 1 },
            INT = new AbilityBlock { Base = 8, RacialBonus = 1 },
            WIS = new AbilityBlock { Base = 8, RacialBonus = 1 },
            CHA = new AbilityBlock { Base = 8, RacialBonus = 1 },
        };

        var result = BuildValidator().Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void ValidFighter_Roll_PassesFullValidation()
    {
        var character = BuildValidFighter();
        character.GenerationMethod = GenerationMethod.Roll;
        character.AbilityScores = new AbilityScores
        {
            STR = new AbilityBlock { Base = 15, RacialBonus = 1 },
            DEX = new AbilityBlock { Base = 14, RacialBonus = 1 },
            CON = new AbilityBlock { Base = 13, RacialBonus = 1 },
            INT = new AbilityBlock { Base = 12, RacialBonus = 1 },
            WIS = new AbilityBlock { Base = 10, RacialBonus = 1 },
            CHA = new AbilityBlock { Base = 8, RacialBonus = 1 },
        };

        var result = BuildValidator().Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    // ── Ability-score errors propagated ──────────────────────────────────

    [Fact]
    public void InvalidAbilityScores_AreReportedInFullValidation()
    {
        var character = BuildValidFighter();
        // Break ability scores — duplicate values in standard array
        character.AbilityScores.STR.Base = 15;
        character.AbilityScores.DEX.Base = 15; // duplicate 15
        character.AbilityScores.CON.Base = 13;
        character.AbilityScores.INT.Base = 12;
        character.AbilityScores.WIS.Base = 10;
        character.AbilityScores.CHA.Base = 8;

        var result = BuildValidator().Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_STDARRAY_INVALID"));
    }

    // ── Race errors propagated ────────────────────────────────────────────

    [Fact]
    public void UnknownRace_IsReportedInFullValidation()
    {
        var character = BuildValidFighter();
        character.RaceId = "race:does-not-exist";

        var result = BuildValidator().Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_RACE_UNKNOWN"));
    }

    [Fact]
    public void MissingSubrace_IsReportedInFullValidation()
    {
        var character = BuildValidFighter();
        character.RaceId = "race:elf";
        character.SubraceId = null; // Elf requires subrace

        var result = BuildValidator().Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SUBRACE_REQUIRED"));
    }

    // ── Class errors propagated ───────────────────────────────────────────

    [Fact]
    public void UnknownClass_IsReportedInFullValidation()
    {
        var character = BuildValidFighter();
        character.Levels[0].ClassId = "class:does-not-exist";

        var result = BuildValidator().Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_CLASS_UNKNOWN"));
    }

    // ── Spell validation in full pipeline ─────────────────────────────────

    [Fact]
    public void WizardWithValidSpells_PassesFullValidation()
    {
        var character = new Character
        {
            Name = "Elara",
            RaceId = "race:elf",
            SubraceId = "race:elf:high-elf",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            BackgroundId = "background:sage",
            GenerationMethod = GenerationMethod.StandardArray,
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 8, RacialBonus = 0 },
                DEX = new AbilityBlock { Base = 14, RacialBonus = 2 },
                CON = new AbilityBlock { Base = 13, RacialBonus = 0 },
                INT = new AbilityBlock { Base = 15, RacialBonus = 1 },
                WIS = new AbilityBlock { Base = 12, RacialBonus = 0 },
                CHA = new AbilityBlock { Base = 10, RacialBonus = 0 },
            },
            Skills = new Dictionary<string, string>
            {
                ["skill:arcana"] = "background",
                ["skill:history"] = "background",
                ["skill:insight"] = "class",
                ["skill:medicine"] = "class",
            },
            Spells =
            [
                new CharacterSpell { ClassId = "class:wizard", SpellId = "spell:fire-bolt", Prepared = false },
                new CharacterSpell { ClassId = "class:wizard", SpellId = "spell:magic-missile", Prepared = true },
                new CharacterSpell { ClassId = "class:wizard", SpellId = "spell:shield", Prepared = true },
                new CharacterSpell { ClassId = "class:wizard", SpellId = "spell:sleep", Prepared = true },
                new CharacterSpell { ClassId = "class:wizard", SpellId = "spell:detect-magic", Prepared = true },
                new CharacterSpell { ClassId = "class:wizard", SpellId = "spell:burning-hands", Prepared = true },
                new CharacterSpell { ClassId = "class:wizard", SpellId = "spell:color-spray", Prepared = true },
            ],
        };

        var result = BuildValidator().Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void UnknownSpell_IsReportedInFullValidation()
    {
        var character = new Character
        {
            Name = "Elara",
            RaceId = "race:elf",
            SubraceId = "race:elf:high-elf",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            BackgroundId = "background:sage",
            GenerationMethod = GenerationMethod.StandardArray,
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 8 },
                DEX = new AbilityBlock { Base = 14, RacialBonus = 2 },
                CON = new AbilityBlock { Base = 13 },
                INT = new AbilityBlock { Base = 15, RacialBonus = 1 },
                WIS = new AbilityBlock { Base = 12 },
                CHA = new AbilityBlock { Base = 10 },
            },
            Skills = new Dictionary<string, string>
            {
                ["skill:arcana"] = "background",
                ["skill:history"] = "background",
                ["skill:insight"] = "class",
                ["skill:medicine"] = "class",
            },
            Spells =
            [
                new CharacterSpell { ClassId = "class:wizard", SpellId = "spell:does-not-exist", Prepared = false },
            ],
        };

        var result = BuildValidator().Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_SPELL_UNKNOWN"));
    }

    // ── Equipment validation in full pipeline ─────────────────────────────

    [Fact]
    public void ValidEquipment_PassesFullValidation()
    {
        var character = BuildValidFighter();
        character.Equipment.Add(new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 });

        var result = BuildValidator().Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void UnknownEquipment_IsReportedInFullValidation()
    {
        var character = BuildValidFighter();
        character.Equipment.Add(new CharacterEquipmentItem { ItemId = "item:does-not-exist", Quantity = 1 });

        var result = BuildValidator().Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_EQUIPMENT_UNKNOWN"));
    }

    // ── NoSpells/NoEquipment — validators are skipped (no false errors) ───

    [Fact]
    public void CharacterWithNoSpells_SpellValidationSkipped()
    {
        var character = BuildValidFighter();
        // No spells assigned — spell validation must not run or error out
        var result = BuildValidator().Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void CharacterWithNoEquipment_EquipmentValidationSkipped()
    {
        var character = BuildValidFighter();
        // No equipment assigned — equipment validation must not run or error out
        Assert.Empty(character.Equipment);
        var result = BuildValidator().Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }
}

/// <summary>
/// Tests for JSON serialization/deserialization of the Character model,
/// covering the export round-trip that the wizard uses.
/// </summary>
public class CharacterExportSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static Character BuildSampleCharacter() => new()
    {
        Id = "char:test-001",
        Name = "Aric Stonehammer",
        PlayerName = "Alice",
        Campaign = "Lost Mines",
        TotalLevel = 1,
        GenerationMethod = GenerationMethod.StandardArray,
        RaceId = "race:human",
        SubraceId = null,
        BackgroundId = "background:soldier",
        Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
        AbilityScores = new AbilityScores
        {
            STR = new AbilityBlock { Base = 15, RacialBonus = 1 },
            DEX = new AbilityBlock { Base = 14, RacialBonus = 1 },
            CON = new AbilityBlock { Base = 13, RacialBonus = 1 },
            INT = new AbilityBlock { Base = 12, RacialBonus = 1 },
            WIS = new AbilityBlock { Base = 10, RacialBonus = 1 },
            CHA = new AbilityBlock { Base = 8, RacialBonus = 1 },
        },
        Skills = new Dictionary<string, string>
        {
            ["skill:perception"] = "class",
            ["skill:survival"] = "class",
            ["skill:athletics"] = "background",
            ["skill:intimidation"] = "background",
        },
        Equipment =
        [
            new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 },
        ],
    };

    [Fact]
    public void Character_SerializesToJson_WithoutThrowing()
    {
        var character = BuildSampleCharacter();
        var json = JsonSerializer.Serialize(character, JsonOptions);

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("aric stonehammer", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Character_RoundTrip_PreservesAllFields()
    {
        var original = BuildSampleCharacter();
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var restored = JsonSerializer.Deserialize<Character>(json, JsonOptions);

        Assert.NotNull(restored);
        Assert.Equal(original.Name, restored!.Name);
        Assert.Equal(original.PlayerName, restored.PlayerName);
        Assert.Equal(original.Campaign, restored.Campaign);
        Assert.Equal(original.TotalLevel, restored.TotalLevel);
        Assert.Equal(original.RaceId, restored.RaceId);
        Assert.Equal(original.BackgroundId, restored.BackgroundId);
        Assert.Equal(original.GenerationMethod, restored.GenerationMethod);
        Assert.Equal(original.Skills.Count, restored.Skills.Count);
        Assert.Equal(original.Equipment.Count, restored.Equipment.Count);
    }

    [Fact]
    public void Character_RoundTrip_PreservesAbilityScores()
    {
        var original = BuildSampleCharacter();
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var restored = JsonSerializer.Deserialize<Character>(json, JsonOptions);

        Assert.NotNull(restored);
        Assert.Equal(original.AbilityScores.STR.Base, restored!.AbilityScores.STR.Base);
        Assert.Equal(original.AbilityScores.STR.RacialBonus, restored.AbilityScores.STR.RacialBonus);
        Assert.Equal(original.AbilityScores.INT.Base, restored.AbilityScores.INT.Base);
    }

    [Fact]
    public void Character_RoundTrip_PreservesEquipment()
    {
        var original = BuildSampleCharacter();
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var restored = JsonSerializer.Deserialize<Character>(json, JsonOptions);

        Assert.NotNull(restored);
        Assert.Single(restored!.Equipment);
        Assert.Equal("item:longsword", restored.Equipment[0].ItemId);
        Assert.Equal(1, restored.Equipment[0].Quantity);
    }

    [Fact]
    public void Character_RoundTrip_PreservesSkills()
    {
        var original = BuildSampleCharacter();
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var restored = JsonSerializer.Deserialize<Character>(json, JsonOptions);

        Assert.NotNull(restored);
        Assert.Equal(4, restored!.Skills.Count);
        Assert.Equal("class", restored.Skills["skill:perception"]);
        Assert.Equal("background", restored.Skills["skill:athletics"]);
    }

    [Fact]
    public void Character_RoundTrip_PreservesLevels()
    {
        var original = BuildSampleCharacter();
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var restored = JsonSerializer.Deserialize<Character>(json, JsonOptions);

        Assert.NotNull(restored);
        Assert.Single(restored!.Levels);
        Assert.Equal("class:fighter", restored.Levels[0].ClassId);
        Assert.Equal(1, restored.Levels[0].Level);
    }

    [Fact]
    public void Character_WithSpells_RoundTrip_PreservesSpells()
    {
        var original = BuildSampleCharacter();
        original.Spells.Add(new CharacterSpell
        {
            ClassId = "class:wizard",
            SpellId = "spell:fire-bolt",
            Prepared = false,
        });
        original.Spells.Add(new CharacterSpell
        {
            ClassId = "class:wizard",
            SpellId = "spell:magic-missile",
            Prepared = true,
        });

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var restored = JsonSerializer.Deserialize<Character>(json, JsonOptions);

        Assert.NotNull(restored);
        Assert.Equal(2, restored!.Spells.Count);
        Assert.Contains(restored.Spells, s => s.SpellId == "spell:fire-bolt" && !s.Prepared);
        Assert.Contains(restored.Spells, s => s.SpellId == "spell:magic-missile" && s.Prepared);
    }

    [Fact]
    public void Character_EmptyCharacter_SerializesAndDeserializesCleanly()
    {
        var empty = new Character();
        var json = JsonSerializer.Serialize(empty, JsonOptions);
        var restored = JsonSerializer.Deserialize<Character>(json, JsonOptions);

        Assert.NotNull(restored);
        Assert.Equal(string.Empty, restored!.Name);
        Assert.Empty(restored.Levels);
        Assert.Empty(restored.Skills);
        Assert.Empty(restored.Equipment);
        Assert.Empty(restored.Spells);
    }

    [Fact]
    public void AbilityBlock_Final_IsCorrectlySummed()
    {
        var block = new AbilityBlock { Base = 15, RacialBonus = 1, OtherBonus = 2 };
        Assert.Equal(18, block.Final);
    }

    [Fact]
    public void AbilityBlock_NoBonus_FinalEqualsBase()
    {
        var block = new AbilityBlock { Base = 14 };
        Assert.Equal(14, block.Final);
    }
}
