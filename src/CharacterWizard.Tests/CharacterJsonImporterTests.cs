using System.Text.Json;
using CharacterWizard.Shared.Export;
using CharacterWizard.Shared.Models;

namespace CharacterWizard.Tests;

public class CharacterJsonImporterTests
{
    // ── Serialization options matching the wizard's ExportJson ───────────────
    private static readonly JsonSerializerOptions ExportOptions = new() { WriteIndented = true };

    private static string Serialize(Character c) => JsonSerializer.Serialize(c, ExportOptions);

    private static readonly CharacterJsonImporter Importer = new();

    // ── Basic field restoration ───────────────────────────────────────────────

    [Fact]
    public void Import_BasicCharacter_NameAndPlayerAreRestored()
    {
        var original = new Character { Name = "Aric", PlayerName = "Dave" };
        var json = Serialize(original);
        var result = Importer.Import(json);
        Assert.NotNull(result);
        Assert.Equal("Aric", result.Name);
        Assert.Equal("Dave", result.PlayerName);
    }

    [Fact]
    public void Import_NullPlayerName_IsPreservedAsNull()
    {
        var original = new Character { Name = "Aric", PlayerName = null };
        var json = Serialize(original);
        var result = Importer.Import(json);
        Assert.NotNull(result);
        Assert.Null(result.PlayerName);
    }

    [Fact]
    public void Import_AllTopLevelFields_AreRestored()
    {
        var original = new Character
        {
            Id = "abc-123",
            Name = "Thorin",
            PlayerName = "Sarah",
            Campaign = "Curse of Strahd",
            TotalLevel = 5,
            RaceId = "race:dwarf",
            SubraceId = "subrace:hill-dwarf",
            BackgroundId = "background:soldier",
            GenerationMethod = GenerationMethod.PointBuy,
        };

        var result = Importer.Import(Serialize(original));

        Assert.NotNull(result);
        Assert.Equal("abc-123", result.Id);
        Assert.Equal("Thorin", result.Name);
        Assert.Equal("Sarah", result.PlayerName);
        Assert.Equal("Curse of Strahd", result.Campaign);
        Assert.Equal(5, result.TotalLevel);
        Assert.Equal("race:dwarf", result.RaceId);
        Assert.Equal("subrace:hill-dwarf", result.SubraceId);
        Assert.Equal("background:soldier", result.BackgroundId);
        Assert.Equal(GenerationMethod.PointBuy, result.GenerationMethod);
    }

