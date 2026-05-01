using System.Text.Json.Serialization;

namespace CharacterWizard.Shared.Models;

/// <summary>
/// Represents a single item (with quantity) granted by choosing an equipment option.
/// </summary>
public class EquipmentGrantItem
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// One selectable option within an equipment choice group.
/// When pickOne is false, all items in grantItems are granted.
/// When pickOne is true, the player must choose exactly one item from grantItems.
/// </summary>
public class EquipmentChoiceOption
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("grantItems")]
    public List<EquipmentGrantItem> GrantItems { get; set; } = [];

    /// <summary>
    /// If true, the player must pick exactly one item from the GrantItems list.
    /// If false (default), all items in GrantItems are automatically granted.
    /// </summary>
    [JsonPropertyName("pickOne")]
    public bool PickOne { get; set; } = false;
}

/// <summary>
/// A group of mutually exclusive equipment options. The player must pick exactly one option.
/// </summary>
public class EquipmentChoiceGroup
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; } = true;

    [JsonPropertyName("options")]
    public List<EquipmentChoiceOption> Options { get; set; } = [];
}

/// <summary>
/// Starting equipment configuration for a single class.
/// </summary>
public class ClassStartingEquipmentEntry
{
    [JsonPropertyName("classId")]
    public string ClassId { get; set; } = string.Empty;

    /// <summary>
    /// The dice roll formula for the class starting wealth alternative (e.g. "2d4*10").
    /// Format: "{count}d{sides}*{multiplier}" where multiplier is optional (defaults to 1).
    /// </summary>
    [JsonPropertyName("startingWealthRoll")]
    public string StartingWealthRoll { get; set; } = string.Empty;

    /// <summary>
    /// Items (with quantity) that every character of this class receives automatically (no choice needed).
    /// </summary>
    [JsonPropertyName("fixedItems")]
    public List<EquipmentGrantItem> FixedItems { get; set; } = [];

    /// <summary>
    /// Mutually exclusive choice groups. For each required group, the player must pick one option.
    /// </summary>
    [JsonPropertyName("choiceGroups")]
    public List<EquipmentChoiceGroup> ChoiceGroups { get; set; } = [];
}

/// <summary>
/// Top-level container for the class-starting-equipment.json data file.
/// </summary>
public class ClassStartingEquipmentData
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<ClassStartingEquipmentEntry> Entries { get; set; } = [];
}

/// <summary>
/// Records the player's choice for a single equipment choice group.
/// </summary>
public class EquipmentGroupChoice
{
    /// <summary>The choice group ID this choice belongs to.</summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>The ID of the chosen option within the group.</summary>
    public string ChosenOptionId { get; set; } = string.Empty;

    /// <summary>
    /// If the chosen option has PickOne=true, this is the item ID the player picked
    /// from the option's GrantItems pool.
    /// </summary>
    public string? PickedItemId { get; set; }
}
