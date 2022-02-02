using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UI;
using UnityEngine;

namespace LibraryOfRuina
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static ManualLogSource logger;

        static readonly float CARD_DICE_MIN_MULT = 1.5f;
        static readonly float CARD_DICE_MAX_MULT = 1f;
        static readonly float CARD_DICE_CHANGE_PROB = 0.9f;
        static readonly float SPEED_DICE_MIN_MULT = 1.5f;
        static readonly float SPEED_DICE_CHANGE_PROB = 0.9f;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            logger = Logger;
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            foreach (var method in harmony.GetPatchedMethods())
            {
                Logger.LogInfo($"Patched method {method.DeclaringType.Name}.{method.Name}");
            }
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is done patching!");
        }

        [HarmonyPatch(typeof(SpeedDiceRule), "Roll")]
        public class Patch_SpeedDiceRule_Roll
        {
            static void Postfix(BattleUnitModel unitModel, ref List<SpeedDice> __result)
            {
                if (unitModel.faction == Faction.Player)
                {
                    var originalRollChance = RandomUtil.valueForProb;
                    if (originalRollChance > SPEED_DICE_CHANGE_PROB)
                    {
                        logger.LogDebug($"Using original speed dice roll at {Math.Round(1 - originalRollChance, 2) * 100} < {Math.Round(1 - SPEED_DICE_CHANGE_PROB, 2) * 100} chance");
                        return;
                    }
                    foreach (var speedDice in __result)
                    {
                        int newMin = Math.Min((int)Math.Ceiling(speedDice.min * SPEED_DICE_MIN_MULT), speedDice.faces);
                        speedDice.value = UnityEngine.Random.Range(newMin, speedDice.faces + 1);
                        logger.LogDebug($"{unitModel.UnitData.unitData.name} speed dice min: ({speedDice.min} => {newMin}) / ({speedDice.faces})");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BattleDiceBehavior), "RollDice")]
        public class Patch_BattleDiceBehavior_RollDice
        {
            static bool Prefix(BattleDiceBehavior __instance, ref int ____diceResultValue)
            {
                if (__instance.owner.faction == Faction.Player)
                {
                    var originalRollChance = RandomUtil.valueForProb;
                    if (originalRollChance > CARD_DICE_CHANGE_PROB)
                    {
                        //logger.LogDebug($"Using original dice roll at {Math.Round(1 - CARD_DICE_CHANGE_PROB, 2) * 100} < {Math.Round(1 - CARD_DICE_CHANGE_PROB, 2) * 100} chance");
                        return true;
                    }
                    int diceMin = (int)Math.Ceiling(__instance.GetDiceMin() * CARD_DICE_MIN_MULT);
                    //int diceMax = (int)Math.Ceiling(__instance.GetDiceMax() * CARD_DICE_MAX_MULT);
                    int diceMax = __instance.GetDiceMax();
                    diceMin = Math.Min(diceMin, diceMax);

                    logger.LogDebug($"{__instance.card.card.GetName()} dice min: ({__instance.GetDiceMin()} => {diceMin}) / ({__instance.GetDiceMax()} => {diceMax})");

                    int trueMin = Mathf.Min(diceMin, diceMax);
                    ____diceResultValue = DiceStatCalculator.MakeDiceResult(diceMin, diceMax, 0);
                    __instance.owner.passiveDetail.ChangeDiceResult(__instance, ref ____diceResultValue);
                    __instance.owner.emotionDetail.ChangeDiceResult(__instance, ref ____diceResultValue);
                    __instance.owner.bufListDetail.ChangeDiceResult(__instance, ref ____diceResultValue);
                    if (____diceResultValue < trueMin)
                    {
                        ____diceResultValue = trueMin;
                    }
                    int numPosCoins = 0;
                    int numNegCoins = 0;
                    if (trueMin != diceMax)
                    {
                        if (____diceResultValue >= diceMax)
                        {
                            numPosCoins++;
                        }
                        else if (____diceResultValue <= trueMin)
                        {
                            numNegCoins++;
                        }
                    }
                    if (____diceResultValue < 1)
                    {
                        ____diceResultValue = 1;
                    }
                    BattleCardTotalResult battleCardResultLog = __instance.owner.battleCardResultLog;
                    if (numPosCoins > 0)
                    {
                        int posEmotion = __instance.owner.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive, numPosCoins);
                        if (battleCardResultLog != null)
                        {
                            battleCardResultLog.AddEmotionCoin(EmotionCoinType.Positive, posEmotion);
                        }
                    }
                    else if (numNegCoins > 0)
                    {
                        int negEmotion = __instance.owner.emotionDetail.CreateEmotionCoin(EmotionCoinType.Negative, numNegCoins);
                        if (battleCardResultLog != null)
                        {
                            battleCardResultLog.AddEmotionCoin(EmotionCoinType.Negative, negEmotion);
                        }
                    }
                    __instance.card.OnRollDice(__instance);
                    __instance.OnEventDiceAbility(DiceCardAbilityBase.DiceCardPassiveType.RollDice, null);
                    __instance.isUsed = true;
                    return false;
                }
                return true;
            }
        }

        //[HarmonyPatch(typeof(BookModel), "CanSuccessionPassive")]
        //public class Patch_BookModel_CanSuccessionPassive
        //{
        //    static void Postfix(ref bool __result, PassiveModel targetpassive, GivePassiveState haspassiveState)
        //    {
        //        BookPassiveInfo bookPassiveInfo = new BookPassiveInfo();
        //        bookPassiveInfo.passive = Singleton<PassiveXmlList>.Instance.GetData(targetpassive.originpassive.id);
        //        logger.LogDebug($"Can attribute {bookPassiveInfo.name}: {__result} because {haspassiveState}");
        //    }
        //}

        [HarmonyPatch(typeof(UIPassiveSuccessionBookSlot), "SetEquipedOtherBook")]
        public class Patch_BookModel_SetEquipedOtherBook
        {
            static bool Prefix(UIPassiveSuccessionBookSlot __instance)
            {
                //logger.LogDebug($"Equipped other book: {__instance.CurrentBookModel.Name}");
                return false;
            }
        }

        [HarmonyPatch(typeof(UIPassiveSuccessionBookSlot), "SetDisabledByTheBluePrimary")]
        public class Patch_BookModel_SetDisabledByTheBluePrimary
        {
            static bool Prefix(UIPassiveSuccessionBookSlot __instance)
            {
                //logger.LogDebug($"Disabled by Blue Reverbation: {__instance.CurrentBookModel.Name}");
                return false;
            }
        }

        //[HarmonyPatch(typeof(BookModel), "IsEquipedPassiveBook")]
        //public class Patch_BookModel_IsEquipedPassiveBook
        //{
        //    static void Postfix(ref bool __result)
        //    {
        //        __result = false;
        //    }
        //}

        [HarmonyPatch(typeof(BookInventoryModel), "GetBookList_PassiveEquip")]
        public class Patch_BookInventoryModel_GetBookList_PassiveEquip
        {
            static void Postfix(BookModel booktobeEquiped, List<BookModel> ____bookList, ref List<BookModel> __result)
            {
                List<BookModel> result = new List<BookModel>();
                List<BookModel> inventory = new List<BookModel>();
                inventory.AddRange(____bookList);
                if (inventory.Contains(booktobeEquiped))
                {
                    inventory.Remove(booktobeEquiped);
                }
                foreach (BookModel bookModel in inventory)
                {
                    if (/*bookModel.owner == null && !bookModel.IsEquipedPassiveBook() && */bookModel.GetPassiveInfoList(true).Count != 0)
                    {
                        result.Add(bookModel);
                    }
                }
                __result = result;
            }
        }

        //[HarmonyPatch(typeof(UIPassiveSuccessionBookListPanel), "ApplyFilter")]
        //public class Patch_UIPassiveSuccessionBookListPanel_ApplyFilter
        //{
        //    static void Postfix(List<BookModel> ____currentBookList, List<BookModel> ____originBookList)
        //    {
        //        logger.LogDebug($"UIPassiveSuccessionBookListPanel.ApplyFilter");
        //        foreach (var book in ____currentBookList)
        //        {
        //            logger.LogDebug($"Current Key page: {book.Name}");
        //        }
        //        foreach (var book in ____originBookList)
        //        {
        //            logger.LogDebug($"Origin Key page: {book.Name}");
        //        }
        //    }
        //}
    }
}
