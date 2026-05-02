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
        new SpellDefinition
        {
            Id = "spell:identify",
            DisplayName = "Identify",
            Description = "Identify a magic item.",
            Level = 1,
            School = "Divination",
            CastingTime = "1 minute",
            Range = "Touch",
            Duration = "Instantaneous",
            Components = new SpellComponents { Verbal = true, Somatic = true, Material = "a pearl worth at least 100 gp" },
            Ritual = true,
            ClassIds = ["class:wizard"],
        },
        new SpellDefinition
        {
            Id = "spell:detect-magic",
            DisplayName = "Detect Magic",
            Description = "Sense the presence of magic.",
            Level = 1,
            School = "Divination",
            CastingTime = "1 action",
            Range = "Self",
            Duration = "10 minutes",
            Components = new SpellComponents { Verbal = true, Somatic = true },
            Concentration = true,
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
        Assert.Equal("Human", c.Element("race")!.Element("name")!.Value);
        Assert.Equal("Soldier", c.Element("background")!.Element("name")!.Value);
        Assert.Equal("Fighter", c.Element("class")!.Element("name")!.Value);
        Assert.Equal("1", c.Element("class")!.Element("level")!.Value);

        var abilities = c.Element("abilities")!.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("16", abilities[0]); // STR
        Assert.Equal("15", abilities[1]); // DEX
        Assert.Equal("14", abilities[2]); // CON
        Assert.Equal("13", abilities[3]); // INT
        Assert.Equal("11", abilities[4]); // WIS
        Assert.Equal("9", abilities[5]);  // CHA
    }

    [Fact]
    public void Export_HasXmlDeclaration()
    {
        var character = new Character { Name = "Test", TotalLevel = 1, Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }] };
        var xml = CreateExporter().Export(character);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", xml);
    }

    [Fact]
    public void Export_HasPcRoot()
    {
        var character = new Character { Name = "Test", TotalLevel = 1, Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }] };
        var xml = CreateExporter().Export(character);
        var doc = XDocument.Parse(xml);
        Assert.Equal("pc", doc.Root!.Name.LocalName);
        Assert.Equal("5", doc.Root!.Attribute("version")!.Value);
        Assert.NotNull(doc.Root.Element("character"));
    }

    // ── Multiclass ────────────────────────────────────────────────────────

    [Fact]
    public void Export_Multiclass_HasSeparateClassElementsForEachClass()
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

        var classes = c.Elements("class").ToList();
        Assert.Equal(2, classes.Count);
        Assert.Equal("Fighter", classes[0].Element("name")!.Value);
        Assert.Equal("3", classes[0].Element("level")!.Value);
        Assert.Equal("Wizard", classes[1].Element("name")!.Value);
        Assert.Equal("2", classes[1].Element("level")!.Value);
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

        // Fighter saves: STR (0) and CON (2) stored as numeric <proficiency> inside <class>
        var classProfs = c.Element("class")!.Elements("proficiency").Select(e => e.Value).ToList();
        Assert.Contains("0", classProfs); // STR
        Assert.Contains("2", classProfs); // CON
        Assert.Equal(2, classProfs.Count(p => int.Parse(p) < 6)); // only the two save proficiencies
    }

    [Fact]
    public void Export_MulticlassSavingThrows_AreStoredPerClass()
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

        var classes = c.Elements("class").ToList();

        // Fighter class: STR (0) and CON (2)
        var fighterProfs = classes[0].Elements("proficiency").Select(e => e.Value).ToList();
        Assert.Contains("0", fighterProfs);
        Assert.Contains("2", fighterProfs);

        // Wizard class: INT (3) and WIS (4)
        var wizardProfs = classes[1].Elements("proficiency").Select(e => e.Value).ToList();
        Assert.Contains("3", wizardProfs);
        Assert.Contains("4", wizardProfs);
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

        // Athletics (103) from class → inside <class>
        var classProfs = c.Element("class")!.Elements("proficiency").Select(e => e.Value).ToList();
        Assert.Contains("103", classProfs); // Athletics

        // Intimidation (107) from background → inside <background>
        var bgProfs = c.Element("background")!.Elements("proficiency").Select(e => e.Value).ToList();
        Assert.Contains("107", bgProfs); // Intimidation
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
        Assert.Equal("EV", fireBolt.Element("school")!.Value);
        Assert.Equal("NO", fireBolt.Element("ritual")!.Value);
        Assert.Equal("1 action", fireBolt.Element("time")!.Value);
        Assert.Equal("120 feet", fireBolt.Element("range")!.Value);
        Assert.Equal("V, S", fireBolt.Element("components")!.Value);
        Assert.Equal("Instantaneous", fireBolt.Element("duration")!.Value);
        Assert.Equal("Wizard", fireBolt.Element("classes")!.Value);
        Assert.Equal("NO", fireBolt.Element("prepared")!.Value);

        var magicMissile = spells.First(s => s.Element("name")!.Value == "Magic Missile");
        Assert.Equal("1", magicMissile.Element("level")!.Value);
        Assert.Equal("YES", magicMissile.Element("prepared")!.Value);
    }

    [Fact]
    public void Export_Spells_RitualSpell_EmitsYesForRitual()
    {
        var character = new Character
        {
            Name = "Myra",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 12 } },
            Spells =
            [
                new CharacterSpell { SpellId = "spell:identify", ClassId = "class:wizard", Prepared = true },
            ],
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var identify = c.Elements("spell").First(s => s.Element("name")!.Value == "Identify");
        Assert.Equal("YES", identify.Element("ritual")!.Value);
        Assert.Equal("D", identify.Element("school")!.Value);
    }

    [Fact]
    public void Export_Spells_ConcentrationSpell_PrefixesDuration()
    {
        var character = new Character
        {
            Name = "Myra",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 12 } },
            Spells =
            [
                new CharacterSpell { SpellId = "spell:detect-magic", ClassId = "class:wizard", Prepared = true },
            ],
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var detectMagic = c.Elements("spell").First(s => s.Element("name")!.Value == "Detect Magic");
        Assert.Equal("Concentration, up to 10 minutes", detectMagic.Element("duration")!.Value);
    }

    [Fact]
    public void Export_Spells_ClassesElementUsesClassDisplayName()
    {
        var character = new Character
        {
            Name = "Myra",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 12 } },
            Spells =
            [
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "class:wizard", Prepared = false },
            ],
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        var spell = c.Elements("spell").First(s => s.Element("name")!.Value == "Fire Bolt");
        Assert.Equal("Wizard", spell.Element("classes")!.Value);
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
    public void Export_AlignmentIsInsideBackground_AndEmpty()
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

        var align = c.Element("background")!.Element("align");
        Assert.NotNull(align);
        Assert.Equal(string.Empty, align.Value);
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

    // ── HP ────────────────────────────────────────────────────────────────

    [Fact]
    public void Export_MaxHp_FallbackLevel1_UsesMaxDieValue()
    {
        // Fighter 1 (no HitPointEntries — fallback algorithm). Level 1 always uses max die.
        // CON 14 (mod +2): HP = max(1, 10 + 2) = 12
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

        Assert.Equal("12", c.Element("hpMax")!.Value);
        Assert.Equal("12", c.Element("hpCurrent")!.Value);
    }

    [Fact]
    public void Export_MaxHp_FallbackLevels2Plus_UsesFixedAverage()
    {
        // Fighter 3 (no HitPointEntries — fallback algorithm). CON 10 (mod 0).
        // lvl1 = max die = 10; lvls 2-3 = 2 × (floor(10/2)+1) = 2×6 = 12 → total = 22
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 3,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 10 },
                DEX = new AbilityBlock { Base = 10 },
                WIS = new AbilityBlock { Base = 10 },
            },
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("22", c.Element("hpMax")!.Value);
    }

    [Fact]
    public void Export_MaxHp_MulticlassIsCorrect()
    {
        // Fighter 3 (d10), Wizard 2 (d6), CON 16 (mod +3) — no HitPointEntries, uses fixed-average fallback
        // Fighter avg = floor(10/2)+1 = 6; lvl1=max(1,10+3)=13; lvls2-3=2×max(1,6+3)=2×9=18 → 31
        // Wizard  avg = floor(6/2)+1  = 4; lvl1=max(1,6+3)=9;   lvl2  =max(1,4+3)=7        → 16
        // Total = 31+16 = 47
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

        Assert.Equal("47", c.Element("hpMax")!.Value);
    }

    [Fact]
    public void Export_MaxHp_UsesHitPointEntriesWithManualAndAverageRolls()
    {
        // Fighter 3 (d10), CON 14 (mod +2).
        // HitPointEntries: lvl1=10, lvl2=6 (average), lvl3=9 (manual)
        // Max HP = max(1,10+2) + max(1,6+2) + max(1,9+2) = 12 + 8 + 11 = 31
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 3,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 10 },
                WIS = new AbilityBlock { Base = 10 },
            },
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 1, Method = "average", DieRollValue = 10 },
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 2, Method = "average", DieRollValue = 6 },
                new HitPointEntry { ClassId = "class:fighter", ClassLevel = 3, Method = "manual",  DieRollValue = 9 },
            ],
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("31", c.Element("hpMax")!.Value);
        Assert.Equal("31", c.Element("hpCurrent")!.Value);
    }

    [Fact]
    public void Export_MaxHp_FallsBackToAverageAlgorithmWhenNoEntries()
    {
        // Fighter 1 only — level 1 always uses max die under the fixed-average algorithm.
        // CON 14 (mod +2): max HP = max(1, 10+2) = 12
        var character = new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14 },
                DEX = new AbilityBlock { Base = 10 },
                WIS = new AbilityBlock { Base = 10 },
            },
            HitPointEntries = [], // no entries
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("12", c.Element("hpMax")!.Value);
    }

    [Fact]
    public void Export_MaxHp_WithEntriesAppliesConModPerEntry()
    {
        // Wizard 2 (d6), CON 8 (mod -1).
        // lvl1=6, lvl2=3 → max(1,6-1) + max(1,3-1) = 5 + 2 = 7
        var character = new Character
        {
            Name = "Zara",
            TotalLevel = 2,
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 2 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 8 },
                DEX = new AbilityBlock { Base = 10 },
                WIS = new AbilityBlock { Base = 14 },
            },
            HitPointEntries =
            [
                new HitPointEntry { ClassId = "class:wizard", ClassLevel = 1, Method = "average", DieRollValue = 6 },
                new HitPointEntry { ClassId = "class:wizard", ClassLevel = 2, Method = "manual",  DieRollValue = 3 },
            ],
        };

        var xml = CreateExporter().Export(character);
        var c = ParseCharacter(xml);

        Assert.Equal("7", c.Element("hpMax")!.Value);
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

        Assert.Equal("25", c.Element("race")!.Element("speed")!.Value);
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

        Assert.Equal("Hill Dwarf", c.Element("race")!.Element("name")!.Value);
    }
}
