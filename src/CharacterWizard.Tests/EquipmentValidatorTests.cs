using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Tests;

public class EquipmentValidatorTests
{
    private static readonly List<EquipmentItemDefinition> TestEquipment =
    [
        new EquipmentItemDefinition { Id = "item:longsword", DisplayName = "Longsword", Category = "weapon", Subcategory = "martial-melee" },
        new EquipmentItemDefinition { Id = "item:dagger", DisplayName = "Dagger", Category = "weapon", Subcategory = "simple-melee" },
        new EquipmentItemDefinition { Id = "item:leather-armor", DisplayName = "Leather Armor", Category = "armor", Subcategory = "light" },
    ];

    [Fact]
    public void EmptyEquipment_IsValid()
    {
        var character = new Character();
        var result = new EquipmentValidator(TestEquipment).Validate(character);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidEquipment_IsValid()
    {
        var character = new Character
        {
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:longsword", Quantity = 1 },
                new CharacterEquipmentItem { ItemId = "item:leather-armor", Quantity = 1 },
            ],
        };
        var result = new EquipmentValidator(TestEquipment).Validate(character);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void UnknownItem_IsInvalid()
    {
        var character = new Character
        {
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:does-not-exist", Quantity = 1 },
            ],
        };
        var result = new EquipmentValidator(TestEquipment).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_EQUIPMENT_UNKNOWN"));
    }

    [Fact]
    public void DuplicateItem_IsInvalid()
    {
        var character = new Character
        {
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:dagger", Quantity = 1 },
                new CharacterEquipmentItem { ItemId = "item:dagger", Quantity = 2 },
            ],
        };
        var result = new EquipmentValidator(TestEquipment).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_EQUIPMENT_DUPLICATE"));
    }

    [Fact]
    public void ZeroQuantity_IsInvalid()
    {
        var character = new Character
        {
            Equipment =
            [
                new CharacterEquipmentItem { ItemId = "item:dagger", Quantity = 0 },
            ],
        };
        var result = new EquipmentValidator(TestEquipment).Validate(character);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ERR_EQUIPMENT_QUANTITY"));
    }
}
