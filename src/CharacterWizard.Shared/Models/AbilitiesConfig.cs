using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class PointBuyCost
{
    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("cost")]
    public int Cost { get; set; }
}

public class PointBuyConfig
{
    [JsonPropertyName("budget")]
    public int Budget { get; set; }

    [JsonPropertyName("minScore")]
    public int MinScore { get; set; }

    [JsonPropertyName("maxScore")]
    public int MaxScore { get; set; }

    [JsonPropertyName("costs")]
    public List<PointBuyCost> Costs { get; set; } = [];
}

public class RollConfig
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class AbilitiesConfig
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("standardArray")]
    public List<int> StandardArray { get; set; } = [];

    [JsonPropertyName("pointBuy")]
    public PointBuyConfig PointBuy { get; set; } = new();

    [JsonPropertyName("roll")]
    public RollConfig Roll { get; set; } = new();
}
