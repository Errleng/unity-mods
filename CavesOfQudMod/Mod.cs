using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using XRL.Rules;
using XRL.World.Parts;
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

        //[HarmonyPatch(typeof(Beguiling))]
        //private class Patches_Beguiling
        //{
        //    [HarmonyPostfix]
        //    [HarmonyPatch("GetMaxTargets")]
        //    private static void Postfix_GetMaxTargets(GameObject Beguiler, ref int __result)
        //    {
        //        if (!Beguiler.IsPlayer())
        //        {
        //            return;
        //        }
        //        Log($"Beguiling.GetMaxTargets. Original result: {__result}");
        //        __result *= 2;
        //    }
        //}

        //[HarmonyPatch(typeof(Persuasion_Proselytize))]
        //private class Patches_Persuasion_Proselytize
        //{
        //    [HarmonyPostfix]
        //    [HarmonyPatch("GetMaxTargets")]
        //    private static void Postfix(GameObject Proselytizer, ref int __result)
        //    {
        //        if (!Proselytizer.IsPlayer())
        //        {
        //            return;
        //        }
        //        Log($"Persuasion_Proselytize.GetMaxTargets. Original result: {__result}");
        //        __result *= 2;
        //    }
        //}

        [HarmonyPatch(typeof(Brain))]
        private class Patches_Brain
        {
            [HarmonyPrefix]
            [HarmonyPatch("FindProspectiveTarget")]
            private static bool Prefix_FindProspectiveTarget(Brain __instance, ref GameObject __result)
            {
                if (!__instance.IsPlayerLed() || Stat.Chance(100 - 10))
                {
                    return true;
                }

                var zone = __instance.ParentObject.CurrentCell.ParentZone;

                var targets = new List<GameObject>();
                foreach (var gameObject in zone.GetObjects())
                {
                    if (gameObject.IsHostileTowards(__instance.PartyLeader) && __instance.ParentObject.canPathTo(gameObject.CurrentCell))
                    {
                        targets.Add(gameObject);
                    }
                }
                if (targets.Count > 1)
                {
                    targets.Sort(new Comparison<GameObject>(__instance.TargetSort));
                }
                if (targets.Count > 0)
                {
                    __result = targets[0];
                    if (__result.pRender == null)
                    {
                        Log($"{__result.BaseDisplayName} has a null pRender");
                    }
                    else
                    {
                        __result.pRender.Visible = true;
                    }
                }

                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch("Think")]
            private static void Postfix_Think(Brain __instance, string Hrm)
            {
                if (!__instance.IsPlayerLed())
                {
                    return;
                }
                Log($"{__instance.ParentObject.BaseDisplayName} thought: {Hrm}");
            }
        }

        //[HarmonyPatch(typeof(Bored))]
        //private class Patches_Bored
        //{
        //    [HarmonyPostfix]
        //    [HarmonyPatch("TakeActionWithPartyLeader")]
        //    private static void Postfix_TakeActionWithPartyLeader(Bored __instance)
        //    {
        //        if (!__instance.ParentBrain.IsPlayerLed() || !__instance.ParentObject.HasEffect<SummonedEffect>())
        //        {
        //            return;
        //        }

        //        if (__instance.ParentBrain.Target == null)
        //        {
        //            if (Stat.Chance(25))
        //            {
        //                __instance.PushChildGoal(new Wander());
        //            }
        //        }
        //    }
        //}

        [HarmonyPatch(typeof(GameObject))]
        private class Patches_GameObject
        {
            [HarmonyPrefix]
            [HarmonyPatch("TakeDamage",
                new Type[] { typeof(int), typeof(string), typeof(string), typeof(string), typeof(GameObject), typeof(GameObject), typeof(GameObject), typeof(GameObject), typeof(GameObject), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(int), typeof(string) },
                new ArgumentType[] { ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
            private static void Prefix_TakeDamage(ref int Amount, GameObject Owner = null, GameObject Attacker = null)
            {
                if (Attacker != null && Attacker.IsPlayerLed())
                {
                    Log($"Attacker is player led: {Attacker.IsPlayerLed()}");
                }
                if (Owner != null && Owner.IsPlayer() && Attacker != null && Attacker.IsPlayerLed())
                {
                    Log($"Negated {Amount} damage from {Attacker.BaseDisplayName} to {Owner.BaseDisplayName}");
                    Amount = 0;
                }
            }
        }
    }
}
