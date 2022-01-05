using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UI;
using UnityEngine;

namespace LibraryOfRuina
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static ManualLogSource logger;

        static readonly float cardDiceMinMultiplier = 1.5f;
        static readonly float cardDiceMaxMultiplier = 1f;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            logger = Logger;
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is done patching!");
        }

        [HarmonyPatch(typeof(BattleDiceBehavior), "GetDiceMin")]
        public class Patch_BattleDiceBehavior_GetDiceMin
        {
            static void Postfix(BattleDiceBehavior __instance, ref int __result)
            {
                if (__instance.owner.faction == Faction.Player)
                {
                    int improvedResult = (int)Math.Ceiling(__result * cardDiceMinMultiplier);
                    improvedResult = Math.Min(improvedResult, __instance.GetDiceMax());
                    logger.LogInfo($"GetDiceMin patch {__instance.card.card.GetName()}: ({__result} => {improvedResult}) / {__instance.GetDiceMax()}");
                    __result = improvedResult;
                }
            }
        }

        [HarmonyPatch(typeof(BattleDiceBehavior), "GetDiceMax")]
        public class Patch_BattleDiceBehavior_GetDiceMax
        {
            static void Postfix(BattleDiceBehavior __instance, ref int __result)
            {
                if (__instance.owner.faction == Faction.Player)
                {
                    int improvedResult = (int)Math.Ceiling(__result * cardDiceMaxMultiplier);
                    __result = improvedResult;
                }
            }
        }

        [HarmonyPatch(typeof(DropBookInventoryModel), "RemoveBook")]
        public class Patch_DropBookInventoryModel_RemoveBook
        {
            static bool Prefix()
            {
                return false;
            }
        }

        //[HarmonyPatch(typeof(BookModel), "CanSuccessionPassive")]
        //public class Patch_BookModel_CanSuccessionPassive
        //{
        //    static void Postfix(ref bool __result, PassiveModel targetpassive, GivePassiveState haspassiveState)
        //    {
        //        BookPassiveInfo bookPassiveInfo = new BookPassiveInfo();
        //        bookPassiveInfo.passive = Singleton<PassiveXmlList>.Instance.GetData(targetpassive.originpassive.id);
        //        logger.LogInfo($"Can attribute {bookPassiveInfo.name}: {__result} because {haspassiveState}");
        //    }
        //}

        [HarmonyPatch(typeof(UIPassiveSuccessionBookSlot), "SetEquipedOtherBook")]
        public class Patch_BookModel_SetEquipedOtherBook
        {
            static bool Prefix(UIPassiveSuccessionBookSlot __instance)
            {
                logger.LogInfo($"Equipped other book: {__instance.CurrentBookModel.Name}");
                return false;
            }
        }

        [HarmonyPatch(typeof(UIPassiveSuccessionBookSlot), "SetDisabledByTheBluePrimary")]
        public class Patch_BookModel_SetDisabledByTheBluePrimary
        {
            static bool Prefix(UIPassiveSuccessionBookSlot __instance)
            {
                logger.LogInfo($"Disabled by Blue Reverbation: {__instance.CurrentBookModel.Name}");
                return false;
            }
        }
    }
}
