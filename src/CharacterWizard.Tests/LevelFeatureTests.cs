using System.Text.Json;
using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

/// <summary>
/// Tests for the by-level unlocks feature:
/// feat type field, LevelFeatureValidator, AsiChoice serialisation,
/// exportable-choice filtering and BuildCharacter features population.
/// </summary>
public class LevelFeatureTests
{
    // ── Data path ────────────────────────────────────────────────────────

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

    // ── §1 — feat type field ─────────────────────────────────────────────

    [Fact]
    public void Feats_Json_AllFeatsHaveValidType()
    {
        var data = DeserializeFile<FeatsData>("feats.json");
        var validTypes = new HashSet<string> { "asi", "class", "background", "general" };

        foreach (var feat in data.Feats)
        {
            Assert.False(string.IsNullOrEmpty(feat.Type),
                $"Feat '{feat.Id}' has no type field");
            Assert.True(validTypes.Contains(feat.Type),
                $"Feat '{feat.Id}' has invalid type '{feat.Type}'");
        }
    }

    [Fact]
    public void Feats_Json_ExactlyOneAsiSentinel()
    {
        var data = DeserializeFile<FeatsData>("feats.json");
        var asiCount = data.Feats.Count(f => f.Type == "asi");
        Assert.Equal(1, asiCount);
        var asiFeats = data.Feats.Where(f => f.Type == "asi").ToList();
        Assert.Equal("feat:asi", asiFeats[0].Id);
    }

    [Fact]
    public void Feats_Json_Total214Feats()
    {
        var data = DeserializeFile<FeatsData>("feats.json");
        Assert.Equal(214, data.Feats.Count);
    }

    [Fact]
    public void Feats_Json_BackgroundFeatsMatchBackgrounds()
    {
        var bgData = DeserializeFile<BackgroundsData>("backgrounds.json");
        var bgFeatIds = bgData.Backgrounds
            .Where(b => !string.IsNullOrEmpty(b.FeatureId))
            .Select(b => b.FeatureId)
            .ToHashSet();

        var featData = DeserializeFile<FeatsData>("feats.json");
        var backgroundTypeFeatIds = featData.Feats
            .Where(f => f.Type == "background")
            .Select(f => f.Id)
            .ToHashSet();

        // Every background feat ID in feats.json should be referenced by a background
        foreach (var featId in backgroundTypeFeatIds)
            Assert.True(bgFeatIds.Contains(featId),
                $"Feat '{featId}' has type 'background' but no background references it");

        // Every background featureId should have a matching feat with type 'background'
        foreach (var bgFeatId in bgFeatIds)
            Assert.True(backgroundTypeFeatIds.Contains(bgFeatId),
                $"Background featureId '{bgFeatId}' has no matching feat with type 'background'");
    }

    [Fact]
    public void Feats_Json_ClassFeatIdsArePresentInClassFeaturesByLevel()
    {
        var clsData = DeserializeFile<ClassesData>("classes.json");
        var classFeatureIds = new HashSet<string>();
        foreach (var cls in clsData.Classes)
            foreach (var (_, ids) in cls.FeaturesByLevel)
                foreach (var id in ids)
                    classFeatureIds.Add(id);

        var featData = DeserializeFile<FeatsData>("feats.json");
        foreach (var feat in featData.Feats.Where(f => f.Type == "class"))
        {
            Assert.True(classFeatureIds.Contains(feat.Id),
                $"Feat '{feat.Id}' has type 'class' but does not appear in any class featuresByLevel");
        }
    }

    [Fact]
    public void Feats_Json_GeneralFeatsNotReferencedByClassOrBackground()
    {
        var clsData = DeserializeFile<ClassesData>("classes.json");
        var classFeatureIds = new HashSet<string>();
        foreach (var cls in clsData.Classes)
            foreach (var (_, ids) in cls.FeaturesByLevel)
                foreach (var id in ids)
                    classFeatureIds.Add(id);

        var bgData = DeserializeFile<BackgroundsData>("backgrounds.json");
        var bgFeatIds = bgData.Backgrounds
            .Where(b => !string.IsNullOrEmpty(b.FeatureId))
            .Select(b => b.FeatureId)
            .ToHashSet();

        var featData = DeserializeFile<FeatsData>("feats.json");
        foreach (var feat in featData.Feats.Where(f => f.Type == "general"))
        {
            Assert.False(classFeatureIds.Contains(feat.Id),
                $"General feat '{feat.Id}' appears in a class featuresByLevel");
            Assert.False(bgFeatIds.Contains(feat.Id),
                $"General feat '{feat.Id}' appears as a background featureId");
        }
    }

    // ── §2 — Data integrity cross-file ──────────────────────────────────

    [Fact]
    public void Classes_FeaturesByLevel_AllFeatIdsResolvable()
    {
        var featData = DeserializeFile<FeatsData>("feats.json");
        var validFeatIds = featData.Feats.Select(f => f.Id).ToHashSet();

        var clsData = DeserializeFile<ClassesData>("classes.json");
        foreach (var cls in clsData.Classes)
            foreach (var (level, ids) in cls.FeaturesByLevel)
                foreach (var id in ids)
                    Assert.True(validFeatIds.Contains(id),
                        $"Class '{cls.Id}' featuresByLevel[{level}] references unknown feat '{id}'");
    }

    [Fact]
    public void Backgrounds_FeatureId_AllResolvable()
    {
        var featData = DeserializeFile<FeatsData>("feats.json");
        var validFeatIds = featData.Feats.Select(f => f.Id).ToHashSet();

        var bgData = DeserializeFile<BackgroundsData>("backgrounds.json");
        foreach (var bg in bgData.Backgrounds)
        {
            if (!string.IsNullOrEmpty(bg.FeatureId))
                Assert.True(validFeatIds.Contains(bg.FeatureId),
                    $"Background '{bg.Id}' featureId '{bg.FeatureId}' does not resolve to a known feat");
        }
    }

