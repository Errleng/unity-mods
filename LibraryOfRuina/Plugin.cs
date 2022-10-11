using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UI;
using UnityEngine;
using static BattleUnitInformationUI_PassiveList;

namespace LibraryOfRuina
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static ManualLogSource logger;

        static readonly float CARD_DICE_MIN_MULT = 5f;
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
                        //logger.LogDebug($"Using original speed dice roll at {Math.Round(1 - originalRollChance, 2) * 100} < {Math.Round(1 - SPEED_DICE_CHANGE_PROB, 2) * 100} chance");
                        return;
                    }
                    foreach (var speedDice in __result)
                    {
                        int newMin = Math.Min((int)Math.Ceiling(speedDice.min * SPEED_DICE_MIN_MULT), speedDice.faces);
                        speedDice.value = UnityEngine.Random.Range(newMin, speedDice.faces + 1);
                        logger.LogDebug($"{unitModel.UnitData.unitData.name} speed: ({speedDice.min}, {newMin}) and ({speedDice.faces})");
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
                        logger.LogDebug($"{__instance.card.card.GetName()} dice: {__instance.GetDiceMin()}, {__instance.GetDiceMax()}. {Math.Round(CARD_DICE_CHANGE_PROB, 2) * 100} < {Math.Round(originalRollChance) * 100}");
                        return true;
                    }
                    int diceMin = (int)Math.Ceiling(__instance.GetDiceMin() * CARD_DICE_MIN_MULT);
                    int diceMax = __instance.GetDiceMax();
                    //diceMax *= CARD_DICE_MAX_MULT;
                    diceMin = Math.Min(diceMin, diceMax - 1); // leave some room for error

                    logger.LogDebug($"{__instance.card.card.GetName()} dice: ({__instance.GetDiceMin()}, {diceMin}) and ({__instance.GetDiceMax()}, {diceMax})");

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

        [HarmonyPatch(typeof(BattleUnitEmotionDetail), "CreateEmotionCoin")]
        public class Patch_BattleUnitEmotionDetail_CreateEmotionCoin
        {
            static void Postfix(EmotionCoinType coinType, ref int __result, BattleUnitModel ____self)
            {
                if (____self.faction == Faction.Player && __result > 0)
                {
                    logger.LogDebug($"Emotion coin for {____self.UnitData.unitData.name}: {__result} {coinType}");
                    __result *= 2;
                }
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

        [HarmonyPatch(typeof(BookModel), "GetMaxPassiveCost")]
        public class Patch_BookModel_GetMaxPassiveCost
        {
            static void Postfix(ref int __result)
            {
                __result = 200;
            }
        }

        [HarmonyPatch(typeof(LibraryModel), "LoadFromSaveData")]
        public class Patch_LibraryModel_LoadFromSaveData
        {
            static void Prefix()
            {
                foreach (var bookXml in Singleton<BookXmlList>.Instance.GetList())
                {
                    bookXml.SuccessionPossibleNumber = 100;
                }
            }
        }

        [HarmonyPatch(typeof(BookModel), "IsNotFullEquipPassiveBook")]
        public class Patch_BookModel_IsNotFullEquipPassiveBook
        {
            static void Postfix(ref bool __result)
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(BookModel), "UnEquipGivePassiveBook")]
        public class Patch_UnEquipGivePassiveBook
        {
            static bool Prefix(BookModel __instance, BookModel unequipbook, bool origin = false)
            {
                try
                {
                    BookModel.BookEquipedBookSavedData bookEquipedBookSavedData = (origin ? __instance.originData : __instance.reservedData);
                    bookEquipedBookSavedData.equipedBookIdListInPassive.Remove(unequipbook.instanceId);
                    if (origin)
                    {
                        unequipbook.originData.equipedPassiveBookInstanceId = -1;
                    }
                    else
                    {
                        unequipbook.reservedData.equipedPassiveBookInstanceId = -1;
                    }
                    return false;
                }
                catch (Exception e)
                {
                    logger.LogDebug($"BookModel.UnEquipGivePassiveBook error: {e.Message}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(BookModel), "ApplyPassiveSuccession")]
        public class Patch_BookModel_ApplyPassiveSuccession
        {
            static bool Prefix(BookModel __instance, List<PassiveModel> ____activatedAllPassives)
            {
                var originData = __instance.originData;
                var reservedData = __instance.reservedData;
                originData.equipedBookIdListInPassive.Clear();
                originData.equipedBookIdListInPassive.AddRange(reservedData.equipedBookIdListInPassive);
                originData.equipedPassiveBookInstanceId = reservedData.equipedPassiveBookInstanceId;
                //if (originData.equipedPassiveBookInstanceId != -1 && !IsEmptyDeckAll())
                //{
                //    EmptyDeckToInventoryAll();
                //}
                foreach (PassiveModel activatedAllPassife in ____activatedAllPassives)
                {
                    activatedAllPassife.ApplyReserved();
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(BattleUnitInformationUI_PassiveList), "SetData")]
        public class Patch_BattleUnitInformationUI_PassiveList_SetData
        {
            static bool Prefix(BattleUnitInformationUI_PassiveList __instance, List<PassiveAbilityBase> passivelist, List<BattleUnitInformationPassiveSlot> ___passiveSlotList, float ___passivespacing, float ___minimumheight, RectTransform ___rect_passiveListPanel)
            {
                var passiveSlotList = ___passiveSlotList;
                var passivespacing = ___passivespacing;
                var minimumheight = ___minimumheight;
                var rect_passiveListPanel = ___rect_passiveListPanel;

                foreach (BattleUnitInformationPassiveSlot passiveSlot in passiveSlotList)
                {
                    if (passiveSlot.Rect.gameObject.activeSelf)
                    {
                        passiveSlot.Rect.gameObject.SetActive(value: false);
                    }
                }
                int num = 0;
                int num2 = 0;
                float num3 = 0f;
                while (num2 < passivelist.Count)
                {
                    if (num2 > passiveSlotList.Count)
                    {
                        return false;
                    }
                    if (passivelist[num2].isHide)
                    {
                        num2++;
                        continue;
                    }
                    if (num >= passiveSlotList.Count)
                    {
                        //logger.LogDebug($"BattleUnitInformationUI_PassiveList.SetData. Index {num} > {passiveSlotList.Count} passive slots and {passivelist.Count} passives");
                        break;
                    }
                    passiveSlotList[num].SetData(passivelist[num2]);
                    passiveSlotList[num].Rect.gameObject.SetActive(value: true);
                    num3 += passiveSlotList[num].Rect.sizeDelta.y * 0.2f + passivespacing;
                    num++;
                    num2++;
                }
                if (num2 == 0)
                {
                    __instance.SetActive(b: false);
                    return false;
                }
                __instance.SetActive(b: true);
                num3 = ((num3 < minimumheight) ? minimumheight : (num3 + 100f));
                Vector2 sizeDelta = rect_passiveListPanel.sizeDelta;
                sizeDelta.y = num3;
                rect_passiveListPanel.sizeDelta = sizeDelta;
                rect_passiveListPanel.anchoredPosition = Vector3.zero;
                return false;
            }
        }

        [HarmonyPatch(typeof(UIPassiveSuccessionPopup), "ChangePassive")]
        public class Patch_UIPassiveSuccessionPopup_ChangePassive
        {
            static bool Prefix(UIPassiveSuccessionPopup __instance, UIPassiveSuccessionCenterPassiveSlot selectedcenterSlot, UIPassiveSuccessionList ___equipPassiveList, BookModel ____currentBookModel, UIPassiveSuccessionSlot ___equipAnimSlot, UIPassiveSuccessionCenterPanel ___centerBookListPanel)
            {
                var equipPassiveList = ___equipPassiveList;
                var _currentBookModel = ____currentBookModel;
                var equipAnimSlot = ___equipAnimSlot;
                var centerBookListPanel = ___centerBookListPanel;
                UIPassiveSuccessionSlot uIPassiveSuccessionSlot = equipPassiveList.slotlist.Find((UIPassiveSuccessionSlot x) => !x.isEmpty && x.passivemodel != null && x.passivemodel.reservedData.currentpassive.id == 9999999);

                logger.LogDebug($"EquipPassiveList size: {equipPassiveList.slotlist.Count}");
                foreach (var passiveSlot in equipPassiveList.slotlist)
                {
                    logger.LogDebug($"{passiveSlot.isEmpty}, {passiveSlot.passivemodel}, {passiveSlot.passivemodel?.reservedData.currentpassive.id}");
                }

                //if (selectedcenterSlot == null)
                //{
                //    Debug.LogError("중앙에 선택한 슬롯이 존재하지 않습니다.");
                //    return false;
                //}
                //if (uIPassiveSuccessionSlot == null)
                //{
                //    Debug.LogError("빈 슬롯이 존재하지 않습니다");
                //    selectedcenterSlot.StartVibe();
                //    return false;
                //}
                if (uIPassiveSuccessionSlot == null)
                {
                    var newSlot = UnityEngine.Object.Instantiate(equipPassiveList.slotlist[0]);
                    newSlot.isEmpty = false;
                    equipPassiveList.slotlist.Add(newSlot);
                    uIPassiveSuccessionSlot = newSlot;
                }

                PassiveModel passivemodel = selectedcenterSlot.Passivemodel;
                if (passivemodel == null)
                {
                    return false;
                }
                PassiveModel passivemodel2 = uIPassiveSuccessionSlot.passivemodel;
                //if (!_currentBookModel.CanSuccessionPassiveByCost(passivemodel2, passivemodel))
                //{
                //    selectedcenterSlot.StartVibe();
                //    StartCostFullAlarm();
                //    return false;
                //}
                equipAnimSlot.SetAnimationSlot(passivemodel2, passivemodel);
                if (passivemodel2.IsReceivedSuccessionPassive)
                {
                    _currentBookModel.ReleasePassive(passivemodel2);
                }
                _currentBookModel.ChangePassive(passivemodel2, passivemodel);
                equipPassiveList.SetEquipModelData(_currentBookModel.GetPassiveModelList());
                centerBookListPanel.SetBooksData(_currentBookModel.GetEquipedBookList());
                equipAnimSlot.transform.SetParent(uIPassiveSuccessionSlot.transform);
                equipAnimSlot.transform.position = uIPassiveSuccessionSlot.transform.position;
                equipAnimSlot.gameObject.SetActive(value: true);
                equipAnimSlot.anim.SetTrigger("ChangeReveal");
                UISoundManager.instance.PlayEffectSound(UISoundType.Ui_Burn);

                var traverse = Traverse.Create(__instance);
                traverse.Method("SetCostData").GetValue();
                selectedcenterSlot.StartUsingAnimation();
                selectedcenterSlot.OnPointerEnter(null);
                return false;
            }
        }

        //[HarmonyPatch(typeof(UIPassiveSuccessionCenterPanel), "Init")]
        //public class Patch_UIPassiveSuccessionCenterPanel_Init
        //{
        //    static void Postfix(ref List<UIPassiveSuccessionCenterEquipBookSlot> ___equipBookSlotList, RectTransform ___rect_bookSlotsLayout)
        //    {
        //        UIPassiveSuccessionCenterEquipBookSlot[] componentsInChildren = ___rect_bookSlotsLayout.GetComponentsInChildren<UIPassiveSuccessionCenterEquipBookSlot>(includeInactive: true);
        //        logger.LogDebug($"{___equipBookSlotList.Count} attributed books, {componentsInChildren.Length}");
        //        foreach (UIPassiveSuccessionCenterEquipBookSlot item in componentsInChildren)
        //        {
        //            logger.LogDebug($"equip slot {item.GetHashCode()} {item.CurrentBookModel?.Name} {item.transform.position} {item.rect.sizeDelta}");
        //            var clone = UnityEngine.Object.Instantiate(item);
        //            clone.Init();
        //            logger.LogDebug($"clone {clone.GetHashCode()} {clone.CurrentBookModel?.Name} {clone.transform.position} {clone.rect.sizeDelta}");
        //            ___equipBookSlotList.Add(clone);
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(UIPassiveSuccessionCenterPanel), "SetScrollSize")]
        //public class Patch_UIPassiveSuccessionCenterPanel_SetScrollSize
        //{
        //    static bool Prefix(ref RectTransform ___rect_ScrollContent, List<UIPassiveSuccessionCenterEquipBookSlot> ___equipBookSlotList)
        //    {
        //        logger.LogDebug($"{___equipBookSlotList.Count} attributed books, {___rect_ScrollContent.sizeDelta}");
        //        foreach (var keypage in ___equipBookSlotList)
        //        {
        //            logger.LogDebug($"{keypage.GetHashCode()} {keypage.CurrentBookModel?.Name} {keypage.transform.position} {keypage.rect.sizeDelta}");
        //        }
        //        return true;
        //        float num = 0f
        //        foreach (UIPassiveSuccessionCenterEquipBookSlot equipBookSlot in ___equipBookSlotList)
        //        {
        //            if (equipBookSlot.gameObject.activeSelf)
        //            {
        //                num += equipBookSlot.rect.sizeDelta.y;
        //                num += 10f;
        //            }
        //        }
        //        ___rect_ScrollContent.sizeDelta = new Vector2(___rect_ScrollContent.sizeDelta.x, num);
        //        ___rect_ScrollContent.sizeDelta = new Vector2(___rect_ScrollContent.sizeDelta.x, ___rect_ScrollContent.sizeDelta.y * 2);
        //        return false;
        //    }
        //}
    }
}
