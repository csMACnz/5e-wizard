using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Client.Services;

/// <summary>
/// Validates a single wizard step using the current WizardContext UI state.
/// Extracted from the monolithic Wizard.razor ValidateStep(int step) method.
/// </summary>
public sealed class WizardStepValidator(WizardContext ctx, CharacterWizardState state)
{
    public ValidationResult ValidateStep(
        int step,
        AbilitiesConfig abilitiesConfig,
        IReadOnlyList<RaceDefinition> races,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<BackgroundDefinition> backgrounds,
        IReadOnlyList<SpellDefinition> spells,
        IReadOnlyList<EquipmentItemDefinition> equipment,
        IReadOnlyList<ClassStartingEquipmentEntry> classStartingEquipmentConfigs)
    {
        var result = new ValidationResult();
        switch (step)
        {
            case 0:
                if (string.IsNullOrWhiteSpace(ctx.CharacterName))
                    result.Errors.Add("Character name is required.");
                if (!Enum.TryParse<GenerationMethod>(ctx.GenerationMethodStr, out _))
                    result.Errors.Add("A generation method must be selected.");
                break;

            case 1:
                int[] bases =
                [
                    GetAbilityBase("STR", abilitiesConfig),
                    GetAbilityBase("DEX", abilitiesConfig),
                    GetAbilityBase("CON", abilitiesConfig),
                    GetAbilityBase("INT", abilitiesConfig),
                    GetAbilityBase("WIS", abilitiesConfig),
                    GetAbilityBase("CHA", abilitiesConfig),
                ];
                var abilityResult = state.Character.GenerationMethod switch
                {
                    GenerationMethod.StandardArray => StandardArrayValidator.Validate(bases, abilitiesConfig.StandardArray.Count > 0 ? abilitiesConfig.StandardArray : null),
                    GenerationMethod.PointBuy => PointBuyValidator.Validate(bases, abilitiesConfig.PointBuy),
                    GenerationMethod.Roll => RollValidator.Validate(bases, abilitiesConfig.Roll.Count > 0 ? abilitiesConfig.Roll.Count : RollValidator.RequiredCount),
                    _ => new ValidationResult(),
                };
                result.Errors.AddRange(abilityResult.Errors);
                result.Warnings.AddRange(abilityResult.Warnings);
                break;

            case 2:
                if (string.IsNullOrEmpty(ctx.SelectedRaceId))
                {
                    result.Errors.Add("A race must be selected.");
                    break;
                }

                var raceTemp = BuildTempCharacterForRace(races, abilitiesConfig);
                var raceResult = new RaceValidator(races).Validate(raceTemp);
                result.Errors.AddRange(raceResult.Errors);
                result.Warnings.AddRange(raceResult.Warnings);
                break;

            case 3:
                if (ctx.ClassEntries.All(e => string.IsNullOrEmpty(e.ClassId)))
                {
                    result.Errors.Add("At least one class must be selected.");
                    break;
                }

                var classTemp = new Character
                {
                    TotalLevel = ctx.TotalClassLevel,
                    Levels = ctx.ClassEntries
                        .Where(e => !string.IsNullOrEmpty(e.ClassId))
                        .Select(e => new ClassLevel
                        {
                            ClassId = e.ClassId,
                            Level = e.Level,
                            SubclassId = string.IsNullOrEmpty(e.SubclassId) ? null : e.SubclassId,
                        })
                        .ToList(),
                    AbilityScores = BuildTempAbilityScores(abilitiesConfig),
                };
                var classResult = new ClassValidator(classes).Validate(classTemp);
                result.Errors.AddRange(classResult.Errors);
                result.Warnings.AddRange(classResult.Warnings);
                break;

            case 4:
                // Features step: warn for incomplete ASI choices (non-blocking)
                {
                    var levels = ctx.ClassEntries.Where(e => !string.IsNullOrEmpty(e.ClassId)).ToList();
                    foreach (var entry in levels)
                    {
                        var clsDef = classes.FirstOrDefault(c => c.Id == entry.ClassId);
                        if (clsDef == null) continue;
                        foreach (var (levelKey, featIds) in clsDef.FeaturesByLevel)
                        {
                            if (!int.TryParse(levelKey, out int grantLevel)) continue;
                            if (grantLevel > entry.Level) continue;
                            if (!featIds.Contains("feat:asi")) continue;
                            string key = $"{entry.ClassId}|{grantLevel}";
                            if (!ctx.AllAsiChoicesByKey.ContainsKey(key))
                                result.Warnings.Add(
                                    $"WARN_ASI_INCOMPLETE: ASI at {clsDef.DisplayName} level {grantLevel} has not been made.");
                        }
                    }

                    // Validate HP die-roll values for manually-set entries
                    foreach (var classEntry in levels)
                    {
                        var clsDef = classes.FirstOrDefault(c => c.Id == classEntry.ClassId);
                        int hitDie = clsDef?.HitDie ?? 8;
                        string clsDisplay = clsDef?.DisplayName ?? classEntry.ClassId;
                        for (int lvlNum = 2; lvlNum <= classEntry.Level; lvlNum++)
                        {
                            string key = $"{classEntry.ClassId}|{lvlNum}";
                            if (ctx.AllHpChoicesByKey.TryGetValue(key, out var saved) && saved.Method == "manual")
                            {
                                if (saved.DieRollValue < 1 || saved.DieRollValue > hitDie)
                                    result.Errors.Add(
                                        $"ERR_HP_ROLL_OUT_OF_RANGE: HP for {clsDisplay} level {lvlNum} " +
                                        $"must be between 1 and {hitDie} (got {saved.DieRollValue}).");
                            }
                        }
                    }
                }
                break;

            case 5:
                if (string.IsNullOrEmpty(ctx.SelectedBackgroundId))
                {
                    result.Errors.Add("A background must be selected.");
                    break;
                }

                var profTemp = new Character
                {
                    BackgroundId = ctx.SelectedBackgroundId,
                    Levels = ctx.ClassEntries
                        .Where(e => !string.IsNullOrEmpty(e.ClassId))
                        .Select(e => new ClassLevel { ClassId = e.ClassId, Level = e.Level })
                        .ToList(),
                };
                var bgDef = backgrounds.FirstOrDefault(b => b.Id == ctx.SelectedBackgroundId);
                if (bgDef != null)
                    foreach (var sk in bgDef.SkillProficiencies)
                        profTemp.Skills[sk] = "background";
                for (int i = 0; i < ctx.ClassSkillOptionIds.Count; i++)
                    if (ctx.ClassSkillSelections[i] && !profTemp.Skills.ContainsKey(ctx.ClassSkillOptionIds[i]))
                        profTemp.Skills[ctx.ClassSkillOptionIds[i]] = "class";
                var profResult = new ProficiencyValidator(classes, backgrounds).Validate(profTemp);
                result.Errors.AddRange(profResult.Errors);
                result.Warnings.AddRange(profResult.Warnings);
                break;

            case 6:
                {
                    var spellTemp = new Character
                    {
                        Levels = ctx.ClassEntries
                            .Where(e => !string.IsNullOrEmpty(e.ClassId))
                            .Select(e => new ClassLevel { ClassId = e.ClassId, Level = e.Level })
                            .ToList(),
                    };
                    foreach (var (classId, spellIds) in ctx.SelectedSpells)
                        foreach (var spellId in spellIds)
                        {
                            var spellDef = spells.FirstOrDefault(s => s.Id == spellId);
                            spellTemp.Spells.Add(new CharacterSpell
                            {
                                SpellId = spellId,
                                ClassId = classId,
                                Prepared = spellDef?.Level > 0,
                            });
                        }

                    if (spellTemp.Spells.Count > 0)
                    {
                        var spellResult = new SpellValidator(spells, classes).Validate(spellTemp);
                        result.Errors.AddRange(spellResult.Errors);
                        result.Warnings.AddRange(spellResult.Warnings);
                    }
                }
                break;

            case 7:
                {
                    var primaryClsId = ctx.ClassEntries.Count > 0 ? ctx.ClassEntries[0].ClassId : string.Empty;
                    var classConfig = classStartingEquipmentConfigs.FirstOrDefault(e => e.ClassId == primaryClsId);

                    var equipTemp = new Character
                    {
                        ClassStartingWealthChosen = ctx.ClassStartingWealthChosen,
                        ClassStartingGold = ctx.ClassStartingGold,
                    };

                    foreach (var (groupId, (optionId, pickedItemId)) in ctx.EquipmentChoices)
                    {
                        equipTemp.StartingEquipmentChoices.Add(new EquipmentGroupChoice
                        {
                            GroupId = groupId,
                            ChosenOptionId = optionId,
                            PickedItemId = pickedItemId,
                        });

                        if (!ctx.ClassStartingWealthChosen && classConfig != null)
                        {
                            var group = classConfig.ChoiceGroups.FirstOrDefault(g => g.Id == groupId);
                            var option = group?.Options.FirstOrDefault(o => o.Id == optionId);
                            if (option != null)
                            {
                                if (option.PickOne)
                                {
                                    if (!string.IsNullOrEmpty(pickedItemId))
                                        equipTemp.Equipment.Add(new CharacterEquipmentItem { ItemId = pickedItemId, Quantity = 1 });
                                }
                                else
                                {
                                    foreach (var grant in option.GrantItems)
                                        equipTemp.Equipment.Add(new CharacterEquipmentItem { ItemId = grant.ItemId, Quantity = grant.Quantity });
                                }
                            }
                        }
                    }

                    if (ctx.ClassStartingWealthChosen)
                        foreach (var itemId in ctx.SelectedEquipmentIds)
                            equipTemp.Equipment.Add(new CharacterEquipmentItem { ItemId = itemId, Quantity = 1 });

                    if (classConfig != null)
                    {
                        var choiceResult = new StartingEquipmentChoiceValidator(classConfig).Validate(equipTemp);
                        result.Errors.AddRange(choiceResult.Errors);
                        result.Warnings.AddRange(choiceResult.Warnings);
                    }
                    else if (equipTemp.Equipment.Count > 0 || ctx.SelectedEquipmentIds.Count > 0)
                    {
                        var legacyTemp = new Character();
                        foreach (var itemId in ctx.SelectedEquipmentIds)
                            legacyTemp.Equipment.Add(new CharacterEquipmentItem { ItemId = itemId, Quantity = 1 });
                        IReadOnlyList<string>? strictAllowedIds = null;
                        if (ctx.StrictEquipment)
                        {
                            var primaryCls = classes.FirstOrDefault(c => c.Id == primaryClsId);
                            if (primaryCls?.StartingEquipmentIds.Count > 0)
                                strictAllowedIds = primaryCls.StartingEquipmentIds;
                        }
                        var equipResult = new EquipmentValidator(equipment).Validate(legacyTemp, strictAllowedIds);
                        result.Errors.AddRange(equipResult.Errors);
                        result.Warnings.AddRange(equipResult.Warnings);
                    }
                }
                break;

            case 8:
                // Review step is always passable
                break;
        }

        return result;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private int GetAbilityBase(string ability, AbilitiesConfig abilitiesConfig) =>
        state.Character.GenerationMethod switch
        {
            GenerationMethod.StandardArray => ctx.AbilitySelections.GetValueOrDefault(ability, 0),
            GenerationMethod.PointBuy => ctx.PointBuyValues.GetValueOrDefault(ability, abilitiesConfig.PointBuy.MinScore),
            GenerationMethod.Roll => ctx.RollValues.GetValueOrDefault(ability, RollValidator.MinScore),
            _ => 0,
        };

    private Character BuildTempCharacterForRace(IReadOnlyList<RaceDefinition> races, AbilitiesConfig abilitiesConfig)
    {
        var temp = new Character
        {
            RaceId = ctx.SelectedRaceId,
            SubraceId = string.IsNullOrEmpty(ctx.SelectedSubraceId) ? null : ctx.SelectedSubraceId,
            AbilityScores = BuildTempAbilityScores(abilitiesConfig),
        };
        var race = races.FirstOrDefault(r => r.Id == ctx.SelectedRaceId);
        if (race != null)
        {
            var bonuses = WizardContext.GetCombinedRacialBonuses(race, ctx.SelectedSubraceId);
            void SetRacial(AbilityBlock block, string ab) =>
                block.RacialBonus = bonuses.TryGetValue(ab, out int b) ? b : 0;
            SetRacial(temp.AbilityScores.STR, "STR");
            SetRacial(temp.AbilityScores.DEX, "DEX");
            SetRacial(temp.AbilityScores.CON, "CON");
            SetRacial(temp.AbilityScores.INT, "INT");
            SetRacial(temp.AbilityScores.WIS, "WIS");
            SetRacial(temp.AbilityScores.CHA, "CHA");
        }

        return temp;
    }

    private AbilityScores BuildTempAbilityScores(AbilitiesConfig abilitiesConfig) => new()
    {
        STR = new AbilityBlock { Base = GetAbilityBase("STR", abilitiesConfig) },
        DEX = new AbilityBlock { Base = GetAbilityBase("DEX", abilitiesConfig) },
        CON = new AbilityBlock { Base = GetAbilityBase("CON", abilitiesConfig) },
        INT = new AbilityBlock { Base = GetAbilityBase("INT", abilitiesConfig) },
        WIS = new AbilityBlock { Base = GetAbilityBase("WIS", abilitiesConfig) },
        CHA = new AbilityBlock { Base = GetAbilityBase("CHA", abilitiesConfig) },
    };
}
