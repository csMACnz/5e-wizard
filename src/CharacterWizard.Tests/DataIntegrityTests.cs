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
    public void ClassStartingEquipment_Json_ClassIdReferencesKnownClass()
    {
        var classData = DeserializeFile<ClassesData>("classes.json");
        var validClassIds = classData.Classes.Select(c => c.Id).ToHashSet();

        var cseData = DeserializeFile<ClassStartingEquipmentData>("class-starting-equipment.json");

        foreach (var entry in cseData.Entries)
        {
            Assert.True(validClassIds.Contains(entry.ClassId),
                $"class-starting-equipment.json entry has unknown classId '{entry.ClassId}'");
        }
    }
}
