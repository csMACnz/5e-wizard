using System.Reflection;
using NJsonSchema;
using Newtonsoft.Json.Linq;

namespace CharacterWizard.Tests;

/// <summary>
/// Validates all /data JSON files against their corresponding /schemas JSON Schemas.
/// </summary>
public class SchemaValidationTests
{
    private static string RepoRoot { get; } = GetRepoRoot();

    private static string GetRepoRoot()
    {
        // Walk up from test assembly location to repo root
        var dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!;
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "README.md")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not locate repo root.");
    }

    private static async Task<JsonSchema> LoadSchemaAsync(string schemaFileName)
    {
        var schemaPath = Path.Combine(RepoRoot, "schemas", schemaFileName);
        Assert.True(File.Exists(schemaPath), $"Schema file not found: {schemaPath}");
        return await JsonSchema.FromFileAsync(schemaPath);
    }

    private static JToken LoadDataFile(string dataFileName)
    {
        var dataPath = Path.Combine(RepoRoot, "data", dataFileName);
        Assert.True(File.Exists(dataPath), $"Data file not found: {dataPath}");
        var json = File.ReadAllText(dataPath);
        return JToken.Parse(json);
    }

    [Fact]
    public async Task RacesJson_ValidatesAgainstRaceSchema()
    {
        var schema = await LoadSchemaAsync("race.schema.json");
        var data = LoadDataFile("races.json");

        var errors = schema.Validate(data);
        Assert.True(errors.Count == 0,
            $"races.json has {errors.Count} schema error(s):\n" +
            string.Join("\n", errors.Select(e => $"  [{e.Path}] {e.Kind}: {e.Property}")));
    }

    [Fact]
    public async Task ClassesJson_ValidatesAgainstClassSchema()
    {
        var schema = await LoadSchemaAsync("class.schema.json");
        var data = LoadDataFile("classes.json");

        var errors = schema.Validate(data);
        Assert.True(errors.Count == 0,
            $"classes.json has {errors.Count} schema error(s):\n" +
            string.Join("\n", errors.Select(e => $"  [{e.Path}] {e.Kind}: {e.Property}")));
    }

    [Fact]
    public async Task SpellsJson_ValidatesAgainstSpellSchema()
    {
        var schema = await LoadSchemaAsync("spell.schema.json");
        var data = LoadDataFile("spells.json");

        var errors = schema.Validate(data);
        Assert.True(errors.Count == 0,
            $"spells.json has {errors.Count} schema error(s):\n" +
            string.Join("\n", errors.Select(e => $"  [{e.Path}] {e.Kind}: {e.Property}")));
    }

    [Fact]
    public async Task FeatsJson_ValidatesAgainstFeatSchema()
    {
        var schema = await LoadSchemaAsync("feat.schema.json");
        var data = LoadDataFile("feats.json");

        var errors = schema.Validate(data);
        Assert.True(errors.Count == 0,
            $"feats.json has {errors.Count} schema error(s):\n" +
            string.Join("\n", errors.Select(e => $"  [{e.Path}] {e.Kind}: {e.Property}")));
    }

    [Fact]
    public void DataFiles_HaveRequiredFields()
    {
        var required = new[]
        {
            "data/data-version.json",
            "data/abilities.json",
            "data/races.json",
            "data/classes.json",
            "data/backgrounds.json",
            "data/spells.json",
            "data/equipment.json",
        };

        foreach (var rel in required)
        {
            var path = Path.Combine(RepoRoot, rel);
            Assert.True(File.Exists(path), $"Required data file missing: {rel}");
            var content = File.ReadAllText(path).Trim();
            Assert.False(string.IsNullOrEmpty(content), $"Data file is empty: {rel}");
            // Should be parseable JSON
            var token = JToken.Parse(content);
            Assert.NotNull(token);
        }
    }

    [Fact]
    public void DataFiles_DoNotContainDisallowedPhrases()
    {
        // Patterns likely to indicate PHB copyrighted flavor text (not SRD)
        var disallowedPatterns = new[]
        {
            "Player's Handbook",
            "PHB p.",
            "Wizards of the Coast owns",
        };

        var dataDir = Path.Combine(RepoRoot, "data");
        var files = Directory.GetFiles(dataDir, "*.json");
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            foreach (var pattern in disallowedPatterns)
            {
                Assert.False(
                    content.Contains(pattern, StringComparison.OrdinalIgnoreCase),
                    $"{Path.GetFileName(file)} contains disallowed phrase: \"{pattern}\"");
            }
        }
    }
}
