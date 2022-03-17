using HarmonyLib;
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
            Debug.Log($"CavesOfQudMod: {message}");
        }

        [HarmonyPatch(typeof(Beguiling))]
        class Patches_Beguiling
        {
            [HarmonyPostfix]
            [HarmonyPatch("GetMaxTargets")]
            static void Postfix_GetMaxTargets(GameObject Beguiler, ref int __result)
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
        class Patches_Persuasion_Proselytize
        {
            [HarmonyPostfix]
            [HarmonyPatch("GetMaxTargets")]
            static void Postfix(GameObject Proselytizer, ref int __result)
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
        class Patches_Brain
        {
            [HarmonyPostfix]
            [HarmonyPatch("IsSuitableTarget")]
            static void Postfix_IsSuitableTarget(Brain __instance, GameObject who, ref bool __result)
            {
                if (__result || !__instance.IsPlayerLed() || who.HasEffect<Asleep>() || !who.IsHostileTowards(__instance.PartyLeader) || !__instance.CheckPerceptionOf(who))
                {
                    return;
                }

                var parent = __instance.ParentObject;
                Log($"Brain.IsSuitableTarget. Original result: {__result}. Targeter: {parent.BaseDisplayNameStripped}. Target: {who.BaseDisplayNameStripped}. Distance: {parent.DistanceTo(who)}. Hostile walk radius to target: {parent.GetHostileWalkRadius(who)}, to this: {who.GetHostileWalkRadius(parent)}, party leader to target: {__instance.PartyLeader.GetHostileWalkRadius(who)}");

                if (__instance.PartyLeader.DistanceTo(who) <= 5)
                {
                    __result = true;
                }
            }
        }
    }
}
