using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

public class ItemCost
{
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;
}

public class EquipmentItemDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("subcategory")]
    public string Subcategory { get; set; } = string.Empty;

    [JsonPropertyName("cost")]
    public ItemCost? Cost { get; set; }

    [JsonPropertyName("weight")]
    public double Weight { get; set; }
}

public class EquipmentData
{
    [JsonPropertyName("equipment")]
    public List<EquipmentItemDefinition> Equipment { get; set; } = [];
}
