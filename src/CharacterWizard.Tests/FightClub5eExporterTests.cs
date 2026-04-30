using System.Xml.Linq;
using CharacterWizard.Shared.Export;
using CharacterWizard.Shared.Models;

namespace CharacterWizard.Tests;

public class FightClub5eExporterTests
{
    // ── Seed data ────────────────────────────────────────────────────────

    private static readonly List<RaceDefinition> TestRaces =
    [
        new RaceDefinition
        {
            Id = "race:human",
            DisplayName = "Human",
            Speed = 30,
            AbilityBonuses = new Dictionary<string, int>
            {
                ["STR"] = 1, ["DEX"] = 1, ["CON"] = 1, ["INT"] = 1, ["WIS"] = 1, ["CHA"] = 1,
            },
            Subraces = [],
        },
        new RaceDefinition
        {
            Id = "race:dwarf",
            DisplayName = "Dwarf",
            Speed = 25,
            AbilityBonuses = new Dictionary<string, int> { ["CON"] = 2 },
            Subraces =
            [
                new SubraceDefinition
                {
                    Id = "subrace:hill-dwarf",
                    DisplayName = "Hill Dwarf",
                    AbilityBonuses = new Dictionary<string, int> { ["WIS"] = 1 },
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
            SkillChoices = new SkillChoices { Count = 2, Options = ["skill:athletics", "skill:intimidation"] },
            MulticlassPrereqs = new Dictionary<string, int> { ["STR"] = 13 },
            SubclassLevel = 3,
        },
        new ClassDefinition
        {
            Id = "class:wizard",
            DisplayName = "Wizard",
            HitDie = 6,
            SavingThrows = ["INT", "WIS"],
            SkillChoices = new SkillChoices { Count = 2, Options = ["skill:arcana", "skill:history"] },
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
        },
    ];

    private static readonly List<SpellDefinition> TestSpells =
    [
        new SpellDefinition
        {
            Id = "spell:fire-bolt",
            DisplayName = "Fire Bolt",
            Description = "Hurls a mote of fire.",
            Level = 0,
            School = "Evocation",
            CastingTime = "1 action",
            Range = "120 feet",
            Duration = "Instantaneous",
            Components = new SpellComponents { Verbal = true, Somatic = true },
            ClassIds = ["class:wizard"],
        },
        new SpellDefinition
        {
            Id = "spell:magic-missile",
            DisplayName = "Magic Missile",
            Description = "Darts of magical force.",
            Level = 1,
            School = "Evocation",
            CastingTime = "1 action",
            Range = "120 feet",
            Duration = "Instantaneous",
            Components = new SpellComponents { Verbal = true, Somatic = true },
            ClassIds = ["class:wizard"],
        },
    ];

    private static readonly List<EquipmentItemDefinition> TestEquipment =
    [
        new EquipmentItemDefinition { Id = "item:longsword", DisplayName = "Longsword", Category = "weapon", Subcategory = "martial-melee" },
        new EquipmentItemDefinition { Id = "item:chain-mail", DisplayName = "Chain Mail", Category = "armor", Subcategory = "heavy" },
    ];

    private FightClub5eExporter CreateExporter() =>
        new(TestRaces, TestClasses, TestBackgrounds, TestSpells, TestEquipment);

    private static XElement ParseCharacter(string xml)
    {
        var doc = XDocument.Parse(xml);
        return doc.Root!.Element("character")!;
    }

    // ── Happy path – single class ─────────────────────────────────────────

    [Fact]
    public void Export_SingleClassFighter_ContainsCorrectBasicFields()
    {
        var character = new Character
        {
            Name = "Aric",
            PlayerName = "Dave",
            RaceId = "race:human",
            BackgroundId = "background:soldier",
            TotalLevel = 1,
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
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("Aric", c.Element("name")!.Value);
        Assert.Equal("Dave", c.Element("player")!.Value);
        Assert.Equal("Human", c.Element("race")!.Value);
        Assert.Equal("Soldier", c.Element("background")!.Value);
        Assert.Equal("Fighter 1", c.Element("class")!.Value);
        Assert.Equal("1", c.Element("level")!.Value);
        Assert.Equal("16", c.Element("str")!.Value);
        Assert.Equal("15", c.Element("dex")!.Value);
        Assert.Equal("14", c.Element("con")!.Value);
        Assert.Equal("13", c.Element("int")!.Value);
        Assert.Equal("11", c.Element("wis")!.Value);
        Assert.Equal("9", c.Element("cha")!.Value);
    }

    [Fact]
    public void Export_HasXmlDeclaration()
    {
        var character = new Character { Name = "Test", TotalLevel = 1, Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }] };
        var xml = CreateExporter().Export(character);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", xml);
    }

    [Fact]
    public void Export_HasDocumentRoot()
    {
        var character = new Character { Name = "Test", TotalLevel = 1, Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }] };
        var xml = CreateExporter().Export(character);
        var doc = XDocument.Parse(xml);
        Assert.Equal("document", doc.Root!.Name.LocalName);
        Assert.NotNull(doc.Root.Element("character"));
    }

    // ── Multiclass ────────────────────────────────────────────────────────

    [Fact]
    public void Export_Multiclass_ClassStringContainsBothClasses()
    {
        var character = new Character
        {
            Name = "Myra",
            TotalLevel = 5,
            Levels =
            [
                new ClassLevel { ClassId = "class:fighter", Level = 3 },
                new ClassLevel { ClassId = "class:wizard", Level = 2 },
            ],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("Fighter 3/Wizard 2", c.Element("class")!.Value);
        Assert.Equal("5", c.Element("level")!.Value);
    }

    // ── Saving throws ─────────────────────────────────────────────────────

    [Fact]
    public void Export_SavingThrows_MatchClassDefinition()
    {
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var saves = c.Elements("save").Select(e => e.Value).ToList();
        Assert.Contains("Strength", saves);
        Assert.Contains("Constitution", saves);
        Assert.Equal(2, saves.Count);
    }

    [Fact]
    public void Export_MulticlassSavingThrows_AreDeduplicated()
    {
        var character = new Character
        {
            Name = "Myra",
            TotalLevel = 5,
            Levels =
            [
                new ClassLevel { ClassId = "class:fighter", Level = 3 },
                new ClassLevel { ClassId = "class:wizard", Level = 2 },
            ],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var saves = c.Elements("save").Select(e => e.Value).ToList();
        // Fighter: STR, CON; Wizard: INT, WIS — 4 unique saves, no duplicates
        Assert.Equal(4, saves.Count);
        Assert.Contains("Strength", saves);
        Assert.Contains("Constitution", saves);
        Assert.Contains("Intelligence", saves);
        Assert.Contains("Wisdom", saves);
    }

    // ── Skill proficiencies ───────────────────────────────────────────────

    [Fact]
    public void Export_SkillProficiencies_MatchCharacterSkills()
    {
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
            Skills = new Dictionary<string, string>
            {
                ["skill:athletics"] = "class",
                ["skill:intimidation"] = "background",
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var skillProfs = c.Elements("skillprof").Select(e => e.Attribute("name")!.Value).ToList();
        Assert.Contains("Athletics", skillProfs);
        Assert.Contains("Intimidation", skillProfs);
        Assert.Equal(2, skillProfs.Count);
    }

    // ── Spells ────────────────────────────────────────────────────────────

    [Fact]
    public void Export_Spells_CantripAndLevelOneSpellBothPresent()
    {
        var character = new Character
        {
            Name = "Myra",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 12 },
                DEX = new AbilityBlock { Base = 14 },
                WIS = new AbilityBlock { Base = 10 },
            },
            Spells =
            [
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "class:wizard", Prepared = false },
                new CharacterSpell { SpellId = "spell:magic-missile", ClassId = "class:wizard", Prepared = true },
            ],
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var spells = c.Elements("spell").ToList();
        Assert.Equal(2, spells.Count);

        var fireBolt = spells.First(s => s.Element("name")!.Value == "Fire Bolt");
        Assert.Equal("0", fireBolt.Element("level")!.Value);
        Assert.Equal("Evocation", fireBolt.Element("school")!.Value);
        Assert.Equal("1 action", fireBolt.Element("time")!.Value);
        Assert.Equal("120 feet", fireBolt.Element("range")!.Value);
        Assert.Equal("V, S", fireBolt.Element("components")!.Value);

        var magicMissile = spells.First(s => s.Element("name")!.Value == "Magic Missile");
        Assert.Equal("1", magicMissile.Element("level")!.Value);
    }

    // ── Equipment ─────────────────────────────────────────────────────────

    [Fact]
    public void Export_Equipment_UsesDisplayName()
    {
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 },
                new CharacterEquipmentItem { ItemId = "item:chain-mail", Quantity = 1 },
            ],
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var items = c.Elements("item").ToList();
        Assert.Equal(2, items.Count);

        var names = items.Select(i => i.Element("name")!.Value).ToList();
        Assert.Contains("Longsword", names);
        Assert.Contains("Chain Mail", names);
    }

    [Fact]
    public void Export_Equipment_QuantityIsPreserved()
    {
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 3 },
            ],
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var item = c.Element("item")!;
        Assert.Equal("3", item.Element("quantity")!.Value);
    }

    // ── Empty / optional fields ───────────────────────────────────────────

    [Fact]
    public void Export_AlignmentIsAlwaysPresent_AndEmpty()
    {
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var alignment = c.Element("alignment");
        Assert.NotNull(alignment);
        Assert.Equal(string.Empty, alignment.Value);
    }

    [Fact]
    public void Export_PlayerIsPresent_AndEmptyWhenNotSet()
    {
        var character = new Character
        {
            Name = "Aric",
            PlayerName = null,
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var player = c.Element("player");
        Assert.NotNull(player);
        Assert.Equal(string.Empty, player.Value);
    }

    // ── Derived stats ─────────────────────────────────────────────────────

    [Fact]
    public void Export_ProficiencyBonus_IsCorrectForLevel1()
    {
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("2", c.Element("proficiencybonus")!.Value);
    }

    [Fact]
    public void Export_ProficiencyBonus_IsCorrectForLevel5()
    {
        var character = new Character
        {
            Name = "Myra",
            TotalLevel = 5,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 5 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("3", c.Element("proficiencybonus")!.Value);
    }

    [Fact]
    public void Export_MaxHp_CalculatedUsingMaxDiePerLevel()
    {
        // Fighter 1, CON 14 (mod +2): max HP = 1 × max(1, 10 + 2) = 12
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var hp = c.Element("hp")!;
        Assert.Equal("12", hp.Attribute("max")!.Value);
        Assert.Equal("12", hp.Attribute("current")!.Value);
        Assert.Equal("0", hp.Attribute("temp")!.Value);
    }

    [Fact]
    public void Export_MaxHp_MulticlassIsCorrect()
    {
        // Fighter 3 (d10), Wizard 2 (d6), CON 16 (mod +3)
        // HP = 3 × max(1, 10+3) + 2 × max(1, 6+3) = 3×13 + 2×9 = 39 + 18 = 57
        var character = new Character
        {
            Name = "Myra",
            TotalLevel = 5,
            Levels =
            [
                new ClassLevel { ClassId = "class:fighter", Level = 3 },
                new ClassLevel { ClassId = "class:wizard", Level = 2 },
            ],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 16 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("57", c.Element("hp")!.Attribute("max")!.Value);
    }

    [Fact]
    public void Export_Speed_ComesFromRaceDefinition()
    {
        var character = new Character
        {
            Name = "Thorin",
            RaceId = "race:dwarf",
            SubraceId = "subrace:hill-dwarf",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 10 },
                WIS = new AbilityBlock { Base = 12 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("25", c.Element("speed")!.Value);
    }

    [Fact]
    public void Export_Race_UsesSubraceDisplayNameWhenPresent()
    {
        var character = new Character
        {
            Name = "Thorin",
            RaceId = "race:dwarf",
            SubraceId = "subrace:hill-dwarf",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 10 },
                WIS = new AbilityBlock { Base = 12 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("Hill Dwarf", c.Element("race")!.Value);
    }

    [Fact]
    public void Export_PassivePerception_IncludesProficiencyBonusWhenProficient()
    {
        // WIS 14 (mod +2), perception proficient, level 1 (prof bonus +2)
        // passive perception = 10 + 2 + 2 = 14
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 14 },
            },
            Skills = new Dictionary<string, string> { ["skill:perception"] = "class" },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("14", c.Element("passiveperception")!.Value);
    }

    [Fact]
    public void Export_PassivePerception_WithoutProficiency_OmitsProficiencyBonus()
    {
        // WIS 14 (mod +2), no perception proficiency
        // passive perception = 10 + 2 = 12
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 14 },
            },
            Skills = new Dictionary<string, string>(),
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("12", c.Element("passiveperception")!.Value);
    }
}
