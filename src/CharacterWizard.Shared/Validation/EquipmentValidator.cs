using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates the equipment selections on a character against the known equipment data.
/// </summary>
public class EquipmentValidator
{
    private readonly IReadOnlyList<EquipmentItemDefinition> _equipment;

    public EquipmentValidator(IReadOnlyList<EquipmentItemDefinition> equipment)
    {
        _equipment = equipment;
    }

    public ValidationResult Validate(Character character, IReadOnlyList<string>? allowedIds = null)
    {
        var result = new ValidationResult();

        if (character.Equipment.Count == 0)
            return result;

        var validIds = _equipment.Select(e => e.Id).ToHashSet();
        var seenIds = new HashSet<string>();

        foreach (var item in character.Equipment)
        {
            if (!seenIds.Add(item.ItemId))
            {
                result.Errors.Add($"ERR_EQUIPMENT_DUPLICATE: Item '{item.ItemId}' appears more than once in the equipment list.");
                continue;
            }

            if (!validIds.Contains(item.ItemId))
            {
                result.Errors.Add($"ERR_EQUIPMENT_UNKNOWN: Item '{item.ItemId}' does not exist in the equipment data.");
            }

            if (item.Quantity < 1)
            {
                result.Errors.Add($"ERR_EQUIPMENT_QUANTITY: Item '{item.ItemId}' must have a quantity of at least 1.");
            }

            if (allowedIds != null && !allowedIds.Contains(item.ItemId))
            {
                result.Errors.Add($"ERR_EQUIPMENT_NOT_ALLOWED: Item '{item.ItemId}' is not a standard starting equipment choice for this class.");
            }
        }

        return result;
    }
}