    // ── §3 — LevelFeatureValidator ───────────────────────────────────────

    private static readonly List<FeatDefinition> TestFeats =
    [
        new FeatDefinition { Id = "feat:asi", DisplayName = "ASI", Type = "asi", Source = "SRD" },
        new FeatDefinition { Id = "feat:rage", DisplayName = "Rage", Type = "class", Source = "SRD" },
        new FeatDefinition { Id = "feat:alert", DisplayName = "Alert", Type = "general", Source = "SRD" },
        new FeatDefinition { Id = "feat:tough", DisplayName = "Tough", Type = "general", Source = "SRD" },
    ];

    private static readonly List<ClassDefinition> TestClasses =
    [
        new ClassDefinition
        {
            Id = "class:fighter",
            DisplayName = "Fighter",
            HitDie = 10,
            SavingThrows = ["STR", "CON"],
            SkillChoices = new SkillChoices { Count = 2, Options = ["skill:athletics"] },
            MulticlassPrereqs = new Dictionary<string, int> { ["STR"] = 13 },
            SubclassLevel = 3,
            FeaturesByLevel = new Dictionary<string, List<string>>
            {
                ["4"] = ["feat:asi"],
                ["8"] = ["feat:asi"],
            },
        },
    ];

    [Fact]
    public void LevelFeatureValidator_NoAsiChoices_ProducesWarningsForEachAsiLevel()
    {
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 8 }],
            AsiChoices = [],
        };

        var result = new LevelFeatureValidator(TestClasses, TestFeats).Validate(character);

        // Should warn for level 4 AND level 8
        Assert.Equal(2, result.Warnings.Count);
        Assert.All(result.Warnings, w => Assert.Contains("WARN_ASI_INCOMPLETE", w));
        Assert.True(result.IsValid, "Warnings must not block IsValid");
    }

    [Fact]
    public void LevelFeatureValidator_AllChoicesMade_NoWarnings()
    {
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 8 }],
            AsiChoices =
            [
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 4, Mode = "plus2", AbilityOne = "STR" },
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 8, Mode = "split", AbilityOne = "CON", AbilityTwo = "WIS" },
            ],
        };

        var result = new LevelFeatureValidator(TestClasses, TestFeats).Validate(character);

        Assert.Empty(result.Warnings);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void LevelFeatureValidator_LevelTooLowForAsi_NoWarning()
    {
        // Fighter at level 3 — ASI at level 4 has not fired yet
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 3 }],
            AsiChoices = [],
        };

        var result = new LevelFeatureValidator(TestClasses, TestFeats).Validate(character);

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void LevelFeatureValidator_InvalidFeatChoice_ProducesError()
    {
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 4 }],
            AsiChoices =
            [
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 4, Mode = "feat", FeatId = "feat:rage" }, // not a general feat
            ],
        };

        var result = new LevelFeatureValidator(TestClasses, TestFeats).Validate(character);

        Assert.Contains(result.Errors, e => e.Contains("ERR_ASI_INVALID_FEAT"));
    }

    [Fact]
    public void LevelFeatureValidator_ValidGeneralFeatChoice_NoErrors()
    {
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 4 }],
            AsiChoices =
            [
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 4, Mode = "feat", FeatId = "feat:alert" },
            ],
        };

        var result = new LevelFeatureValidator(TestClasses, TestFeats).Validate(character);

        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void LevelFeatureValidator_UnknownFeatId_ProducesError()
    {
        var character = new Character
        {
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 4 }],
            AsiChoices =
            [
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 4, Mode = "feat", FeatId = "feat:nonexistent" },
            ],
        };

        var result = new LevelFeatureValidator(TestClasses, TestFeats).Validate(character);

        Assert.Contains(result.Errors, e => e.Contains("ERR_ASI_INVALID_FEAT"));
    }

    // ── §4 — AsiChoice serialisation round-trip ──────────────────────────

    [Fact]
    public void AsiChoice_RoundTrip_Json()
    {
        var character = new Character
        {
            Name = "Test Fighter",
            TotalLevel = 8,
            Levels = [new ClassLevel { ClassId = "class:fighter", Level = 8 }],
            AsiChoices =
            [
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 4, Mode = "plus2", AbilityOne = "STR" },
                new AsiChoice { ClassId = "class:fighter", ClassLevel = 8, Mode = "feat", FeatId = "feat:alert" },
            ],
        };

        var json = JsonSerializer.Serialize(character, new JsonSerializerOptions { WriteIndented = false });
        var restored = JsonSerializer.Deserialize<Character>(json);

        Assert.NotNull(restored);
        Assert.Equal(2, restored!.AsiChoices.Count);

        var choice4 = restored.AsiChoices.First(a => a.ClassLevel == 4);
        Assert.Equal("class:fighter", choice4.ClassId);
        Assert.Equal("plus2", choice4.Mode);
        Assert.Equal("STR", choice4.AbilityOne);
        Assert.Null(choice4.FeatId);

        var choice8 = restored.AsiChoices.First(a => a.ClassLevel == 8);
        Assert.Equal("feat", choice8.Mode);
        Assert.Equal("feat:alert", choice8.FeatId);
        Assert.Null(choice8.AbilityOne);
    }

    [Fact]
    public void Character_WithNoAsiChoices_SerialisesAndRestoresEmptyList()
    {
        var character = new Character { Name = "New Character" };
        var json = JsonSerializer.Serialize(character);
        var restored = JsonSerializer.Deserialize<Character>(json);

        Assert.NotNull(restored);
        Assert.Empty(restored!.AsiChoices);
    }
}
