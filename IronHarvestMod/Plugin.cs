using Basis.Presentation.Messages;
using Basis.Simulation;
using Basis.Simulation.BattlePlans;
using Basis.Simulation.Entities;
using Basis.Simulation.Entities.Configs;
using Basis.Simulation.Entities.Units;
using Basis.Simulation.Production;
using Basis.Tools.ReadonlyCollections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;

namespace IronHarvestMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static readonly int TICKS_PER_SECOND = 8;
        static ManualLogSource logger;

        private void Awake()
        {
            // Plugin startup logic
            logger = Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            foreach (var method in harmony.GetPatchedMethods())
            {
                Logger.LogInfo($"Patched method {method.Name}");
            }
        }

        class Patches_Battle
        {
            static int tickCount = 0;
            [HarmonyPatch(typeof(Battle), "ComputeTick")]
            class Patch_ComputeTick
            {
                static void Postfix(Battle __instance)
                {
                    ++tickCount;
                    if (tickCount % (TICKS_PER_SECOND * 10) == 0)
                    {
                        logger.LogDebug($"ComputeTick(): 10 second interval");

                        //var sceneTraverse = Traverse.Create(__instance.Scene);
                        //var updateableEntitiesProperty = sceneTraverse.Property("UpdateableEntities");
                        //var updateableEntities = updateableEntitiesProperty.GetValue() as IEnumerable;

                        //foreach (var updateableEntity in updateableEntities)
                        //{
                        //    if (updateableEntity.GetType().IsSubclassOf(typeof(Unit)) || updateableEntity.GetType() == typeof(Unit))
                        //    {
                        //        var unit = updateableEntity as Unit;
                        //        if (unit.Faction == unit.Battle.LocalPlayerFactionId)
                        //        {
                        //            float healAmount = unit.MaxHealth * 0.05f;
                        //            unit.DamageModel.Heal(healAmount, unit.EntityId);
                        //        }
                        //    }
                        //}

                        foreach (var unit in __instance.Scene.Units)
                        {
                            if (unit.Faction == __instance.LocalPlayerFactionId)
                            {
                                float healAmount = unit.MaxHealth * 0.05f;
                                unit.DamageModel.Heal(healAmount, unit.EntityId);
                            }
                        }

                        foreach (var structure in __instance.Scene.Structures)
                        {
                            if (structure.Faction == __instance.LocalPlayerFactionId)
                            {
                                float healAmount = structure.MaxHealth * 0.05f;
                                structure.DamageModel.Heal(healAmount, structure.EntityId);
                            }
                        }
                    }

                    if (tickCount % (TICKS_PER_SECOND * 60) == 0)
                    {
                        foreach (var unit in __instance.Scene.Units)
                        {
                            if (unit.Faction == __instance.LocalPlayerFactionId)
                            {
                                try
                                {
                                    logger.LogDebug($"Friendly squad: {unit.EntityId} {unit.SquadId} {unit.Squad}");
                                    if (unit.Squad == null)
                                    {
                                        continue;
                                    }
                                    unit.Squad.VeterancySystem.AddExperience(unit.Squad.VeterancySystem.GetNeededExperienceForLevelUp(), RankChangedReason.Experience);
                                    var healthBoxConfig = new HealthBoxConfig("");
                                    var healthBox = new HealthBox(__instance, healthBoxConfig, 0, new Vectrics.Vector2D(0, 0), 0);
                                    var healthBoxTraverse = Traverse.Create(healthBox);
                                    healthBoxTraverse.Method("Pickup").GetValue(unit.Squad);
                                    logger.LogDebug($"Reinforced squad: {unit.Squad}");
                                }
                                catch (Exception e)
                                {
                                    logger.LogWarning($"ComputeTick(): 60 second interval encountered an error: {e.Message}");
                                }
                            }
                        }
                    }
                }
            }
        }

        class Patches_ResourceSystem
        {
            //[HarmonyPatch(typeof(ResourceSystem), "GetResourceIncomeAmount")]
            //class Patch_GetResourceIncomeAmount
            //{
            //    static void Postfix(ResourceSystem __instance, byte faction, ResourceType type, ref float __result)
            //    {
            //        if (faction == __instance.Battle.LocalPlayerFactionId && (type == ResourceType.Iron || type == ResourceType.Oil))
            //        {
            //            logger.LogDebug($"GetResourceIncomeAmount for faction {faction} for resource {type}: {__result}");
            //            __result *= 2;
            //        }
            //    }
            //}

            //[HarmonyPatch(typeof(ResourceSystem), "GetResourceProductionAmount")]
            //class Patch_GetResourceProductionAmount
            //{
            //    static void Postfix(ResourceSystem __instance, byte faction, ResourceType type, ref float __result)
            //    {
            //        if (faction == __instance.Battle.LocalPlayerFactionId && (type == ResourceType.Iron || type == ResourceType.Oil))
            //        {
            //            logger.LogDebug($"Patch_GetResourceProductionAmount for faction {faction} for resource {type}: {__result}");
            //            __result *= 2;
            //        }
            //    }
            //}

            [HarmonyPatch(typeof(ResourceSystem), "GrantResource")]
            class Patch_GrantResource
            {
                static void Prefix(ResourceSystem __instance, byte faction, ResourceType type, ResourceChangeReason changeReason, ref int amount)
                {
                    if (faction == __instance.Battle.LocalPlayerFactionId &&
                        (type == ResourceType.Iron || type == ResourceType.Oil) &&
                        changeReason == ResourceChangeReason.Production)
                    {
                        //logger.LogDebug($"Patch_GrantResource for faction {faction} for resource {type}: {amount}");
                        amount *= 4;
                    }
                }
            }

            [HarmonyPatch(typeof(ResourceSystem), "CanPayResources", new Type[] { typeof(byte), typeof(ResourceCost) })]
            class Patch_GetResourceLimit
            {
                static bool Prefix(ResourceSystem __instance, byte faction, ref ResourceCost buildCost)
                {
                    if (faction == __instance.Battle.LocalPlayerFactionId)
                    {
                        Traverse.Create(buildCost).Property("PopulationCost").SetValue(0);
                    }
                    return true;
                }
            }
        }

        class Patches_ProductionStructure
        {
            [HarmonyPatch(typeof(ProductionStructure), "ProgressProduction")]
            class Patch_ProgressProduction
            {
                static void Prefix(ProductionStructure __instance)
                {
                    if (__instance.Faction == __instance.Battle.LocalPlayerFactionId)
                    {
                        if (__instance.BuildOrders.Count > 0)
                        {
                            BuildOrder buildOrder = __instance.BuildOrders.Peek();
                            var remainingBuildTimeField = Traverse.Create(buildOrder).Field("_remainingTimeTicks");
                            var remainingBuildTime = remainingBuildTimeField.GetValue<int>();
                            if (remainingBuildTime > 0)
                            {
                                remainingBuildTimeField.SetValue(1);
                            }
                        }
                    }
                }
            }
        }

        //class Patches_Unit
        //{
        //    [HarmonyPatch(typeof(Unit), "Plan")]
        //    class Patch_Plan
        //    {
        //        static Dictionary<int, > entityTicks;
        //        static void Postfix(Unit __instance)
        //        {
        //            if (__instance.Faction == __instance.Battle.LocalPlayerFactionId)
        //            {
        //                float healAmount = (__instance.MaxHealth * 0.01f) / TICKS_PER_SECOND;
        //                __instance.DamageModel.Heal(healAmount, __instance.EntityId);
        //            }
        //        }
        //    }
        //}
    }
}
