using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using GameObject = XRL.World.GameObject;

namespace CavesOfQudMod
{
    internal class Mod
        static void Log(string message)
        {
            Debug.Log($"CavesOfQudMod: {message}");
        }

        class Patches_Beguiling
        {
            [HarmonyPatch(typeof(Beguiling), "GetMaxTargets")]
            class Patch_GetMaxTargets
            {
                static void Postfix(Beguiling __instance, ref int __result)
                {
                    if (!__instance.ParentObject.IsPlayer())
                    {
                        return;
                    }
                    Log($"Beguiling.GetMaxTargets. Original result: {__result}");
                    __result *= 2;
                }
            }
        }

        class Patches_Persuasion_Proselytize
        {
            [HarmonyPatch(typeof(Persuasion_Proselytize), "GetMaxTargets")]
            class Patch_GetMaxTargets
            {
                static void Postfix(Persuasion_Proselytize __instance, ref int __result)
                {
                    if (!__instance.ParentObject.IsPlayer())
                    {
                        return;
                    }
                    Log($"Persuasion_Proselytize.GetMaxTargets. Original result: {__result}");
                    __result *= 2;
                }
            }
        }

        [HarmonyPatch(typeof(Brain))]
        class Patches_Brain
        {
            [HarmonyPostfix]
            [HarmonyPatch("IsSuitableTarget")]
            static void Postfix_IsSuitableTarget(Brain __instance, GameObject who, ref bool __result)
            {
                if (!__instance.IsPlayerLed())
                {
                    return;
                }
                Log($"Brain.IsSuitableTarget. Original result: {__result}. Target: {who.BaseDisplayNameStripped}.");
                if (!__result)
                {
                    Log($"Brain.IsSuitableTarget. Rejected target {who.BaseDisplayNameStripped} which is at distance {who.DistanceTo(who.Target)} and hostile walk radius {who.GetHostileWalkRadius(who.Target)} from its target {who.Target.BaseDisplayNameStripped}");
                }
            }
        }
    }
}
