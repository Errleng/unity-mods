using HarmonyLib;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using XRL.World.Effects;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using GameObject = XRL.World.GameObject;

namespace CavesOfQudMod
{
    internal class Mod
    {
        public static void Log(string message)
        {
            UnityEngine.Debug.Log($"CavesOfQudMod - {message}");
        }

        public static void Debug(string message, [CallerMemberName] string callerMethod = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerFileLineNum = 0)
        {
            var callerFileName = Path.GetFileName(callerFilePath);
            UnityEngine.Debug.Log($"CavesOfQudMod.{callerFileName}.{callerMethod}:{callerFileLineNum} - {message}");
        }

        [HarmonyPatch(typeof(Beguiling))]
        private class Patches_Beguiling
        {
            [HarmonyPostfix]
            [HarmonyPatch("GetMaxTargets")]
            private static void Postfix_GetMaxTargets(GameObject Beguiler, ref int __result)
            {
                if (!Beguiler.IsPlayer())
                {
                    return;
                }
                Log($"Beguiling.GetMaxTargets. Original result: {__result}");
                __result *= 2;
            }
        }

        [HarmonyPatch(typeof(Persuasion_Proselytize))]
        private class Patches_Persuasion_Proselytize
        {
            [HarmonyPostfix]
            [HarmonyPatch("GetMaxTargets")]
            private static void Postfix(GameObject Proselytizer, ref int __result)
            {
                if (!Proselytizer.IsPlayer())
                {
                    return;
                }
                Log($"Persuasion_Proselytize.GetMaxTargets. Original result: {__result}");
                __result *= 2;
            }
        }

        [HarmonyPatch(typeof(Brain))]
        private class Patches_Brain
        {
            [HarmonyPostfix]
            [HarmonyPatch("IsSuitableTarget")]
            private static void Postfix_IsSuitableTarget(Brain __instance, GameObject who, ref bool __result)
            {
                if (__result || !__instance.IsPlayerLed() || who.HasEffect<Asleep>() || !who.IsHostileTowards(__instance.PartyLeader) || !__instance.CheckPerceptionOf(who))
                {
                    return;
                }

                var parent = __instance.ParentObject;
                if (__instance.PartyLeader.DistanceTo(who) <= 10)
                {
                    __result = true;
                }
            }
        }
    }
}
