﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Noggog;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;

namespace SlotsSlotsSlots
{
    class Program
    {
        static Lazy<Settings> _LazySettings = null!;
        static Settings Settings => _LazySettings.Value;

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings(
                    nickname: "Settings",
                    path: "settings.json",
                    out _LazySettings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SlotsSlotsSlots.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            float baseCarryWeightMult = Settings.BaseMultiplier;
            float effectMultiplier = Settings.CarryweightEffectMultiplier;
            float potionWeights = Settings.PotionSlotUse;
            float scrollWeights = Settings.ScrollSlotUse;
            bool noHealFromWeightless = Settings.WeightlessItemsOfferNoHealing;
            int minWeaponSlots = Settings.MinimumUsedWeaponSlots;
            int maxWeaponSlots = Settings.MaximumUsedWeaponSlots;
            int minArmorslots = Settings.MinimumUsedArmorSlots;
            int maxArmorslots = Settings.MaximumUsedArmorSlots;

            state.PatchMod.Races.Set(
                state.LoadOrder.PriorityOrder.Race().WinningOverrides()
                    .Where(r => r.HasKeyword(Skyrim.Keyword.ActorTypeNPC)
                        && !r.EditorID.Equals("TestRace"))
                    .Select(r => r.DeepCopy())
                    .Do(r =>
                    {
                        r.BaseCarryWeight *= baseCarryWeightMult;
                    })
            );


            (HashSet<IFormLinkGetter<IMagicEffectGetter>> carryWeight, HashSet<IFormLinkGetter<IMagicEffectGetter>> health) magicEffects = MagicEffects(state);

            var carryWeightSpells = new HashSet<(Dictionary<FormKey, List<FormKey>> SpellAndEffects,Dictionary<FormKey,HashSet<int>> EffectAndMagnitudes)>();

            var SpellAndEffects = new Dictionary<FormKey, List<FormKey>>();
            var EffectAndMagnitudes = new Dictionary<FormKey, HashSet<int>>();
            

            foreach (var spell in state.LoadOrder.PriorityOrder.Spell().WinningOverrides())
            {
                if (!spell.EditorID.ToString().Equals("AbDragon")) { 
                    var deepCopySpell = spell.DeepCopy();
                    foreach (var e in deepCopySpell.Effects)
                    {
                        foreach (var carryWeightEffect in magicEffects.carryWeight)
                        {
                            if (e.BaseEffect.Equals(carryWeightEffect))
                            {
                                float startingMagnitude = e.Data.Magnitude;

                                e.Data.Magnitude *= effectMultiplier;

                                SpellAndEffects.GetOrAdd(spell.FormKey).Add(e.BaseEffect.FormKey);

                                var finalMagnitudesHashset = new HashSet<int>();
                                finalMagnitudesHashset.Add((int)startingMagnitude);
                                if (EffectAndMagnitudes.TryGetValue(e.BaseEffect.FormKey, out var magnitudesHashSet))
                                {
                                    foreach (var magnitudeInSet in magnitudesHashSet)
                                    {
                                        finalMagnitudesHashset.Add(magnitudeInSet);
                                        finalMagnitudesHashset.Add(magnitudeInSet + (int)startingMagnitude);
                                    }
                                }
                                EffectAndMagnitudes.GetOrAdd(e.BaseEffect.FormKey).UnionWith(finalMagnitudesHashset);

                                carryWeightSpells.Add((SpellAndEffects, EffectAndMagnitudes));

                                if ((deepCopySpell.Description.ToString().Contains($"carry") || deepCopySpell.Description.ToString().Contains($"Carry")) && deepCopySpell.Description.ToString().Contains($"{(int)startingMagnitude}"))
                                {
                                    deepCopySpell.Description = deepCopySpell.Description
                                        .ToString()
                                        .Replace($"{(int)startingMagnitude}", $"{(int)e.Data.Magnitude}")
                                        .Replace($"Carry Weight is", "Slots are")
                                        .Replace($"Carry Weight", $"Number of Slots")
                                        .Replace($"carry weight is", "slots are")
                                        .Replace($"carry weight", $"number of slots");
                                    Console.WriteLine($"{spell.EditorID.ToString()} was considered a CarryWeight altering Spell and the description, if needed, adjusted:\n \"{ deepCopySpell.Description}\"\n");
                                }
                                state.PatchMod.Spells.Set(deepCopySpell);
                            }
                        }
                    }
                }
            };

            // The following could profit from optimization, way to many foreach loops.
            
            foreach (var perk in state.LoadOrder.PriorityOrder.Perk().WinningOverrides())
            {
                var deepCopyPerk = perk.DeepCopy();
                foreach (var effect in perk.ContainedFormLinks)
                {
                    if (!perk.Description.ToString().IsNullOrWhitespace())
                    {
                        foreach (var e in perk.Effects)
                        {
                            foreach (var fl in e.ContainedFormLinks)
                            {
                                foreach (var carryWeightSpell in carryWeightSpells)
                                {
                                    if (carryWeightSpell.SpellAndEffects.TryGetValue(fl.FormKey, out var spellEffectSet))
                                    {
                                        foreach (var spellEffect in spellEffectSet)
                                        {
                                            if (carryWeightSpell.EffectAndMagnitudes.TryGetValue(spellEffect, out var magnitudesList))
                                            {
                                                foreach (int magnitude in magnitudesList)
                                                {
                                                    if ((deepCopyPerk.Description.ToString().Contains($"carry") || deepCopyPerk.Description.ToString().Contains($"Carry")) && deepCopyPerk.Description.ToString().Contains($"{magnitude}"))
                                                    {
                                                        int slots = (int)(magnitude * effectMultiplier);
                                                        deepCopyPerk.Description = deepCopyPerk.Description
                                                            .ToString()
                                                            .Replace($" {magnitude} ", $" {slots} ")
                                                            .Replace($" {magnitude}.", $" {slots}.")
                                                            .Replace($" {magnitude},", $" {slots},")
                                                            .Replace($"Carry Weight is", "Slots are")
                                                            .Replace($"Carry Weight", $"Number of Slots")
                                                            .Replace($"carry weight is", "slots are")
                                                            .Replace($"carry weight", $"number of slots");
                                                        Console.WriteLine($"{perk.EditorID.ToString()} was considered a CarryWeight altering Perk and the description, if needed, adjusted:\n \"{ deepCopyPerk.Description}\"\n");

                                                        state.PatchMod.Perks.Set(deepCopyPerk);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                
                }
                
            };

            state.PatchMod.MiscItems.Set(
                state.LoadOrder.PriorityOrder.MiscItem().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));

            state.PatchMod.Scrolls.Set(
                state.LoadOrder.PriorityOrder.Scroll().WinningOverrides()
                    .Where(s => s.Weight != 0.0f)
                    .Select(s => s.DeepCopy())
                    .Do(s => s.Weight = scrollWeights));

            foreach (var ingestible in state.LoadOrder.PriorityOrder.Ingestible().WinningOverrides())
            {
                var ingestibleCopy = ingestible.DeepCopy();
                if (ingestible.HasKeyword(Skyrim.Keyword.VendorItemPotion))
                {
                    ingestibleCopy.Weight = potionWeights;
                }
                else if (!ingestible.EditorID.Equals("dunSleepingTreeCampSap"))
                {
                    ingestibleCopy.Weight = 0.0f;
                }
                foreach (var carryWeightEffect in magicEffects.carryWeight)
                {
                    foreach (var effect in ingestibleCopy.Effects)
                    {
                        if (carryWeightEffect.Equals(effect.BaseEffect))
                        {
                            effect.Data.Magnitude *= effectMultiplier;
                        }
                    }

                }
                if (noHealFromWeightless)
                {
                    foreach (var healthEffect in magicEffects.health)
                    {
                        foreach (var e in ingestibleCopy.Effects)
                        {
                            if (healthEffect.Equals(e.BaseEffect)
                            &&
                            !(ingestible.HasKeyword(Skyrim.Keyword.VendorItemPotion)
                            || ingestible.EditorID.Equals("dunSleepingTreeCampSap")))
                            {
                                e.Data.Magnitude = 0;
                            }
                        }
                    }
                }

                state.PatchMod.Ingestibles.Set(ingestibleCopy);            
            }



            foreach (var ingredient in state.LoadOrder.PriorityOrder.Ingredient().WinningOverrides())
            {           
                var ingredientCopy = ingredient.DeepCopy();
                ingredientCopy.Weight = 0.0f;
                foreach (var carryWeightEffect in magicEffects.carryWeight)
                {
                    foreach (var effect in ingredientCopy.Effects)
                    {
                        if (carryWeightEffect.Equals(effect.BaseEffect))
                        {
                            effect.Data.Magnitude *= effectMultiplier;
                        }
                    }
                
                }
                if (noHealFromWeightless)
                {
                    foreach (var healthMagicEffect in magicEffects.health)
                    {
                        foreach (var e in ingredientCopy.Effects)
                        {
                            if (healthMagicEffect.Equals(e.BaseEffect))
                            {
                                e.Data.Magnitude = 0;
                            }
                        }
                    }
                }
                state.PatchMod.Ingredients.Set(ingredientCopy);            
            }

            foreach (var objectEffect in state.LoadOrder.PriorityOrder.ObjectEffect().WinningOverrides()) 
            {
                foreach (var carryWeightEffect in magicEffects.carryWeight)
                {
                    var objectEffectCopy = objectEffect.DeepCopy();
                    foreach (var e in objectEffectCopy.Effects)
                    {
                        if (carryWeightEffect.Equals(e.BaseEffect))
                        {
                            e.Data.Magnitude *= effectMultiplier;
                            state.PatchMod.ObjectEffects.Set(objectEffectCopy);
                        }
                    }
                }
            }

            state.PatchMod.Books.Set(
                state.LoadOrder.PriorityOrder.Book().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));

            state.PatchMod.Ammunitions.Set(
                state.LoadOrder.PriorityOrder.Ammunition().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));

            state.PatchMod.SoulGems.Set(
                state.LoadOrder.PriorityOrder.SoulGem().WinningOverrides()
                    .Where(m => m.Weight != 0.0f)
                    .Select(m => m.DeepCopy())
                    .Do(m => m.Weight = 0.0f));

            var weapons = state.LoadOrder.PriorityOrder.Weapon().WinningOverrides();
            var weaponWeights = weapons
                                .Where(w => w.BasicStats?.Weight != 0)
                                .Select(w => w.BasicStats?.Weight ?? 0.0f);
            var weaponDistributions = MakeDistributions(weaponWeights, minWeaponSlots, maxWeaponSlots);

            foreach (var weapon in weapons)
            {
                var calculatedWeight = FindWeight(weaponDistributions, weapon.BasicStats.Weight);
                if (weapon.BasicStats.Weight == 0 || weapon.BasicStats.Weight == calculatedWeight) continue;

                var weaponCopy = weapon.DeepCopy();
                weaponCopy.BasicStats.Weight = calculatedWeight;
                state.PatchMod.Weapons.Set(weaponCopy);
            }

            var armorWithWeights = state.LoadOrder.PriorityOrder.Armor()
                                                                .WinningOverrides()
                                                                .Where(w => w.Weight != 0 && w.Weight != FindWeight(weaponDistributions, w.Weight));

            var armorDistributions = MakeDistributions(armorWithWeights.Select(w => w.Weight), minArmorslots, maxArmorslots);
            state.PatchMod.Armors.Set(
                    armorWithWeights
                    .Select(m => m.DeepCopy())
                    .Do(w =>
                    {
                        var weight = FindWeight(armorDistributions, w.Weight);
                        w.Weight = weight;
                    })
                
            );
        }

        private static float FindWeight(IEnumerable<(float MaxWeight, int Slots)> distributions, float weight)
        {
            var found = distributions.FirstOrDefault(d => d.MaxWeight >= weight);
            if (found == default) 
                found = distributions.Last();
            return found.Slots;
        }

        private static HashSet<(float MaxWeight, int Slots)> MakeDistributions(IEnumerable<float> weights, int minSlots = 1, int maxSlots = 5)
        {
            var warr = weights.ToArray();
            var deltaSlots = maxSlots - minSlots;
            var minWeight = (float)warr.Min();
            var maxWeight = (float)warr.Max();
            var deltaWeight = maxWeight - minWeight;
            var sectionSize = deltaWeight / (deltaSlots + 1);

            var output = new HashSet<(float MaxWeight, int Slots)>();
            var weight = minWeight + sectionSize;
            for (var slots = minSlots; slots <= maxSlots; slots += 1)
            {
                output.Add((weight, slots));
                weight += sectionSize;
            }

            return output;
        }

        private static (HashSet<IFormLinkGetter<IMagicEffectGetter>>, HashSet<IFormLinkGetter<IMagicEffectGetter>>) MagicEffects(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var foundCarryWeight = new HashSet<IFormLinkGetter<IMagicEffectGetter>>();
            var foundHealth = new HashSet<IFormLinkGetter<IMagicEffectGetter>>();
            foreach (var e in state.LoadOrder.PriorityOrder.MagicEffect().WinningOverrides())
            {
                if (e.Archetype.ActorValue.Equals(ActorValue.CarryWeight))
                {
                    foundCarryWeight.Add(e.AsLink());
                    var deepCopyEffect = e.DeepCopy();
                    if (deepCopyEffect.Description.ToString().Contains("carry") || deepCopyEffect.Description.ToString().Contains("Carry"))
                    {
                        deepCopyEffect.Description = deepCopyEffect.Description
                                .ToString()
                                .Replace("points from Carry Weight", "of your Slots")
                                .Replace($"Carry Weight is", "Slots are")
                                .Replace($"Carry Weight", $"Number of Slots")
                                .Replace($"carry weight is", "slots are")
                                .Replace($"carry weight", $"number of slots")
                                .Replace($"points",$"slots")
                                .Replace($"Points", $"Slots");
                        Console.WriteLine($"{deepCopyEffect} is altering Carry Weight, and its description was changed to:\n\"{deepCopyEffect.Description}\"\n");
                    }
                }
                if (e.Archetype.ActorValue.Equals(ActorValue.Health)
                    && !e.Flags.HasFlag(MagicEffect.Flag.Hostile)
                    && !e.Description.String.IsNullOrWhitespace())
                {
                    foundHealth.Add(e.AsLink());
                }
            }
            return (foundCarryWeight, foundHealth);
        }

    }
}
