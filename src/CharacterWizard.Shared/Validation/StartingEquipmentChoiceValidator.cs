using CharacterWizard.Shared.Models;

namespace CharacterWizard.Shared.Validation;

/// <summary>
/// Validates a character's starting equipment choices against the configured choice groups
/// for their primary class. Enforces mutual exclusivity, cardinality, and the
/// class starting wealth alternative.
/// </summary>
public class StartingEquipmentChoiceValidator
{
    private readonly ClassStartingEquipmentEntry? _config;

    public StartingEquipmentChoiceValidator(ClassStartingEquipmentEntry? config)
    {
        _config = config;
    }

    public ValidationResult Validate(Character character)
    {
        var result = new ValidationResult();

        if (_config == null)
            return result;

        if (character.ClassStartingWealthChosen)
        {
            ValidateStartingWealthPath(character, result);
            return result;
        }

        ValidateChoiceGroupsPath(character, result);
        return result;
    }

    private void ValidateStartingWealthPath(Character character, ValidationResult result)
    {
        // When wealth is chosen, no class starting equipment should be present.
        // Build the set of all item IDs that appear in any choice group option or fixed items.
        var classItemIds = GetAllClassItemIds();

        var classEquipSelected = character.Equipment
            .Where(e => classItemIds.Contains(e.ItemId))
            .ToList();

        if (classEquipSelected.Count > 0)
        {
            var ids = string.Join(", ", classEquipSelected.Select(e => e.ItemId));
            result.Errors.Add(
                $"ERR_WEALTH_WITH_EQUIPMENT: Class starting equipment items were selected " +
                $"({ids}) but starting wealth was chosen. Select one or the other.");
        }

        if (_config != null && _config.StartingWealthRoll != string.Empty && character.ClassStartingGold == null)
        {
            result.Warnings.Add(
                "WARN_WEALTH_NO_GOLD: Starting wealth was chosen but no gold amount was recorded.");
        }

        if (character.ClassStartingGold.HasValue && character.ClassStartingGold.Value < 0)
        {
            result.Errors.Add(
                "ERR_WEALTH_NEGATIVE: Starting gold cannot be negative.");
        }
    }

    private void ValidateChoiceGroupsPath(Character character, ValidationResult result)
    {
        if (_config == null || _config.ChoiceGroups.Count == 0)
            return;

        var selectedItemIds = character.Equipment.Select(e => e.ItemId).ToHashSet();
        var choiceMap = character.StartingEquipmentChoices
            .ToDictionary(c => c.GroupId, c => c);

        foreach (var group in _config.ChoiceGroups)
        {
            if (!group.Required)
                continue;

            if (!choiceMap.TryGetValue(group.Id, out var choice))
            {
                result.Errors.Add(
                    $"ERR_CHOICE_MISSING: No selection made for required equipment choice group '{group.Id}' ({group.Description}).");
                continue;
            }

            var chosenOption = group.Options.FirstOrDefault(o => o.Id == choice.ChosenOptionId);
            if (chosenOption == null)
            {
                result.Errors.Add(
                    $"ERR_CHOICE_INVALID_OPTION: Option '{choice.ChosenOptionId}' is not valid for choice group '{group.Id}'.");
                continue;
            }

            if (chosenOption.PickOne)
            {
                ValidatePickOneOption(group, chosenOption, choice, selectedItemIds, result);
            }
            else
            {
                ValidateFixedOption(group, chosenOption, selectedItemIds, result);
            }
        }
    }

    private static void ValidatePickOneOption(
        EquipmentChoiceGroup group,
        EquipmentChoiceOption option,
        EquipmentGroupChoice choice,
        HashSet<string> selectedItemIds,
        ValidationResult result)
    {
        var validItemIds = option.GrantItems.Select(g => g.ItemId).ToHashSet();

        if (string.IsNullOrEmpty(choice.PickedItemId))
        {
            result.Errors.Add(
                $"ERR_PICK_ONE_MISSING: Choice group '{group.Id}' option '{option.Id}' " +
                $"requires picking one item from the list, but none was picked.");
            return;
        }

        if (!validItemIds.Contains(choice.PickedItemId))
        {
            result.Errors.Add(
                $"ERR_PICK_ONE_INVALID: Item '{choice.PickedItemId}' is not a valid selection for " +
                $"choice group '{group.Id}' option '{option.Id}'. " +
                $"Valid items: {string.Join(", ", validItemIds)}.");
        }
        else if (!selectedItemIds.Contains(choice.PickedItemId))
        {
            result.Errors.Add(
                $"ERR_PICK_ONE_NOT_IN_EQUIPMENT: Picked item '{choice.PickedItemId}' for choice group " +
                $"'{group.Id}' is not present in the character's equipment.");
        }
    }

    private static void ValidateFixedOption(
        EquipmentChoiceGroup group,
        EquipmentChoiceOption option,
        HashSet<string> selectedItemIds,
        ValidationResult result)
    {
        foreach (var grant in option.GrantItems)
        {
            if (!selectedItemIds.Contains(grant.ItemId))
            {
                result.Errors.Add(
                    $"ERR_FIXED_ITEM_MISSING: Item '{grant.ItemId}' required by choice group '{group.Id}' " +
                    $"option '{option.Id}' is not present in the character's equipment.");
            }
        }
    }

    private HashSet<string> GetAllClassItemIds()
    {
        if (_config == null)
            return [];

        var ids = new HashSet<string>(_config.FixedItemIds);
        foreach (var group in _config.ChoiceGroups)
            foreach (var option in group.Options)
                foreach (var grant in option.GrantItems)
                    ids.Add(grant.ItemId);

        return ids;
    }
}