    [Fact]
    public void Import_AbilityScores_BaseRacialAndOtherBonusAreRestored()
    {
        var original = new Character
        {
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15, RacialBonus = 1, OtherBonus = 2 },
                DEX = new AbilityBlock { Base = 14 },
                CON = new AbilityBlock { Base = 13, RacialBonus = 2 },
                INT = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10, RacialBonus = 1 },
                CHA = new AbilityBlock { Base = 8 },
            },
        };

        var result = Importer.Import(Serialize(original));

        Assert.NotNull(result);
        Assert.Equal(15, result.AbilityScores.STR.Base);
        Assert.Equal(1, result.AbilityScores.STR.RacialBonus);
        Assert.Equal(2, result.AbilityScores.STR.OtherBonus);
        Assert.Equal(18, result.AbilityScores.STR.Final);   // 15+1+2

        Assert.Equal(2, result.AbilityScores.CON.RacialBonus);
        Assert.Equal(15, result.AbilityScores.CON.Final);   // 13+2
    }

    [Fact]
    public void Import_Levels_AreRestored()
    {
        var original = new Character
        {
            TotalLevel = 5,
            Levels =
            [
                new ClassLevel { ClassId = "class:fighter", Level = 3, SubclassId = "subclass:champion" },
                new ClassLevel { ClassId = "class:wizard", Level = 2 },
            ],
        };

        var result = Importer.Import(Serialize(original));

        Assert.NotNull(result);
        Assert.Equal(2, result.Levels.Count);
        Assert.Equal("class:fighter", result.Levels[0].ClassId);
        Assert.Equal(3, result.Levels[0].Level);
        Assert.Equal("subclass:champion", result.Levels[0].SubclassId);
        Assert.Equal("class:wizard", result.Levels[1].ClassId);
        Assert.Equal(2, result.Levels[1].Level);
        Assert.Null(result.Levels[1].SubclassId);
    }

    [Fact]
    public void Import_Skills_AreRestored()
    {
        var original = new Character
        {
            Skills = new Dictionary<string, string>
            {
                ["skill:athletics"] = "class",
                ["skill:intimidation"] = "background",
            },
        };

        var result = Importer.Import(Serialize(original));

        Assert.NotNull(result);
        Assert.Equal(2, result.Skills.Count);
        Assert.Equal("class", result.Skills["skill:athletics"]);
        Assert.Equal("background", result.Skills["skill:intimidation"]);
    }

    [Fact]
    public void Import_Proficiencies_AreRestored()
    {
        var original = new Character
        {
            Proficiencies = new CharacterProficiencies
            {
                Armor = ["light", "medium", "heavy", "shields"],
                Weapons = ["simple", "martial"],
                Tools = ["playing card set"],
                Languages = ["common", "dwarvish"],
            },
        };

        var result = Importer.Import(Serialize(original));

        Assert.NotNull(result);
        Assert.Equal(["light", "medium", "heavy", "shields"], result.Proficiencies.Armor);
        Assert.Equal(["simple", "martial"], result.Proficiencies.Weapons);
        Assert.Equal(["playing card set"], result.Proficiencies.Tools);
        Assert.Equal(["common", "dwarvish"], result.Proficiencies.Languages);
    }

    [Fact]
    public void Import_Features_AreRestored()
    {
        var original = new Character
        {
            Features =
            [
                new CharacterFeature { FeatureId = "feat:darkvision", SourceId = "race:dwarf" },
                new CharacterFeature { FeatureId = "feat:military-rank", SourceId = "background:soldier", DisplayOverride = "Military Rank" },
            ],
        };

        var result = Importer.Import(Serialize(original));

        Assert.NotNull(result);
        Assert.Equal(2, result.Features.Count);
        Assert.Equal("feat:darkvision", result.Features[0].FeatureId);
        Assert.Equal("race:dwarf", result.Features[0].SourceId);
        Assert.Equal("feat:military-rank", result.Features[1].FeatureId);
        Assert.Equal("Military Rank", result.Features[1].DisplayOverride);
    }

    [Fact]
    public void Import_Spells_AreRestored()
    {
        var original = new Character
        {
            Spells =
            [
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "class:wizard", Prepared = false },
                new CharacterSpell { SpellId = "spell:magic-missile", ClassId = "class:wizard", Prepared = true },
            ],
        };

        var result = Importer.Import(Serialize(original));

        Assert.NotNull(result);
        Assert.Equal(2, result.Spells.Count);
        Assert.Equal("spell:fire-bolt", result.Spells[0].SpellId);
        Assert.False(result.Spells[0].Prepared);
        Assert.True(result.Spells[1].Prepared);
    }

    [Fact]
    public void Import_Equipment_AreRestored()
    {
        var original = new Character
        {
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 },
                new CharacterEquipmentItem { ItemId = "item:chain-mail", Quantity = 3 },
            ],
        };

        var result = Importer.Import(Serialize(original));

        Assert.NotNull(result);
        Assert.Equal(2, result.Equipment.Count);
        Assert.Equal("item:longsword", result.Equipment[0].ItemId);
        Assert.Equal(1, result.Equipment[0].Quantity);
        Assert.Equal(3, result.Equipment[1].Quantity);
    }

    [Fact]
    public void Import_AsiChoices_AreRestored()
    {
        var original = new Character
        {
            AsiChoices =
            [
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 4, Mode = "plus2", AbilityOne = "STR" },
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 8, Mode = "feat", FeatId = "feat:great-weapon-master" },
            ],
        };

        var result = Importer.Import(Serialize(original));

        Assert.NotNull(result);
        Assert.Equal(2, result.AsiChoices.Count);
        Assert.Equal("class:fighter", result.AsiChoices[0].ClassId);
        Assert.Equal(4, result.AsiChoices[0].ClassLevel);
        Assert.Equal("plus2", result.AsiChoices[0].Mode);
        Assert.Equal("STR", result.AsiChoices[0].AbilityOne);
        Assert.Equal("feat:great-weapon-master", result.AsiChoices[1].FeatId);
    }

    [Fact]
    public void Import_StartingEquipmentChoices_AreRestored()
    {
        var original = new Character
        {
            ClassStartingWealthChosen = true,
            ClassStartingGold = 150,
        };

        var result = Importer.Import(Serialize(original));

        Assert.NotNull(result);
        Assert.True(result.ClassStartingWealthChosen);
        Assert.Equal(150, result.ClassStartingGold);
    }

    // ── Error handling ────────────────────────────────────────────────────────

    [Fact]
    public void Import_EmptyString_ReturnsNull()
    {
        Assert.Null(Importer.Import(string.Empty));
    }

    [Fact]
    public void Import_WhitespaceOnly_ReturnsNull()
    {
        Assert.Null(Importer.Import("   "));
    }

    [Fact]
    public void Import_InvalidJson_ReturnsNull()
    {
        Assert.Null(Importer.Import("this is not json"));
    }

    [Fact]
    public void Import_XmlString_ReturnsNull()
    {
        // FC5e XML is not valid JSON — should return null gracefully.
        Assert.Null(Importer.Import("<?xml version=\"1.0\"?><pc version=\"5\"><character></character></pc>"));
    }

    [Fact]
    public void Import_JsonArray_ReturnsNull()
    {
        // The exporter produces a JSON object, not an array.
        Assert.Null(Importer.Import("[\"not\",\"a\",\"character\"]"));
    }

    // ── Case insensitivity ────────────────────────────────────────────────────

    [Fact]
    public void Import_CamelCaseJson_IsAccepted()
    {
        // Verify that camelCase JSON (from different serialization settings) is accepted.
        const string camelJson = """{"name":"Aric","playerName":"Dave","totalLevel":1}""";
        var result = Importer.Import(camelJson);
        Assert.NotNull(result);
        Assert.Equal("Aric", result.Name);
        Assert.Equal("Dave", result.PlayerName);
        Assert.Equal(1, result.TotalLevel);
    }

    // ── Export → Import → Export roundtrip ───────────────────────────────────
    //
    // Serializing a Character, deserializing it, then serializing again must produce
    // byte-for-byte identical JSON to the original.

    [Fact]
    public void Roundtrip_ExportImportExport_SimpleCharacter_JsonIsIdentical()
    {
        var original = new Character
        {
            Id = "abc-123",
            Name = "Aric",
            PlayerName = "Dave",
            Campaign = "Curse of Strahd",
            TotalLevel = 1,
            RaceId = "race:human",
            BackgroundId = "background:soldier",
            GenerationMethod = GenerationMethod.StandardArray,
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
                ["skill:athletics"] = "class",
                ["skill:intimidation"] = "background",
            },
            Proficiencies = new CharacterProficiencies
            {
                Armor = ["light", "medium", "heavy", "shields"],
                Weapons = ["simple", "martial"],
            },
        };

        var json1 = Serialize(original);
        var json2 = Serialize(Importer.Import(json1)!);

        Assert.Equal(json1, json2);
    }

    [Fact]
    public void Roundtrip_ExportImportExport_FullCharacterWithSpellsAndEquipment_JsonIsIdentical()
    {
        var original = new Character
        {
            Id = "full-123",
            Name = "Myra",
            TotalLevel = 5,
            RaceId = "race:human",
            BackgroundId = "background:soldier",
            GenerationMethod = GenerationMethod.PointBuy,
            Levels =
            [
                new ClassLevel { ClassId = "class:fighter", Level = 3, SubclassId = "subclass:champion" },
                new ClassLevel { ClassId = "class:wizard", Level = 2 },
            ],
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 14, RacialBonus = 1, OtherBonus = 2 },
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
            Features =
            [
                new CharacterFeature { FeatureId = "feat:darkvision", SourceId = "race:human" },
                new CharacterFeature { FeatureId = "feat:military-rank", SourceId = "background:soldier" },
            ],
            Spells =
            [
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "class:wizard", Prepared = false },
                new CharacterSpell { SpellId = "spell:magic-missile", ClassId = "class:wizard", Prepared = true },
            ],
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 },
                new CharacterEquipmentItem { ItemId = "item:chain-mail", Quantity = 1 },
            ],
            AsiChoices =
            [
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 4, Mode = "plus2", AbilityOne = "STR" },
            ],
            ClassStartingWealthChosen = false,
        };

        var json1 = Serialize(original);
        var json2 = Serialize(Importer.Import(json1)!);

        Assert.Equal(json1, json2);
    }

    [Fact]
    public void Roundtrip_ExportImportExport_EmptyCharacter_JsonIsIdentical()
    {
        var original = new Character();
        var json1 = Serialize(original);
        var json2 = Serialize(Importer.Import(json1)!);
        Assert.Equal(json1, json2);
    }

    // ── Import → Export → Import roundtrip ───────────────────────────────────
    //
    // Deserializing JSON, serializing the result, then deserializing again must
    // produce a character with identical observable state to the first import.

    [Fact]
    public void Roundtrip_ImportExportImport_AbilityScoresArePreserved()
    {
        var startJson = Serialize(new Character
        {
            Name = "Aric",
            AbilityScores = new AbilityScores
            {
                STR = new AbilityBlock { Base = 15, RacialBonus = 1, OtherBonus = 2 },
                DEX = new AbilityBlock { Base = 14 },
                CON = new AbilityBlock { Base = 13, RacialBonus = 2 },
                INT = new AbilityBlock { Base = 12 },
                WIS = new AbilityBlock { Base = 10, RacialBonus = 1 },
                CHA = new AbilityBlock { Base = 8 },
            },
        });

        var char1 = Importer.Import(startJson)!;
        var char2 = Importer.Import(Serialize(char1))!;

        Assert.Equal(char1.AbilityScores.STR.Base, char2.AbilityScores.STR.Base);
        Assert.Equal(char1.AbilityScores.STR.RacialBonus, char2.AbilityScores.STR.RacialBonus);
        Assert.Equal(char1.AbilityScores.STR.OtherBonus, char2.AbilityScores.STR.OtherBonus);
        Assert.Equal(char1.AbilityScores.STR.Final, char2.AbilityScores.STR.Final);

        Assert.Equal(char1.AbilityScores.CON.RacialBonus, char2.AbilityScores.CON.RacialBonus);
        Assert.Equal(char1.AbilityScores.CON.Final, char2.AbilityScores.CON.Final);
    }

    [Fact]
    public void Roundtrip_ImportExportImport_ClassRaceBackgroundArePreserved()
    {
        var startJson = Serialize(new Character
        {
            Id = "id-1",
            Name = "Thorin",
            PlayerName = "Sarah",
            Campaign = "Lost Mines",
            TotalLevel = 3,
            RaceId = "race:dwarf",
            SubraceId = "subrace:hill-dwarf",
            BackgroundId = "background:soldier",
            GenerationMethod = GenerationMethod.Roll,
            Levels =
            [
                new ClassLevel { ClassId = "class:fighter", Level = 3, SubclassId = "subclass:champion" },
            ],
        });

        var char1 = Importer.Import(startJson)!;
        var char2 = Importer.Import(Serialize(char1))!;

        Assert.Equal(char1.Id, char2.Id);
        Assert.Equal(char1.Name, char2.Name);
        Assert.Equal(char1.PlayerName, char2.PlayerName);
        Assert.Equal(char1.Campaign, char2.Campaign);
        Assert.Equal(char1.TotalLevel, char2.TotalLevel);
        Assert.Equal(char1.RaceId, char2.RaceId);
        Assert.Equal(char1.SubraceId, char2.SubraceId);
        Assert.Equal(char1.BackgroundId, char2.BackgroundId);
        Assert.Equal(char1.GenerationMethod, char2.GenerationMethod);
        Assert.Equal(char1.Levels[0].ClassId, char2.Levels[0].ClassId);
        Assert.Equal(char1.Levels[0].Level, char2.Levels[0].Level);
        Assert.Equal(char1.Levels[0].SubclassId, char2.Levels[0].SubclassId);
    }

    [Fact]
    public void Roundtrip_ImportExportImport_SkillsAndProficienciesArePreserved()
    {
        var startJson = Serialize(new Character
        {
            Skills = new Dictionary<string, string>
            {
                ["skill:athletics"] = "class",
                ["skill:intimidation"] = "background",
                ["skill:arcana"] = "class",
            },
            Proficiencies = new CharacterProficiencies
            {
                Armor = ["light", "medium", "heavy"],
                Weapons = ["simple", "martial"],
                Tools = ["herbalism kit"],
                Languages = ["common", "elvish"],
            },
        });

        var char1 = Importer.Import(startJson)!;
        var char2 = Importer.Import(Serialize(char1))!;

        Assert.Equal(char1.Skills, char2.Skills);
        Assert.Equal(char1.Proficiencies.Armor, char2.Proficiencies.Armor);
        Assert.Equal(char1.Proficiencies.Weapons, char2.Proficiencies.Weapons);
        Assert.Equal(char1.Proficiencies.Tools, char2.Proficiencies.Tools);
        Assert.Equal(char1.Proficiencies.Languages, char2.Proficiencies.Languages);
    }

    [Fact]
    public void Roundtrip_ImportExportImport_SpellsAndEquipmentArePreserved()
    {
        var startJson = Serialize(new Character
        {
            Spells =
            [
                new CharacterSpell { SpellId = "spell:fire-bolt", ClassId = "class:wizard", Prepared = false },
                new CharacterSpell { SpellId = "spell:magic-missile", ClassId = "class:wizard", Prepared = true },
            ],
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 2 },
                new CharacterEquipmentItem { ItemId = "item:chain-mail", Quantity = 1 },
            ],
        });

        var char1 = Importer.Import(startJson)!;
        var char2 = Importer.Import(Serialize(char1))!;

        Assert.Equal(char1.Spells.Count, char2.Spells.Count);
        for (int i = 0; i < char1.Spells.Count; i++)
        {
            Assert.Equal(char1.Spells[i].SpellId, char2.Spells[i].SpellId);
            Assert.Equal(char1.Spells[i].ClassId, char2.Spells[i].ClassId);
            Assert.Equal(char1.Spells[i].Prepared, char2.Spells[i].Prepared);
        }

        Assert.Equal(char1.Equipment.Count, char2.Equipment.Count);
        for (int i = 0; i < char1.Equipment.Count; i++)
        {
            Assert.Equal(char1.Equipment[i].ItemId, char2.Equipment[i].ItemId);
            Assert.Equal(char1.Equipment[i].Quantity, char2.Equipment[i].Quantity);
        }
    }

    [Fact]
    public void Roundtrip_ImportExportImport_FeaturesAndAsiChoicesArePreserved()
    {
        var startJson = Serialize(new Character
        {
            Features =
            [
                new CharacterFeature { FeatureId = "feat:darkvision", SourceId = "race:dwarf", DisplayOverride = "Darkvision" },
                new CharacterFeature { FeatureId = "feat:military-rank", SourceId = "background:soldier" },
            ],
            AsiChoices =
            [
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 4, Mode = "split", AbilityOne = "STR", AbilityTwo = "CON" },
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 8, Mode = "feat", FeatId = "feat:alert" },
            ],
        });

        var char1 = Importer.Import(startJson)!;
        var char2 = Importer.Import(Serialize(char1))!;

        Assert.Equal(char1.Features.Count, char2.Features.Count);
        for (int i = 0; i < char1.Features.Count; i++)
        {
            Assert.Equal(char1.Features[i].FeatureId, char2.Features[i].FeatureId);
            Assert.Equal(char1.Features[i].SourceId, char2.Features[i].SourceId);
            Assert.Equal(char1.Features[i].DisplayOverride, char2.Features[i].DisplayOverride);
        }

        Assert.Equal(char1.AsiChoices.Count, char2.AsiChoices.Count);
        for (int i = 0; i < char1.AsiChoices.Count; i++)
        {
            Assert.Equal(char1.AsiChoices[i].ClassId, char2.AsiChoices[i].ClassId);
            Assert.Equal(char1.AsiChoices[i].ClassLevel, char2.AsiChoices[i].ClassLevel);
            Assert.Equal(char1.AsiChoices[i].Mode, char2.AsiChoices[i].Mode);
            Assert.Equal(char1.AsiChoices[i].AbilityOne, char2.AsiChoices[i].AbilityOne);
            Assert.Equal(char1.AsiChoices[i].AbilityTwo, char2.AsiChoices[i].AbilityTwo);
            Assert.Equal(char1.AsiChoices[i].FeatId, char2.AsiChoices[i].FeatId);
        }
    }
}
