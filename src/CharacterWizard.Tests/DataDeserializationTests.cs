using System.Text.Json;
using CharacterWizard.Shared.Models;

namespace CharacterWizard.Tests;

/// <summary>
/// Tests that the JSON data files deserialize correctly into the model classes,
/// covering the behaviour that IDataService would provide at runtime.
/// </summary>
public class DataDeserializationTests
{
    private static readonly string DataDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data"));

    private static T DeserializeFile<T>(string fileName)
    {
        var path = Path.Combine(DataDir, fileName);
        var json = File.ReadAllText(path);
        var result = JsonSerializer.Deserialize<T>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = false });
        Assert.NotNull(result);
        return result!;
    }

    [Fact]
    public void Races_Json_DeserializesCorrectly()
    {
        var data = DeserializeFile<RacesData>("races.json");

        Assert.NotEmpty(data.Races);

        // All races must have an ID and DisplayName
        foreach (var race in data.Races)
        {
            Assert.False(string.IsNullOrWhiteSpace(race.Id), $"Race missing Id: {race.DisplayName}");
            Assert.False(string.IsNullOrWhiteSpace(race.DisplayName), $"Race missing DisplayName: {race.Id}");
        }

        // Human race should have +1 to all abilities
        var human = data.Races.FirstOrDefault(r => r.Id == "race:human");
        Assert.NotNull(human);
        Assert.Equal(6, human!.AbilityBonuses.Count);
        Assert.All(human.AbilityBonuses.Values, bonus => Assert.Equal(1, bonus));
    }

    [Fact]
    public void Races_Json_SubracesHaveDisplayName()
    {
        var data = DeserializeFile<RacesData>("races.json");

        foreach (var race in data.Races)
        {
            foreach (var sub in race.Subraces)
            {
                Assert.False(string.IsNullOrWhiteSpace(sub.Id),
                    $"Subrace of '{race.Id}' missing Id");
                Assert.False(string.IsNullOrWhiteSpace(sub.DisplayName),
                    $"Subrace '{sub.Id}' of '{race.Id}' missing DisplayName");
            }
        }
    }

    [Fact]
    public void Classes_Json_DeserializesCorrectly()
    {
        var data = DeserializeFile<ClassesData>("classes.json");

        Assert.NotEmpty(data.Classes);

        foreach (var cls in data.Classes)
        {
            Assert.False(string.IsNullOrWhiteSpace(cls.Id), $"Class missing Id: {cls.DisplayName}");
            Assert.False(string.IsNullOrWhiteSpace(cls.DisplayName), $"Class missing DisplayName: {cls.Id}");
            Assert.True(cls.HitDie > 0, $"Class '{cls.Id}' has no HitDie");
            Assert.NotEmpty(cls.SavingThrows);
            Assert.NotEmpty(cls.SkillChoices.Options);
            Assert.True(cls.SkillChoices.Count > 0, $"Class '{cls.Id}' has 0 skill choices");
        }

        // Fighter should have d10 hit die and STR or DEX saving throws
        var fighter = data.Classes.FirstOrDefault(c => c.Id == "class:fighter");
        Assert.NotNull(fighter);
        Assert.Equal(10, fighter!.HitDie);
        Assert.Contains("STR", fighter.SavingThrows);
        Assert.Equal(2, fighter.SkillChoices.Count);
    }

    [Fact]
    public void Backgrounds_Json_DeserializesCorrectly()
    {
        var data = DeserializeFile<BackgroundsData>("backgrounds.json");

        Assert.NotEmpty(data.Backgrounds);

        foreach (var bg in data.Backgrounds)
        {
            Assert.False(string.IsNullOrWhiteSpace(bg.Id), $"Background missing Id: {bg.DisplayName}");
            Assert.False(string.IsNullOrWhiteSpace(bg.DisplayName), $"Background missing DisplayName: {bg.Id}");
            Assert.NotEmpty(bg.SkillProficiencies);
            Assert.False(string.IsNullOrWhiteSpace(bg.FeatureId), $"Background '{bg.Id}' missing FeatureId");
        }

        // Soldier background (used in existing tests) must be present
        var soldier = data.Backgrounds.FirstOrDefault(b => b.Id == "background:soldier");
        Assert.NotNull(soldier);
        Assert.Contains("skill:athletics", soldier!.SkillProficiencies);
        Assert.Contains("skill:intimidation", soldier.SkillProficiencies);
    }

    [Fact]
    public void Spells_Json_DeserializesCorrectly()
    {
        var data = DeserializeFile<SpellsData>("spells.json");

        Assert.NotEmpty(data.Spells);

        foreach (var spell in data.Spells)
        {
            Assert.False(string.IsNullOrWhiteSpace(spell.Id), $"Spell missing Id: {spell.DisplayName}");
            Assert.False(string.IsNullOrWhiteSpace(spell.DisplayName), $"Spell missing DisplayName: {spell.Id}");
            Assert.True(spell.Level >= 0, $"Spell '{spell.Id}' has negative level");
            Assert.NotEmpty(spell.ClassIds);
        }

        // Fireball should be level 3 sorcerer/wizard spell
        var fireball = data.Spells.FirstOrDefault(s => s.Id == "spell:fireball");
        Assert.NotNull(fireball);
        Assert.Equal(3, fireball!.Level);
        Assert.Contains("class:wizard", fireball.ClassIds);
        Assert.Contains("class:sorcerer", fireball.ClassIds);
    }

    [Fact]
    public void Spells_Json_CantripHasLevelZero()
    {
        var data = DeserializeFile<SpellsData>("spells.json");
        var acidSplash = data.Spells.FirstOrDefault(s => s.Id == "spell:acid-splash");
        Assert.NotNull(acidSplash);
        Assert.Equal(0, acidSplash!.Level);
    }

    [Fact]
    public void Equipment_Json_DeserializesCorrectly()
    {
        var data = DeserializeFile<EquipmentData>("equipment.json");

        Assert.NotEmpty(data.Equipment);

        foreach (var item in data.Equipment)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Id), $"Item missing Id: {item.DisplayName}");
            Assert.False(string.IsNullOrWhiteSpace(item.DisplayName), $"Item missing DisplayName: {item.Id}");
            Assert.False(string.IsNullOrWhiteSpace(item.Category), $"Item '{item.Id}' missing Category");
        }

        // Longsword should be a martial-melee weapon
        var longsword = data.Equipment.FirstOrDefault(e => e.Id == "item:longsword");
        Assert.NotNull(longsword);
        Assert.Equal("weapon", longsword!.Category);
        Assert.Equal("martial-melee", longsword.Subcategory);
    }

    [Fact]
    public void Classes_Json_SpellcastingInfoDeserializes()
    {
        var data = DeserializeFile<ClassesData>("classes.json");

        var wizard = data.Classes.FirstOrDefault(c => c.Id == "class:wizard");
        Assert.NotNull(wizard);
        Assert.NotNull(wizard!.Spellcasting);
        Assert.Equal("full", wizard.Spellcasting!.CastingType);
        Assert.Equal("INT", wizard.Spellcasting.SpellcastingAbility);
        Assert.True(wizard.Spellcasting.PrepareSpells);
        Assert.Equal(20, wizard.Spellcasting.CantripsKnownByLevel.Count);

        var fighter = data.Classes.FirstOrDefault(c => c.Id == "class:fighter");
        Assert.NotNull(fighter);
        Assert.Null(fighter!.Spellcasting);
    }

    [Fact]
    public void Backgrounds_Json_StartingEquipmentDeserializes()
    {
        var data = DeserializeFile<BackgroundsData>("backgrounds.json");

        var acolyte = data.Backgrounds.FirstOrDefault(b => b.Id == "background:acolyte");
        Assert.NotNull(acolyte);
        Assert.NotEmpty(acolyte!.StartingEquipmentIds);
        Assert.Contains("item:holy-symbol", acolyte.StartingEquipmentIds);
    }

    [Fact]
    public void Names_Json_DeserializesCorrectly()
    {
        var data = DeserializeFile<NamesData>("names.json");

        Assert.NotEmpty(data.Full);
        Assert.NotEmpty(data.Given);
        Assert.NotEmpty(data.Surname);
    }

    [Fact]
    public void Names_Json_Has500EntriesEach()
    {
        var data = DeserializeFile<NamesData>("names.json");

        Assert.Equal(500, data.Full.Count);
        Assert.Equal(500, data.Given.Count);
        Assert.Equal(500, data.Surname.Count);
    }

    [Fact]
    public void Names_Json_AllEntriesNonEmpty()
    {
        var data = DeserializeFile<NamesData>("names.json");

        Assert.All(data.Full, name => Assert.False(string.IsNullOrWhiteSpace(name), $"Full name entry is empty or whitespace"));
        Assert.All(data.Given, name => Assert.False(string.IsNullOrWhiteSpace(name), $"Given name entry is empty or whitespace"));
        Assert.All(data.Surname, name => Assert.False(string.IsNullOrWhiteSpace(name), $"Surname entry is empty or whitespace"));
    }

    [Fact]
    public void ClassStartingEquipment_Json_DeserializesCorrectly()
    {
        var data = DeserializeFile<ClassStartingEquipmentData>("class-starting-equipment.json");

        Assert.NotEmpty(data.Entries);

        foreach (var entry in data.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.ClassId),
                $"Entry missing ClassId");
            Assert.False(string.IsNullOrWhiteSpace(entry.StartingWealthRoll),
                $"Entry '{entry.ClassId}' missing StartingWealthRoll");

            foreach (var group in entry.ChoiceGroups)
            {
                Assert.False(string.IsNullOrWhiteSpace(group.Id),
                    $"Choice group in '{entry.ClassId}' missing Id");
                Assert.NotEmpty(group.Options);

                foreach (var option in group.Options)
                {
                    Assert.False(string.IsNullOrWhiteSpace(option.Id),
                        $"Option in group '{group.Id}' of '{entry.ClassId}' missing Id");
                    Assert.NotEmpty(option.GrantItems);

                    foreach (var grant in option.GrantItems)
                    {
                        Assert.False(string.IsNullOrWhiteSpace(grant.ItemId),
                            $"Grant item in option '{option.Id}' of '{entry.ClassId}' missing ItemId");
                        Assert.True(grant.Quantity >= 1,
                            $"Grant item '{grant.ItemId}' in option '{option.Id}' has invalid quantity");
                    }
                }
            }
        }
    }

    [Fact]
    public void ClassStartingEquipment_Json_AllSrdClassesPresent()
    {
        var data = DeserializeFile<ClassStartingEquipmentData>("class-starting-equipment.json");
        var expectedClassIds = new[]
        {
            "class:barbarian", "class:bard", "class:cleric", "class:druid",
            "class:fighter", "class:monk", "class:paladin", "class:ranger",
            "class:rogue", "class:sorcerer", "class:warlock", "class:wizard",
        };
        var actualIds = data.Entries.Select(e => e.ClassId).ToHashSet();
        foreach (var expected in expectedClassIds)
            Assert.Contains(expected, actualIds);
    }

    [Fact]
    public void ClassStartingEquipment_Json_StartingWealthRollFormat()
    {
        var data = DeserializeFile<ClassStartingEquipmentData>("class-starting-equipment.json");
        // Validate that each roll expression matches pattern: {n}d{sides} or {n}d{sides}*{mult}
        var rollPattern = new System.Text.RegularExpressions.Regex(@"^\d+d\d+(\*\d+)?$");
        foreach (var entry in data.Entries)
            Assert.True(rollPattern.IsMatch(entry.StartingWealthRoll),
                $"'{entry.ClassId}' startingWealthRoll '{entry.StartingWealthRoll}' does not match expected format");
    }

    [Fact]
    public void ClassStartingEquipment_Json_FighterHasChoiceGroups()
    {
        var data = DeserializeFile<ClassStartingEquipmentData>("class-starting-equipment.json");
        var fighter = data.Entries.FirstOrDefault(e => e.ClassId == "class:fighter");
        Assert.NotNull(fighter);
        Assert.True(fighter!.ChoiceGroups.Count >= 2,
            "Fighter should have at least 2 choice groups");
        Assert.Equal("5d4*10", fighter.StartingWealthRoll);
    }

    [Fact]
    public void ClassStartingEquipment_Json_MonkHasNoMultiplierInWealthRoll()
    {
        var data = DeserializeFile<ClassStartingEquipmentData>("class-starting-equipment.json");
        var monk = data.Entries.FirstOrDefault(e => e.ClassId == "class:monk");
        Assert.NotNull(monk);
        // Monk starting wealth is 5d4 gp (no x10 multiplier)
        Assert.Equal("5d4", monk!.StartingWealthRoll);
    }

    [Fact]
    public void ClassStartingEquipment_Json_ItemIdsReferenceKnownEquipment()
    {
        var equipData = DeserializeFile<EquipmentData>("equipment.json");
        var validItemIds = equipData.Equipment.Select(e => e.Id).ToHashSet();

        var startEquipData = DeserializeFile<ClassStartingEquipmentData>("class-starting-equipment.json");
        foreach (var entry in startEquipData.Entries)
        {
            foreach (var fixedItem in entry.FixedItems)
                Assert.Contains(fixedItem.ItemId, validItemIds);

            foreach (var group in entry.ChoiceGroups)
                foreach (var option in group.Options)
                    foreach (var grant in option.GrantItems)
                        Assert.Contains(grant.ItemId, validItemIds);
        }
    }

    [Fact]
    public void Feats_Json_DeserializesCorrectly()
    {
        var data = DeserializeFile<FeatsData>("feats.json");

        Assert.NotEmpty(data.Feats);

        foreach (var feat in data.Feats)
        {
            Assert.False(string.IsNullOrWhiteSpace(feat.Id), $"Feat missing Id: {feat.DisplayName}");
            Assert.False(string.IsNullOrWhiteSpace(feat.DisplayName), $"Feat missing DisplayName: {feat.Id}");
            Assert.False(string.IsNullOrWhiteSpace(feat.Source), $"Feat '{feat.Id}' missing Source");
        }
    }

    [Fact]
    public void Feats_Json_AllIdsHaveFeatPrefix()
    {
        var data = DeserializeFile<FeatsData>("feats.json");

        foreach (var feat in data.Feats)
            Assert.True(feat.Id.StartsWith("feat:"), $"Feat Id '{feat.Id}' does not start with 'feat:'");
    }

    [Fact]
    public void Feats_Json_AllIdsAreUnique()
    {
        var data = DeserializeFile<FeatsData>("feats.json");

        var ids = data.Feats.Select(f => f.Id).ToList();
        var distinctIds = ids.Distinct().ToList();
        Assert.True(distinctIds.Count == ids.Count,
            $"Found {ids.Count - distinctIds.Count} duplicate feat ID(s)");
    }

    [Fact]
    public void Feats_Json_KnownFeatsPresent()
    {
        var data = DeserializeFile<FeatsData>("feats.json");
        var featIds = data.Feats.Select(f => f.Id).ToHashSet();

        // Class features
        Assert.Contains("feat:rage", featIds);
        Assert.Contains("feat:asi", featIds);
        Assert.Contains("feat:second-wind", featIds);
        Assert.Contains("feat:sneak-attack-1d6", featIds);

        // Background features
        Assert.Contains("feat:shelter-of-the-faithful", featIds);
        Assert.Contains("feat:military-rank", featIds);

        // General SRD feats
        Assert.Contains("feat:lucky", featIds);
        Assert.Contains("feat:alert", featIds);
        Assert.Contains("feat:tough", featIds);
    }

    [Fact]
    public void Abilities_Json_DeserializesCorrectly()
    {
        var data = DeserializeFile<AbilitiesConfig>("abilities.json");

        Assert.False(string.IsNullOrWhiteSpace(data.SchemaVersion), "SchemaVersion should not be empty");
        Assert.False(string.IsNullOrWhiteSpace(data.Source), "Source should not be empty");
    }

    [Fact]
    public void Abilities_Json_StandardArrayIsCorrect()
    {
        var data = DeserializeFile<AbilitiesConfig>("abilities.json");

        Assert.Equal(6, data.StandardArray.Count);
        Assert.Equal([15, 14, 13, 12, 10, 8], data.StandardArray);
    }

    [Fact]
    public void Abilities_Json_PointBuyConfigIsCorrect()
    {
        var data = DeserializeFile<AbilitiesConfig>("abilities.json");

        Assert.Equal(27, data.PointBuy.Budget);
        Assert.Equal(8, data.PointBuy.MinScore);
        Assert.Equal(15, data.PointBuy.MaxScore);
        Assert.NotEmpty(data.PointBuy.Costs);

        // Score 8 costs 0, score 15 costs 9
        var cost8 = data.PointBuy.Costs.FirstOrDefault(c => c.Score == 8);
        Assert.NotNull(cost8);
        Assert.Equal(0, cost8!.Cost);

        var cost15 = data.PointBuy.Costs.FirstOrDefault(c => c.Score == 15);
        Assert.NotNull(cost15);
        Assert.Equal(9, cost15!.Cost);
    }

    [Fact]
    public void Abilities_Json_RollConfigIsCorrect()
    {
        var data = DeserializeFile<AbilitiesConfig>("abilities.json");

        Assert.False(string.IsNullOrWhiteSpace(data.Roll.Method), "Roll method should not be empty");
        Assert.Equal("4d6-drop-lowest", data.Roll.Method);
        Assert.Equal(6, data.Roll.Count);
    }
}
