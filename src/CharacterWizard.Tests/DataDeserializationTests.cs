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
}
