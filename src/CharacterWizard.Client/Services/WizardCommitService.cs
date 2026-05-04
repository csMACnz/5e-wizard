using CharacterWizard.Shared.Models;
using CharacterWizard.Shared.Validation;

namespace CharacterWizard.Client.Services;

/// <summary>
/// Maps all wizard UI state (WizardContext) onto the character model (CharacterWizardState.Character).
/// Extracted from the monolithic Wizard.razor CommitAllToCharacter() method.
/// </summary>
public sealed class WizardCommitService(WizardContext ctx, CharacterWizardState state)
{
    public void CommitAll(
        IReadOnlyList<RaceDefinition> races,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<BackgroundDefinition> backgrounds,
        IReadOnlyList<SpellDefinition> spells,
        IReadOnlyList<ClassStartingEquipmentEntry> classStartingEquipmentConfigs,
        AbilitiesConfig abilitiesConfig)
    {
        var c = state.Character;

        // Step 1 — Meta
        c.Name = ctx.CharacterName;
        c.PlayerName = string.IsNullOrWhiteSpace(ctx.PlayerName) ? null : ctx.PlayerName;
        c.Campaign = string.IsNullOrWhiteSpace(ctx.CampaignName) ? null : ctx.CampaignName;
        if (Enum.TryParse<GenerationMethod>(ctx.GenerationMethodStr, out var gm))
            c.GenerationMethod = gm;

        // Step 2 — Ability Scores (base values)
        void SetBase(AbilityBlock block, string ab) =>
            block.Base = GetAbilityBase(ab, c.GenerationMethod, abilitiesConfig);
        SetBase(c.AbilityScores.STR, "STR");
        SetBase(c.AbilityScores.DEX, "DEX");
        SetBase(c.AbilityScores.CON, "CON");
        SetBase(c.AbilityScores.INT, "INT");
        SetBase(c.AbilityScores.WIS, "WIS");
        SetBase(c.AbilityScores.CHA, "CHA");

        // Step 3 — Race & racial bonuses
        c.RaceId = ctx.SelectedRaceId;
        c.SubraceId = string.IsNullOrEmpty(ctx.SelectedSubraceId) ? null : ctx.SelectedSubraceId;
        var selectedRace = races.FirstOrDefault(r => r.Id == ctx.SelectedRaceId);
        if (selectedRace != null)
        {
            var bonuses = WizardContext.GetCombinedRacialBonuses(selectedRace, ctx.SelectedSubraceId);
            void SetRacial(AbilityBlock block, string ab) =>
                block.RacialBonus = bonuses.TryGetValue(ab, out int b) ? b : 0;
            SetRacial(c.AbilityScores.STR, "STR");
            SetRacial(c.AbilityScores.DEX, "DEX");
            SetRacial(c.AbilityScores.CON, "CON");
            SetRacial(c.AbilityScores.INT, "INT");
            SetRacial(c.AbilityScores.WIS, "WIS");
            SetRacial(c.AbilityScores.CHA, "CHA");
        }

        // Step 4 — Class levels
        c.Levels = ctx.ClassEntries
            .Where(e => !string.IsNullOrEmpty(e.ClassId))
            .Select(e => new ClassLevel
            {
                ClassId = e.ClassId,
                Level = e.Level,
                SubclassId = string.IsNullOrEmpty(e.SubclassId) ? null : e.SubclassId,
            })
            .ToList();
        c.TotalLevel = c.Levels.Sum(l => l.Level);

        // Step 5 — Features & ASI choices
        c.AsiChoices.Clear();
        c.AsiChoices.AddRange(GetExportableAsiChoices());

        // Race traits + subrace traits + background feature + class features
        c.Features.Clear();
        if (selectedRace != null)
        {
            foreach (var traitId in selectedRace.TraitIds)
                c.Features.Add(new CharacterFeature { FeatureId = traitId, SourceId = selectedRace.Id });
            if (!string.IsNullOrEmpty(c.SubraceId))
            {
                var sub = selectedRace.Subraces.FirstOrDefault(s => s.Id == c.SubraceId);
                if (sub != null)
                    foreach (var traitId in sub.TraitIds)
                        c.Features.Add(new CharacterFeature { FeatureId = traitId, SourceId = sub.Id });
            }
        }

        var bgForFeatures = backgrounds.FirstOrDefault(b => b.Id == ctx.SelectedBackgroundId);
        if (bgForFeatures != null && !string.IsNullOrEmpty(bgForFeatures.FeatureId))
            c.Features.Add(new CharacterFeature { FeatureId = bgForFeatures.FeatureId, SourceId = bgForFeatures.Id });

        foreach (var lvl in c.Levels)
        {
            var clsDef = classes.FirstOrDefault(cl => cl.Id == lvl.ClassId);
            if (clsDef == null) continue;
            for (int lvlNum = 1; lvlNum <= lvl.Level; lvlNum++)
            {
                if (!clsDef.FeaturesByLevel.TryGetValue(lvlNum.ToString(), out var featIds)) continue;
                foreach (var featId in featIds)
                {
                    if (featId == "feat:asi") continue;
                    c.Features.Add(new CharacterFeature { FeatureId = featId, SourceId = lvl.ClassId });
                }
            }
        }

        // General feats from ASI choices
        foreach (var choice in c.AsiChoices)
            if (choice.FeatId != null)
                c.Features.Add(new CharacterFeature { FeatureId = choice.FeatId, SourceId = choice.ClassId });

        // Apply ASI ability bumps
        c.AbilityScores.STR.OtherBonus = 0;
        c.AbilityScores.DEX.OtherBonus = 0;
        c.AbilityScores.CON.OtherBonus = 0;
        c.AbilityScores.INT.OtherBonus = 0;
        c.AbilityScores.WIS.OtherBonus = 0;
        c.AbilityScores.CHA.OtherBonus = 0;
        foreach (var choice in c.AsiChoices)
        {
            if (choice.FeatId != null) continue;
            if (choice.AbilityOne != null)
            {
                int bonus = choice.Mode == "split" ? 1 : 2;
                AddOtherBonus(c.AbilityScores, choice.AbilityOne, bonus);
            }
            if (choice.Mode == "split" && choice.AbilityTwo != null)
                AddOtherBonus(c.AbilityScores, choice.AbilityTwo, 1);
        }

        // HP entries from shadow state (only exportable levels)
        c.HitPointEntries.Clear();
        foreach (var classLevel in c.Levels)
        {
            var clsDef = classes.FirstOrDefault(cl => cl.Id == classLevel.ClassId);
            int hitDie = clsDef?.HitDie ?? 8;
            int average = (hitDie / 2) + 1;
            for (int lvlNum = 1; lvlNum <= classLevel.Level; lvlNum++)
            {
                string key = $"{classLevel.ClassId}|{lvlNum}";
                string method;
                int dieRollValue;
                if (lvlNum == 1)
                {
                    method = "average";
                    dieRollValue = hitDie;
                }
                else if (ctx.AllHpChoicesByKey.TryGetValue(key, out var saved))
                {
                    method = saved.Method;
                    dieRollValue = saved.DieRollValue;
                }
                else
                {
                    method = "average";
                    dieRollValue = average;
                }

                c.HitPointEntries.Add(new HitPointEntry
                {
                    ClassLevel = lvlNum,
                    ClassId = classLevel.ClassId,
                    Method = method,
                    DieRollValue = dieRollValue,
                });
            }
        }

        // Step 6 — Background & skills
        c.BackgroundId = ctx.SelectedBackgroundId;
        c.Skills.Clear();
        var bg = backgrounds.FirstOrDefault(b => b.Id == ctx.SelectedBackgroundId);
        if (bg != null)
            foreach (var sk in bg.SkillProficiencies)
                c.Skills[sk] = "background";
        for (int i = 0; i < ctx.ClassSkillOptionIds.Count; i++)
            if (ctx.ClassSkillSelections[i] && !c.Skills.ContainsKey(ctx.ClassSkillOptionIds[i]))
                c.Skills[ctx.ClassSkillOptionIds[i]] = "class";

        // Languages: fixed (race) ∪ chosen extras
        c.Proficiencies.Languages.Clear();
        c.Proficiencies.Languages.AddRange(
            ctx.GetAllLanguageIds(races, backgrounds));

        // Step 7 — Spells
        c.Spells.Clear();
        foreach (var (classId, spellIds) in ctx.SelectedSpells)
            foreach (var spellId in spellIds)
            {
                var spellDef = spells.FirstOrDefault(s => s.Id == spellId);
                c.Spells.Add(new CharacterSpell
                {
                    SpellId = spellId,
                    ClassId = classId,
                    Prepared = spellDef?.Level > 0,
                });
            }

        // Auto-apply subclass bonus spells
        foreach (var classLevel in c.Levels)
        {
            if (classLevel.SubclassId == null) continue;
            var clsDef = classes.FirstOrDefault(cl => cl.Id == classLevel.ClassId);
            var subclassDef = clsDef?.SubclassOptions.FirstOrDefault(s => s.Id == classLevel.SubclassId);
            if (subclassDef?.BonusSpells == null) continue;
            foreach (var bonus in subclassDef.BonusSpells)
            {
                if (classLevel.Level < bonus.GrantLevel) continue;
                if (c.Spells.Any(s => s.SpellId == bonus.SpellId)) continue;
                c.Spells.Add(new CharacterSpell { SpellId = bonus.SpellId, ClassId = classLevel.ClassId, Prepared = true });
            }
        }

        // Racial cantrip
        if (!string.IsNullOrEmpty(ctx.SelectedRacialCantripId))
        {
            string racialSourceId = !string.IsNullOrEmpty(ctx.SelectedSubraceId) ? ctx.SelectedSubraceId : ctx.SelectedRaceId;
            if (!c.Spells.Any(s => s.SpellId == ctx.SelectedRacialCantripId))
                c.Spells.Add(new CharacterSpell { SpellId = ctx.SelectedRacialCantripId, ClassId = racialSourceId, Prepared = false });
        }

        // Wizard spellbook
        foreach (var spellId in ctx.WizardSpellbookIds)
            if (!c.Spells.Any(s => s.SpellId == spellId && s.ClassId == "class:wizard"))
                c.Spells.Add(new CharacterSpell { SpellId = spellId, ClassId = "class:wizard", Prepared = false });

        // Magical secrets
        foreach (var (_, spellIds) in ctx.MagicalSecretsSelections)
            foreach (var spellId in spellIds)
                if (!string.IsNullOrEmpty(spellId) && !c.Spells.Any(s => s.SpellId == spellId))
                    c.Spells.Add(new CharacterSpell { SpellId = spellId, ClassId = "class:bard", Prepared = true });

        // Mystic arcanum
        foreach (var (_, spellId) in ctx.MysticArcanumSelections)
            if (!string.IsNullOrEmpty(spellId) && !c.Spells.Any(s => s.SpellId == spellId))
                c.Spells.Add(new CharacterSpell { SpellId = spellId, ClassId = "class:warlock", Prepared = false });

        // Step 8 — Equipment
        c.Equipment.Clear();
        c.StartingEquipmentChoices.Clear();
        c.ClassStartingWealthChosen = ctx.ClassStartingWealthChosen;
        c.ClassStartingGold = ctx.ClassStartingGold;

        // Background equipment is always added regardless of wealth path
        var bgForCommit = backgrounds.FirstOrDefault(b => b.Id == ctx.SelectedBackgroundId);
        foreach (var bgItemId in bgForCommit?.StartingEquipmentIds ?? [])
            c.Equipment.Add(new CharacterEquipmentItem { ItemId = bgItemId, Quantity = 1 });

        if (!ctx.ClassStartingWealthChosen)
        {
            var primaryClassId = ctx.ClassEntries.Count > 0 ? ctx.ClassEntries[0].ClassId : string.Empty;
            var classConfig = classStartingEquipmentConfigs.FirstOrDefault(e => e.ClassId == primaryClassId);
            if (classConfig != null)
            {
                // Fixed items
                foreach (var fixedItem in classConfig.FixedItems)
                    if (!c.Equipment.Any(e => e.ItemId == fixedItem.ItemId))
                        c.Equipment.Add(new CharacterEquipmentItem { ItemId = fixedItem.ItemId, Quantity = fixedItem.Quantity });

                // Choice group items
                foreach (var (groupId, (optionId, pickedItemId)) in ctx.EquipmentChoices)
                {
                    var group = classConfig.ChoiceGroups.FirstOrDefault(g => g.Id == groupId);
                    var option = group?.Options.FirstOrDefault(o => o.Id == optionId);
                    if (option == null) continue;

                    c.StartingEquipmentChoices.Add(new EquipmentGroupChoice
                    {
                        GroupId = groupId,
                        ChosenOptionId = optionId,
                        PickedItemId = pickedItemId,
                    });

                    if (option.PickOne)
                    {
                        if (!string.IsNullOrEmpty(pickedItemId))
                        {
                            var grant = option.GrantItems.FirstOrDefault(g => g.ItemId == pickedItemId);
                            if (grant != null && !c.Equipment.Any(e => e.ItemId == grant.ItemId))
                                c.Equipment.Add(new CharacterEquipmentItem { ItemId = grant.ItemId, Quantity = grant.Quantity });
                        }
                    }
                    else
                    {
                        foreach (var grant in option.GrantItems)
                            if (!c.Equipment.Any(e => e.ItemId == grant.ItemId))
                                c.Equipment.Add(new CharacterEquipmentItem { ItemId = grant.ItemId, Quantity = grant.Quantity });
                    }
                }
            }
            else
            {
                // Fallback: legacy flat selection
                foreach (var itemId in ctx.SelectedEquipmentIds)
                    c.Equipment.Add(new CharacterEquipmentItem { ItemId = itemId, Quantity = 1 });
            }
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private int GetAbilityBase(string ability, GenerationMethod method, AbilitiesConfig abilitiesConfig) =>
        method switch
        {
            GenerationMethod.StandardArray => ctx.AbilitySelections.GetValueOrDefault(ability, 0),
            GenerationMethod.PointBuy => ctx.PointBuyValues.GetValueOrDefault(ability, abilitiesConfig.PointBuy.MinScore),
            GenerationMethod.Roll => ctx.RollValues.GetValueOrDefault(ability, RollValidator.MinScore),
            _ => 0,
        };

    private IEnumerable<AsiChoice> GetExportableAsiChoices()
    {
        foreach (var (_, choice) in ctx.AllAsiChoicesByKey)
        {
            var entry = ctx.ClassEntries.FirstOrDefault(e => e.ClassId == choice.ClassId);
            if (entry != null && entry.Level >= choice.ClassLevel)
                yield return choice;
        }
    }

    private static void AddOtherBonus(AbilityScores scores, string ability, int amount)
    {
        switch (ability)
        {
            case "STR": scores.STR.OtherBonus += amount; break;
            case "DEX": scores.DEX.OtherBonus += amount; break;
            case "CON": scores.CON.OtherBonus += amount; break;
            case "INT": scores.INT.OtherBonus += amount; break;
            case "WIS": scores.WIS.OtherBonus += amount; break;
            case "CHA": scores.CHA.OtherBonus += amount; break;
        }
    }
}
