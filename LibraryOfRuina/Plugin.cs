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

        static readonly float CARD_DICE_MIN_MULT = 1.5f;
        static readonly float CARD_DICE_MAX_MULT = 1f;
        static readonly float CARD_DICE_CHANGE_PROB = 0.8f;

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
                        logger.LogDebug($"Using original at {(int)(originalRollChance * 100)}% < {(int)((1 - CARD_DICE_CHANGE_PROB) * 100)}% chance");
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
                logger.LogDebug($"Equipped other book: {__instance.CurrentBookModel.Name}");
                return false;
            }
        }

        [HarmonyPatch(typeof(UIPassiveSuccessionBookSlot), "SetDisabledByTheBluePrimary")]
        public class Patch_BookModel_SetDisabledByTheBluePrimary
        {
            static bool Prefix(UIPassiveSuccessionBookSlot __instance)
            {
                logger.LogDebug($"Disabled by Blue Reverbation: {__instance.CurrentBookModel.Name}");
                return false;
            }
        }
    }
}
