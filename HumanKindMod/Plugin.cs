using BepInEx;
using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Amplitude.Mercury.Simulation;
using BepInEx.Logging;
using Amplitude;
using Amplitude.Mercury.Data.Simulation;

[assembly: IgnoresAccessChecksTo("Amplitude.Mercury.Firstpass")]
namespace HumankindMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static ManualLogSource log;
        private void Awake()
        {
            // Plugin startup logic
            log = Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is done patching!");
        }


        [HarmonyPatch(typeof(DepartmentOfIndustry), "GetConstructibleProductionCostForSettlement")]
        public class Patch_DepartmentOfIndustry_GetConstructibleProductionCostForSettlement
        {
            //static MethodBase TargetMethod()
            //{
            //    // use reflections (with or without AccessTools) to return the MethodInfo of the original method
            //    var type = AccessTools.TypeByName("Amplitude.Mercury.Simulation.DepartmentOfIndustry");
            //    return AccessTools.Method(type, "GetConstructibleProductionCostForSettlement");
            //}

            static void Postfix(DepartmentOfIndustry __instance, ConstructibleDefinition constructibleDefinition, ref FixedPoint __result)
            {
                if (__instance.Empire.IsControlledByHuman)
                {
                    //log.LogInfo($"Empire: {__instance.Empire.GetDebugName()}, Construction: {constructibleDefinition.Name}, cost: {__result}");
                    __result /= 2;
                }
            }
        }

        //[HarmonyPatch]
        //public class Patch_DepartmentOfIndustry_AddConstruction
        //{
        //    static MethodBase TargetMethod()
        //    {
        //        // use reflections (with or without AccessTools) to return the MethodInfo of the original method
        //        var type = AccessTools.TypeByName("Amplitude.Mercury.Simulation.DepartmentOfIndustry");
        //        return AccessTools.Method(type, "AddConstruction");
        //    }

        //    static void Postfix(DepartmentOfIndustry __instance, ref ConstructionQueue queue)
        //    {
        //        if (queue.Constructions.Count == 0)
        //        {
        //            return;
        //        }
        //        var construction = queue.Constructions[queue.Constructions.Count - 1];
        //        if (__instance.Empire.IsControlledByHuman)
        //        {
        //            construction.Cost = 1;
        //        }
        //        Debug.Log($"Construction added: {construction}, cost: {construction.Cost}");
        //    }
        //}

        //[HarmonyPatch]
        //public class Patch_DepartmentOfCulture_GainInfluence
        //{
        //    static MethodBase TargetMethod()
        //    {
        //        // use reflections (with or without AccessTools) to return the MethodInfo of the original method
        //        var type = AccessTools.TypeByName("Amplitude.Mercury.Simulation.DepartmentOfCulture");
        //        return AccessTools.Method(type, "GainInfluence");
        //    }

        //    static void Prefix()
        //    {
        //        Debug.Log($"Gain influence");
        //    }
        //}
    }
}
