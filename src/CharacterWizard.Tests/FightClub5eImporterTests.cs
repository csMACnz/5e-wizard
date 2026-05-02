using System.Xml.Linq;
using CharacterWizard.Shared.Export;
using CharacterWizard.Shared.Models;

namespace CharacterWizard.Tests;

public class FightClub5eImporterTests
{
    // ── Shared seed data (mirrors FightClub5eExporterTests) ──────────────────

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

    private static readonly List<FeatDefinition> TestFeats =
    [
        new FeatDefinition { Id = "feat:military-rank", DisplayName = "Military Rank", Type = "background", Source = "srd" },
        new FeatDefinition { Id = "feat:darkvision", DisplayName = "Darkvision", Type = "class", Source = "srd" },
    ];

    private FightClub5eImporter CreateImporter(bool withFeats = false) =>
        new(TestRaces, TestClasses, TestBackgrounds, TestSpells, TestEquipment,
            withFeats ? TestFeats : null);

    private FightClub5eExporter CreateExporter(bool withFeats = false) =>
        new(TestRaces, TestClasses, TestBackgrounds, TestSpells, TestEquipment,
            withFeats ? TestFeats : null);

    // ── Basic import – single class ──────────────────────────────────────────

    [Fact]
    public void Import_BasicCharacter_RestoresNameAndPlayer()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            PlayerName = "Dave",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
        });

        var result = CreateImporter().Import(xml);

        Assert.Equal("Aric", result.Name);
        Assert.Equal("Dave", result.PlayerName);
    }

    [Fact]
    public void Import_NullPlayerName_IsRestoredAsNull()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            PlayerName = null,
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
        });

        var result = CreateImporter().Import(xml);

        Assert.Null(result.PlayerName);
    }

    [Fact]
    public void Import_Race_RestoresRaceId()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            RaceId = "race:human",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
        });

        var result = CreateImporter().Import(xml);

        Assert.Equal("race:human", result.RaceId);
        Assert.Null(result.SubraceId);
    }

    [Fact]
    public void Import_Subrace_RestoresBothRaceIdAndSubraceId()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Thorin",
            RaceId = "race:dwarf",
            SubraceId = "subrace:hill-dwarf",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
        });

        var result = CreateImporter().Import(xml);

        Assert.Equal("race:dwarf", result.RaceId);
        Assert.Equal("subrace:hill-dwarf", result.SubraceId);
    }

    [Fact]
    public void Import_ClassAndLevel_AreRestored()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            TotalLevel = 3,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
        });

        var result = CreateImporter().Import(xml);

        Assert.Single(result.Levels);
        Assert.Equal("class:fighter", result.Levels[0].ClassId);
        Assert.Equal(3, result.Levels[0].Level);
        Assert.Equal(3, result.TotalLevel);
    }

    [Fact]
    public void Import_Background_IsRestored()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            BackgroundId = "background:soldier",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
        });

        var result = CreateImporter().Import(xml);

        Assert.Equal("background:soldier", result.BackgroundId);
    }

    [Fact]
    public void Import_AbilityScores_AreFinalValuesStoredAsBase()
    {
        // Export with racial bonuses; the FC5e XML stores only the final values.
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            RaceId = "race:human",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15, RacialBonus = 1 },   // Final = 16
                DEX = new AbilityBlock { Base = 14, RacialBonus = 1 },   // Final = 15
                CON = new AbilityBlock { Base = 13, RacialBonus = 1 },   // Final = 14
                INT = new AbilityBlock { Base = 12, RacialBonus = 1 },   // Final = 13
                WIS = new AbilityBlock { Base = 10, RacialBonus = 1 },   // Final = 11
                CHA = new AbilityBlock { Base = 8, RacialBonus = 1 },    // Final = 9
            },
        });

        var result = CreateImporter().Import(xml);

        // The importer stores Final values as Base (no racial split in FC5e format).
        Assert.Equal(16, result.AbilityScores.STR.Final);
        Assert.Equal(15, result.AbilityScores.DEX.Final);
        Assert.Equal(14, result.AbilityScores.CON.Final);
        Assert.Equal(13, result.AbilityScores.INT.Final);
        Assert.Equal(11, result.AbilityScores.WIS.Final);
        Assert.Equal(9, result.AbilityScores.CHA.Final);

        // Racial and other bonuses are zero after import.
        Assert.Equal(0, result.AbilityScores.STR.RacialBonus);
        Assert.Equal(0, result.AbilityScores.STR.OtherBonus);
    }

    // ── Skills ───────────────────────────────────────────────────────────────

    [Fact]
    public void Import_ClassSkills_AreTaggedWithClassSource()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
            Skills = new Dictionary<string, string>
            {
                ["skill:athletics"] = "class",
            },
        });

        var result = CreateImporter().Import(xml);

        Assert.True(result.Skills.TryGetValue("skill:athletics", out var source));
        Assert.Equal("class", source);
    }

    [Fact]
    public void Import_BackgroundSkills_AreTaggedWithBackgroundSource()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
            Skills = new Dictionary<string, string>
            {
                ["skill:intimidation"] = "background",
            },
        });

        var result = CreateImporter().Import(xml);

        Assert.True(result.Skills.TryGetValue("skill:intimidation", out var source));
        Assert.Equal("background", source);
    }

    // ── Proficiencies ─────────────────────────────────────────────────────────

    [Fact]
    public void Import_Proficiencies_ArmorWeaponsTools_AreRestored()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
            Proficiencies = new CharacterProficiencies
            {
                Armor = ["light", "medium", "heavy", "shields"],
                Weapons = ["simple", "martial"],
                Tools = ["playing card set"],
            },
        });

        var result = CreateImporter().Import(xml);

        Assert.Equal(["light", "medium", "heavy", "shields"], result.Proficiencies.Armor);
        Assert.Equal(["simple", "martial"], result.Proficiencies.Weapons);
        Assert.Equal(["playing card set"], result.Proficiencies.Tools);
    }

    // ── Equipment ────────────────────────────────────────────────────────────

    [Fact]
    public void Import_Equipment_ItemIdAndQuantityAreRestored()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 },
                new CharacterEquipmentItem { ItemId = "item:chain-mail", Quantity = 1 },
            ],
        });

        var result = CreateImporter().Import(xml);

        Assert.Equal(2, result.Equipment.Count);
        Assert.Contains(result.Equipment, e => e.ItemId == "item:longsword" && e.Quantity == 1);
        Assert.Contains(result.Equipment, e => e.ItemId == "item:chain-mail" && e.Quantity == 1);
    }

    [Fact]
    public void Import_Equipment_QuantityAboveOneIsPreserved()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
            Equipment = [new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 5 }],
        });

        var result = CreateImporter().Import(xml);

        Assert.Single(result.Equipment);
        Assert.Equal(5, result.Equipment[0].Quantity);
    }

    // ── Spells ───────────────────────────────────────────────────────────────

    [Fact]
    public void Import_Spells_SpellIdClassIdAndPreparedAreRestored()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Myra",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 12 } },
            Spells =
            [
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "class:wizard", Prepared = false },
                new CharacterSpell { SpellId = "spell:magic-missile", ClassId = "class:wizard", Prepared = true },
            ],
        });

        var result = CreateImporter().Import(xml);

        Assert.Equal(2, result.Spells.Count);

        var fireBolt = result.Spells.First(s => s.SpellId == "spell:fire-bolt");
        Assert.Equal("class:wizard", fireBolt.ClassId);
        Assert.False(fireBolt.Prepared);

        var magicMissile = result.Spells.First(s => s.SpellId == "spell:magic-missile");
        Assert.Equal("class:wizard", magicMissile.ClassId);
        Assert.True(magicMissile.Prepared);
    }

    // ── Features ─────────────────────────────────────────────────────────────

    [Fact]
    public void Import_Features_FeatureIdAndSourceIdAreRestored()
    {
        // Provide feats so the exporter writes the DisplayName and the importer can map it back.
        var xml = CreateExporter(withFeats: true).Export(new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
            Features =
            [
                new CharacterFeature { FeatureId = "feat:military-rank", SourceId = "background:soldier" },
            ],
        });

        var result = CreateImporter(withFeats: true).Import(xml);

        Assert.Single(result.Features);
        Assert.Equal("feat:military-rank", result.Features[0].FeatureId);
        Assert.Equal("background:soldier", result.Features[0].SourceId);
    }

    [Fact]
    public void Import_Features_UnknownFeatIsStoredWithRawNameAsFeatureId()
    {
        // When no feat definitions are provided (or the feat is not in the list), the raw
        // name from the XML becomes the FeatureId — matching the exporter fallback behaviour.
        var xml = CreateExporter().Export(new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
            Features =
            [
                // No feats provided to exporter, so FeatureId is written as the name.
                new CharacterFeature { FeatureId = "feat:darkvision", SourceId = "race:dwarf" },
            ],
        });

        var result = CreateImporter().Import(xml);  // no feats → no lookup

        Assert.Single(result.Features);
        // FeatureId was written verbatim by the exporter (no feats list) and read back as-is.
        Assert.Equal("feat:darkvision", result.Features[0].FeatureId);
        Assert.Equal("race:dwarf", result.Features[0].SourceId);
    }

    // ── Multiclass ────────────────────────────────────────────────────────────

    [Fact]
    public void Import_Multiclass_BothClassEntriesAreRestored()
    {
        var xml = CreateExporter().Export(new Character
        {
            Name = "Myra",
            TotalLevel = 5,
            Levels =
            [
                new ClassLevel { ClassId = "class:fighter", Level = 3 },
                new ClassLevel { ClassId = "class:wizard", Level = 2 },
            ],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
        });

        var result = CreateImporter().Import(xml);

        Assert.Equal(2, result.Levels.Count);
        Assert.Equal("class:fighter", result.Levels[0].ClassId);
        Assert.Equal(3, result.Levels[0].Level);
        Assert.Equal("class:wizard", result.Levels[1].ClassId);
        Assert.Equal(2, result.Levels[1].Level);
        Assert.Equal(5, result.TotalLevel);
    }

    // ── Export → Import → Export roundtrip ───────────────────────────────────
    //
    // After a full export→import→export cycle the produced XML must be byte-for-byte
    // identical to the original export.  The importer stores only the Final ability
    // score values (as Base), which the exporter re-reads via the Final property, so
    // all derived values (HP, abilities string, etc.) are preserved.

    [Fact]
    public void Roundtrip_ExportImportExport_SingleClassFighter_XmlIsIdentical()
    {
        var exporter = CreateExporter();
        var importer = CreateImporter();

        var original = new Character
        {
            Name = "Aric",
            PlayerName = "Dave",
            RaceId = "race:human",
            BackgroundId = "background:soldier",
            TotalLevel = 3,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3 }],
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15, RacialBonus = 1 },
                DEX = new AbilityBlock { Base = 14, RacialBonus = 1 },
                CON = new AbilityBlock { Base = 13, RacialBonus = 1 },
                INT = new AbilityBlock { Base = 12, RacialBonus = 1 },
                WIS = new AbilityBlock { Base = 10, RacialBonus = 1 },
                CHA = new AbilityBlock { Base = 8, RacialBonus = 1 },
            },
            Proficiencies = new CharacterProficiencies
            {
                Armor = ["light", "medium", "heavy", "shields"],
                Weapons = ["simple", "martial"],
                Tools = [],
            },
            Skills = new Dictionary<string, string>
            {
                ["skill:athletics"] = "class",
                ["skill:intimidation"] = "background",
            },
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 },
                new CharacterEquipmentItem { ItemId = "item:chain-mail", Quantity = 1 },
            ],
        };

        var xml1 = exporter.Export(original);
        var imported = importer.Import(xml1);
        var xml2 = exporter.Export(imported);

        Assert.Equal(xml1, xml2);
    }

    [Fact]
    public void Roundtrip_ExportImportExport_WithSubrace_XmlIsIdentical()
    {
        var exporter = CreateExporter();
        var importer = CreateImporter();

        var original = new Character
        {
            Name = "Thorin",
            RaceId = "race:dwarf",
            SubraceId = "subrace:hill-dwarf",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15 },
                DEX = new AbilityBlock { Base = 10 },
                CON = new AbilityBlock { Base = 14, RacialBonus = 2 },
                INT = new AbilityBlock { Base = 10 },
                WIS = new AbilityBlock { Base = 12, RacialBonus = 1 },
                CHA = new AbilityBlock { Base = 8 },
            },
        };

        var xml1 = exporter.Export(original);
        var imported = importer.Import(xml1);
        var xml2 = exporter.Export(imported);

        Assert.Equal(xml1, xml2);
    }

    [Fact]
    public void Roundtrip_ExportImportExport_MulticlassWithSpells_XmlIsIdentical()
    {
        var exporter = CreateExporter();
        var importer = CreateImporter();

        var original = new Character
        {
            Name = "Myra",
            RaceId = "race:human",
            BackgroundId = "background:soldier",
            TotalLevel = 5,
            Levels =
            [
                new ClassLevel { ClassId = "class:fighter", Level = 3 },
                new ClassLevel { ClassId = "class:wizard", Level = 2 },
            ],
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 14, RacialBonus = 1 },
                DEX = new AbilityBlock { Base = 12, RacialBonus = 1 },
                CON = new AbilityBlock { Base = 13, RacialBonus = 1 },
                INT = new AbilityBlock { Base = 15, RacialBonus = 1 },
                WIS = new AbilityBlock { Base = 10, RacialBonus = 1 },
                CHA = new AbilityBlock { Base = 8, RacialBonus = 1 },
            },
            Skills = new Dictionary<string, string>
            {
                ["skill:athletics"] = "class",
                ["skill:arcana"] = "class",
                ["skill:intimidation"] = "background",
            },
            Spells =
            [
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "class:wizard", Prepared = false },
                new CharacterSpell { SpellId = "spell:magic-missile", ClassId = "class:wizard", Prepared = true },
                new CharacterSpell { SpellId = "spell:detect-magic", ClassId = "class:wizard", Prepared = false },
            ],
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 },
            ],
        };

        var xml1 = exporter.Export(original);
        var imported = importer.Import(xml1);
        var xml2 = exporter.Export(imported);

        Assert.Equal(xml1, xml2);
    }

    [Fact]
    public void Roundtrip_ExportImportExport_WithFeatures_XmlIsIdentical()
    {
        var exporter = CreateExporter(withFeats: true);
        var importer = CreateImporter(withFeats: true);

        var original = new Character
        {
            Name = "Aric",
            RaceId = "race:dwarf",
            SubraceId = "subrace:hill-dwarf",
            BackgroundId = "background:soldier",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15 },
                DEX = new AbilityBlock { Base = 10 },
                CON = new AbilityBlock { Base = 14, RacialBonus = 2 },
                INT = new AbilityBlock { Base = 10 },
                WIS = new AbilityBlock { Base = 12, RacialBonus = 1 },
                CHA = new AbilityBlock { Base = 8 },
            },
            Features =
            [
                new CharacterFeature { FeatureId = "feat:darkvision", SourceId = "race:dwarf" },
                new CharacterFeature { FeatureId = "feat:military-rank", SourceId = "background:soldier" },
            ],
        };

        var xml1 = exporter.Export(original);
        var imported = importer.Import(xml1);
        var xml2 = exporter.Export(imported);

        Assert.Equal(xml1, xml2);
    }

    // ── Import → Export → Import roundtrip ───────────────────────────────────
    //
    // Starting from an already-imported character, a second import of the re-exported XML
    // must produce a character that is observationally equivalent to the first import.

    [Fact]
    public void Roundtrip_ImportExportImport_FinalAbilityScoresArePreserved()
    {
        var exporter = CreateExporter();
        var importer = CreateImporter();

        // Build a starting XML via export so the test is self-contained.
        var startXml = exporter.Export(new Character
        {
            Name = "Aric",
            RaceId = "race:human",
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
        });

        var char1 = importer.Import(startXml);
        var xml2 = exporter.Export(char1);
        var char2 = importer.Import(xml2);

        // Final values must be preserved through the second roundtrip.
        Assert.Equal(char1.AbilityScores.STR.Final, char2.AbilityScores.STR.Final);
        Assert.Equal(char1.AbilityScores.DEX.Final, char2.AbilityScores.DEX.Final);
        Assert.Equal(char1.AbilityScores.CON.Final, char2.AbilityScores.CON.Final);
        Assert.Equal(char1.AbilityScores.INT.Final, char2.AbilityScores.INT.Final);
        Assert.Equal(char1.AbilityScores.WIS.Final, char2.AbilityScores.WIS.Final);
        Assert.Equal(char1.AbilityScores.CHA.Final, char2.AbilityScores.CHA.Final);
    }

    [Fact]
    public void Roundtrip_ImportExportImport_ClassRaceBackgroundArePreserved()
    {
        var exporter = CreateExporter();
        var importer = CreateImporter();

        var startXml = exporter.Export(new Character
        {
            Name = "Thorin",
            PlayerName = "Sarah",
            RaceId = "race:dwarf",
            SubraceId = "subrace:hill-dwarf",
            BackgroundId = "background:soldier",
            TotalLevel = 2,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 2 }],
            AbilityScores = new AbilityScores
            {
                CON = new AbilityBlock { Base = 14, RacialBonus = 2 },
                WIS = new AbilityBlock { Base = 12, RacialBonus = 1 },
            },
        });

        var char1 = importer.Import(startXml);
        var xml2 = exporter.Export(char1);
        var char2 = importer.Import(xml2);

        Assert.Equal(char1.Name, char2.Name);
        Assert.Equal(char1.PlayerName, char2.PlayerName);
        Assert.Equal(char1.RaceId, char2.RaceId);
        Assert.Equal(char1.SubraceId, char2.SubraceId);
        Assert.Equal(char1.BackgroundId, char2.BackgroundId);
        Assert.Equal(char1.TotalLevel, char2.TotalLevel);
        Assert.Equal(char1.Levels[0].ClassId, char2.Levels[0].ClassId);
        Assert.Equal(char1.Levels[0].Level, char2.Levels[0].Level);
    }

    [Fact]
    public void Roundtrip_ImportExportImport_SkillsAndProficienciesArePreserved()
    {
        var exporter = CreateExporter();
        var importer = CreateImporter();

        var startXml = exporter.Export(new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
            Proficiencies = new CharacterProficiencies
            {
                Armor = ["light", "medium"],
                Weapons = ["simple"],
                Tools = ["herbalism kit"],
            },
            Skills = new Dictionary<string, string>
            {
                ["skill:athletics"] = "class",
                ["skill:intimidation"] = "background",
            },
        });

        var char1 = importer.Import(startXml);
        var xml2 = exporter.Export(char1);
        var char2 = importer.Import(xml2);

        Assert.Equal(char1.Proficiencies.Armor, char2.Proficiencies.Armor);
        Assert.Equal(char1.Proficiencies.Weapons, char2.Proficiencies.Weapons);
        Assert.Equal(char1.Proficiencies.Tools, char2.Proficiencies.Tools);
        Assert.Equal(char1.Skills, char2.Skills);
    }

    [Fact]
    public void Roundtrip_ImportExportImport_EquipmentAndSpellsArePreserved()
    {
        var exporter = CreateExporter();
        var importer = CreateImporter();

        var startXml = exporter.Export(new Character
        {
            Name = "Myra",
            TotalLevel = 2,
            Levels = [new ClassLevel { ClassId = "class:wizard", Level = 2 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 12 } },
            Equipment = [new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 2 }],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "class:wizard", Prepared = false },
                new CharacterSpell { SpellId = "spell:magic-missile", ClassId = "class:wizard", Prepared = true },
            ],
        });

        var char1 = importer.Import(startXml);
        var xml2 = exporter.Export(char1);
        var char2 = importer.Import(xml2);

        Assert.Equal(char1.Equipment.Count, char2.Equipment.Count);
        Assert.Equal(char1.Equipment[0].ItemId, char2.Equipment[0].ItemId);
        Assert.Equal(char1.Equipment[0].Quantity, char2.Equipment[0].Quantity);

        Assert.Equal(char1.Spells.Count, char2.Spells.Count);
        for (int i = 0; i < char1.Spells.Count; i++)
        {
            Assert.Equal(char1.Spells[i].SpellId, char2.Spells[i].SpellId);
            Assert.Equal(char1.Spells[i].ClassId, char2.Spells[i].ClassId);
            Assert.Equal(char1.Spells[i].Prepared, char2.Spells[i].Prepared);
        }
    }

    [Fact]
    public void Roundtrip_ImportExportImport_FeaturesArePreserved()
    {
        var exporter = CreateExporter(withFeats: true);
        var importer = CreateImporter(withFeats: true);

        var startXml = exporter.Export(new Character
        {
            Name = "Aric",
            TotalLevel = 1,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 1 }],
            AbilityScores = new AbilityScores { CON = new AbilityBlock { Base = 14 } },
            Features =
            [
                new CharacterFeature { FeatureId = "feat:darkvision", SourceId = "race:dwarf" },
                new CharacterFeature { FeatureId = "feat:military-rank", SourceId = "background:soldier" },
            ],
        });

        var char1 = importer.Import(startXml);
        var xml2 = exporter.Export(char1);
        var char2 = importer.Import(xml2);

        Assert.Equal(char1.Features.Count, char2.Features.Count);
        for (int i = 0; i < char1.Features.Count; i++)
        {
            Assert.Equal(char1.Features[i].FeatureId, char2.Features[i].FeatureId);
            Assert.Equal(char1.Features[i].SourceId, char2.Features[i].SourceId);
        }
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void Import_UnknownRaceName_LeavesRaceIdEmpty()
    {
        // Craft XML with a race name that isn't in the reference data.
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <pc version="5"><character>
              <name>Ghost</name>
              <player></player>
              <race><name>Tiefling</name><speed>30</speed></race>
              <class><name>Fighter</name><level>1</level><hd>10</hd></class>
              <background><name></name><align></align></background>
              <abilities>10,10,10,10,10,10,</abilities>
              <hpMax>10</hpMax><hpCurrent>10</hpCurrent><xp>0</xp>
            </character></pc>
            """;

        var result = CreateImporter().Import(xml);

        Assert.Equal(string.Empty, result.RaceId);
        Assert.Null(result.SubraceId);
    }

    [Fact]
    public void Import_UnknownClassName_StoresEmptyClassId()
    {
        // Craft XML with a class name that isn't in the reference data.
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <pc version="5"><character>
              <name>Ghost</name>
              <player></player>
              <race><name>Human</name><speed>30</speed></race>
              <class><name>Artificer</name><level>3</level><hd>8</hd></class>
              <background><name></name><align></align></background>
              <abilities>10,10,10,10,10,10,</abilities>
              <hpMax>27</hpMax><hpCurrent>27</hpCurrent><xp>0</xp>
            </character></pc>
            """;

        var result = CreateImporter().Import(xml);

        Assert.Single(result.Levels);
        Assert.Equal(string.Empty, result.Levels[0].ClassId);
        Assert.Equal(3, result.Levels[0].Level);
    }
}
