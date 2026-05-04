using System.Text.Json;
using CharacterWizard.Shared.Models;

namespace CharacterWizard.Tests;

/// <summary>
/// Tests that IDs referenced across JSON data files resolve to valid entries in their target data sets.
/// These tests catch broken cross-file references introduced by new or modified data.
/// </summary>
public class DataIntegrityTests
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

    // ---------------------------------------------------------------------------
    // Spell cross-file references
    // ---------------------------------------------------------------------------

    [Fact]
    public void Spells_Json_ClassIdsReferenceKnownClasses()
    {
        var classData = DeserializeFile<ClassesData>("classes.json");
        var validClassIds = classData.Classes.Select(c => c.Id).ToHashSet();

        var spellData = DeserializeFile<SpellsData>("spells.json");

        foreach (var spell in spellData.Spells)
        {
            foreach (var classId in spell.ClassIds)
            {
                Assert.True(validClassIds.Contains(classId),
                    $"Spell '{spell.Id}' references unknown classId '{classId}'");
            }
        }
    }

    // ---------------------------------------------------------------------------
    // Equipment cross-file references
    // ---------------------------------------------------------------------------

    [Fact]
    public void Classes_Json_StartingEquipmentIdsReferenceKnownEquipment()
    {
        var equipData = DeserializeFile<EquipmentData>("equipment.json");
        var validItemIds = equipData.Equipment.Select(e => e.Id).ToHashSet();

        var classData = DeserializeFile<ClassesData>("classes.json");

        foreach (var cls in classData.Classes)
        {
            foreach (var itemId in cls.StartingEquipmentIds)
            {
                Assert.True(validItemIds.Contains(itemId),
                    $"Class '{cls.Id}' startingEquipmentIds references unknown item '{itemId}'");
            }
        }
    }

    [Fact]
    public void Backgrounds_Json_StartingEquipmentIdsReferenceKnownEquipment()
    {
        var equipData = DeserializeFile<EquipmentData>("equipment.json");
        var validItemIds = equipData.Equipment.Select(e => e.Id).ToHashSet();

        var bgData = DeserializeFile<BackgroundsData>("backgrounds.json");

        foreach (var bg in bgData.Backgrounds)
        {
            foreach (var itemId in bg.StartingEquipmentIds)
            {
                Assert.True(validItemIds.Contains(itemId),
                    $"Background '{bg.Id}' startingEquipmentIds references unknown item '{itemId}'");
            }
        }
    }

    [Fact]
    public void Classes_Json_StartingEquipmentItemIdsReferenceKnownEquipment()
    {
        var equipData = DeserializeFile<EquipmentData>("equipment.json");
        var validItemIds = equipData.Equipment.Select(e => e.Id).ToHashSet();

        var classData = DeserializeFile<ClassesData>("classes.json");

        foreach (var cls in classData.Classes)
        {
            var entry = cls.StartingEquipment;
            if (entry == null) continue;

            foreach (var fixedItem in entry.FixedItems)
                Assert.True(validItemIds.Contains(fixedItem.ItemId),
                    $"Class '{cls.Id}' startingEquipment fixedItem references unknown item '{fixedItem.ItemId}'");

            foreach (var group in entry.ChoiceGroups)
                foreach (var option in group.Options)
                    foreach (var grant in option.GrantItems)
                        Assert.True(validItemIds.Contains(grant.ItemId),
                            $"Class '{cls.Id}' startingEquipment choice option references unknown item '{grant.ItemId}'");
        }
    }

    // ---------------------------------------------------------------------------
    // Feat cross-file references (existing)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Backgrounds_Json_FeatureIdReferencesKnownFeat()
    {
        var featData = DeserializeFile<FeatsData>("feats.json");
        var validFeatIds = featData.Feats.Select(f => f.Id).ToHashSet();

        var bgData = DeserializeFile<BackgroundsData>("backgrounds.json");

        foreach (var bg in bgData.Backgrounds)
        {
            Assert.True(validFeatIds.Contains(bg.FeatureId),
                $"Background '{bg.Id}' featureId '{bg.FeatureId}' is not in feats.json");
        }
    }

    [Fact]
    public void Classes_Json_FeaturesByLevelFeatIdsReferenceKnownFeats()
    {
        var featData = DeserializeFile<FeatsData>("feats.json");
        var validFeatIds = featData.Feats.Select(f => f.Id).ToHashSet();

        var classData = DeserializeFile<ClassesData>("classes.json");

        foreach (var cls in classData.Classes)
        {
            foreach (var (level, featIds) in cls.FeaturesByLevel)
            {
                foreach (var featId in featIds)
                {
                    Assert.True(validFeatIds.Contains(featId),
                        $"Class '{cls.Id}' level {level} references unknown feat '{featId}'");
                }
            }
        }
    }

    // ---------------------------------------------------------------------------
    // FR3.6 — Semantic feat integrity rules
    // ---------------------------------------------------------------------------

    /// <summary>FR3.6: Every BackgroundDefinition.featureId must resolve to a feat with type = "background".</summary>
    [Fact]
    public void Backgrounds_Json_FeatureIdReferencesBackgroundTypeFeat()
    {
        var featData = DeserializeFile<FeatsData>("feats.json");
        var backgroundFeatIds = featData.Feats
            .Where(f => f.Type == "background")
            .Select(f => f.Id)
            .ToHashSet();

        var bgData = DeserializeFile<BackgroundsData>("backgrounds.json");

        var failures = new List<string>();
        foreach (var bg in bgData.Backgrounds)
        {
            if (!string.IsNullOrEmpty(bg.FeatureId) && !backgroundFeatIds.Contains(bg.FeatureId))
                failures.Add($"Background '{bg.Id}' featureId '{bg.FeatureId}' exists in feats.json but has type != \"background\"");
        }

        Assert.True(failures.Count == 0,
            $"Backgrounds reference feats that are not of type \"background\":\n{string.Join("\n", failures)}");
    }

    /// <summary>FR3.6: Every feat with type = "class" must appear in at least one class's featuresByLevel.</summary>
    [Fact]
    public void Feats_ClassType_AppearsInAtLeastOneClassFeaturesByLevel()
    {
        var classData = DeserializeFile<ClassesData>("classes.json");
        var classFeatIdSet = classData.Classes
            .SelectMany(c => c.FeaturesByLevel.Values.SelectMany(ids => ids))
            .ToHashSet();

        var featData = DeserializeFile<FeatsData>("feats.json");

        var failures = new List<string>();
        foreach (var feat in featData.Feats.Where(f => f.Type == "class"))
        {
            if (!classFeatIdSet.Contains(feat.Id))
                failures.Add($"Feat '{feat.Id}' (type=\"class\") does not appear in any class's featuresByLevel");
        }

        Assert.True(failures.Count == 0,
            $"Class-type feats are unreachable from any class's featuresByLevel:\n{string.Join("\n", failures)}");
    }

    /// <summary>
    /// FR3.6: Every feat with type = "general" must not appear in any class's featuresByLevel
    /// or as a background featureId.
    /// </summary>
    [Fact]
    public void Feats_GeneralType_NotInClassFeaturesByLevelOrBackgroundFeatureId()
    {
        var classData = DeserializeFile<ClassesData>("classes.json");
        var classFeatIdSet = classData.Classes
            .SelectMany(c => c.FeaturesByLevel.Values.SelectMany(ids => ids))
            .ToHashSet();

        var bgData = DeserializeFile<BackgroundsData>("backgrounds.json");
        var backgroundFeatureIdSet = bgData.Backgrounds
            .Where(b => !string.IsNullOrEmpty(b.FeatureId))
            .Select(b => b.FeatureId)
            .ToHashSet();

        var featData = DeserializeFile<FeatsData>("feats.json");

        var failures = new List<string>();
        foreach (var feat in featData.Feats.Where(f => f.Type == "general"))
        {
            if (classFeatIdSet.Contains(feat.Id))
                failures.Add($"Feat '{feat.Id}' (type=\"general\") appears in a class's featuresByLevel");

            if (backgroundFeatureIdSet.Contains(feat.Id))
                failures.Add($"Feat '{feat.Id}' (type=\"general\") is used as a background featureId");
        }

        Assert.True(failures.Count == 0,
            $"General-type feats appear where they should not:\n{string.Join("\n", failures)}");
    }

    // ---------------------------------------------------------------------------
    // ID uniqueness within collections
    // ---------------------------------------------------------------------------

    [Fact]
    public void Classes_Json_IdsAreUnique()
    {
        var classData = DeserializeFile<ClassesData>("classes.json");
        var ids = classData.Classes.Select(c => c.Id).ToList();
        var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate class IDs found: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void Classes_Json_SubclassIdsAreUniqueAcrossAllClasses()
    {
        var classData = DeserializeFile<ClassesData>("classes.json");
        var allSubclassIds = classData.Classes
            .SelectMany(c => c.SubclassOptions.Select(s => (ClassId: c.Id, SubclassId: s.Id)))
            .ToList();

        var duplicates = allSubclassIds
            .GroupBy(x => x.SubclassId)
            .Where(g => g.Count() > 1)
            .Select(g => $"'{g.Key}' appears in: {string.Join(", ", g.Select(x => x.ClassId))}")
            .ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate subclass IDs found across all classes:\n{string.Join("\n", duplicates)}");
    }

    [Fact]
    public void Races_Json_IdsAreUnique()
    {
        var raceData = DeserializeFile<RacesData>("races.json");
        var ids = raceData.Races.Select(r => r.Id).ToList();
        var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate race IDs found: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void Races_Json_SubraceIdsAreUniqueAcrossAllRaces()
    {
        var raceData = DeserializeFile<RacesData>("races.json");
        var allSubraceIds = raceData.Races
            .SelectMany(r => r.Subraces.Select(s => (RaceId: r.Id, SubraceId: s.Id)))
            .ToList();

        var duplicates = allSubraceIds
            .GroupBy(x => x.SubraceId)
            .Where(g => g.Count() > 1)
            .Select(g => $"'{g.Key}' appears in: {string.Join(", ", g.Select(x => x.RaceId))}")
            .ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate subrace IDs found across all races:\n{string.Join("\n", duplicates)}");
    }

    [Fact]
    public void Backgrounds_Json_IdsAreUnique()
    {
        var bgData = DeserializeFile<BackgroundsData>("backgrounds.json");
        var ids = bgData.Backgrounds.Select(b => b.Id).ToList();
        var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate background IDs found: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void Spells_Json_IdsAreUnique()
    {
        var spellData = DeserializeFile<SpellsData>("spells.json");
        var ids = spellData.Spells.Select(s => s.Id).ToList();
        var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate spell IDs found: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void Equipment_Json_IdsAreUnique()
    {
        var equipData = DeserializeFile<EquipmentData>("equipment.json");
        var ids = equipData.Equipment.Select(e => e.Id).ToList();
        var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate equipment IDs found: {string.Join(", ", duplicates)}");
    }

    // ---------------------------------------------------------------------------
    // Subclass bonus spell cross-file references
    // ---------------------------------------------------------------------------

    [Fact]
    public void Classes_Json_SubclassBonusSpellIdsReferenceKnownSpells()
    {
        var spellData = DeserializeFile<SpellsData>("spells.json");
        var validSpellIds = spellData.Spells.Select(s => s.Id).ToHashSet();

        var classData = DeserializeFile<ClassesData>("classes.json");

        var failures = new List<string>();
        foreach (var cls in classData.Classes)
        {
            foreach (var subclass in cls.SubclassOptions)
            {
                if (subclass.BonusSpells == null) continue;

                foreach (var bonusSpell in subclass.BonusSpells)
                {
                    if (!validSpellIds.Contains(bonusSpell.SpellId))
                        failures.Add(
                            $"Subclass '{subclass.Id}' (class '{cls.Id}') bonusSpells references unknown spell '{bonusSpell.SpellId}' (grantLevel {bonusSpell.GrantLevel})");
                }
            }
        }

        Assert.True(failures.Count == 0,
            $"Subclass bonusSpells reference unknown spell IDs:\n{string.Join("\n", failures)}");
    }

    // ---------------------------------------------------------------------------
    // Race/subrace language cross-file references
    // ---------------------------------------------------------------------------

    [Fact]
    public void Races_Json_LanguageIdsReferenceKnownLanguages()
    {
        var langData = DeserializeFile<LanguagesData>("languages.json");
        var validLangIds = langData.Languages.Select(l => l.Id).ToHashSet();

        var raceData = DeserializeFile<RacesData>("races.json");

        var failures = new List<string>();
        foreach (var race in raceData.Races)
        {
            foreach (var langId in race.LanguageIds)
            {
                if (!validLangIds.Contains(langId))
                    failures.Add($"Race '{race.Id}' languageIds references unknown language '{langId}'");
            }
        }

        Assert.True(failures.Count == 0,
            $"Races reference unknown language IDs:\n{string.Join("\n", failures)}");
    }

    [Fact]
    public void Races_Json_SubraceLanguageIdsReferenceKnownLanguages()
    {
        var langData = DeserializeFile<LanguagesData>("languages.json");
        var validLangIds = langData.Languages.Select(l => l.Id).ToHashSet();

        var raceData = DeserializeFile<RacesData>("races.json");

        var failures = new List<string>();
        foreach (var race in raceData.Races)
        {
            foreach (var subrace in race.Subraces)
            {
                foreach (var langId in subrace.LanguageIds)
                {
                    if (!validLangIds.Contains(langId))
                        failures.Add($"Subrace '{subrace.Id}' (race '{race.Id}') languageIds references unknown language '{langId}'");
                }
            }
        }

        Assert.True(failures.Count == 0,
            $"Subraces reference unknown language IDs:\n{string.Join("\n", failures)}");
    }
}
