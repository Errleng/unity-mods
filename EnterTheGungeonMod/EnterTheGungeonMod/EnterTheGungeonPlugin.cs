using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace EnterTheGungeonMod
{
    [BepInPlugin("org.bepinex.plugins.enterthegungeonplugin", "Enter The Gungeon Plugin", "1.0.0.0")]
    public class EnterTheGungeonPlugin : BaseUnityPlugin
    {
        static ManualLogSource logger;

        private void Awake()
        {
            logger = Logger;
            logger.LogInfo("Enter The Gungeon plugin loaded!");
            Harmony.CreateAndPatchAll(typeof(Patch));
        }

        public class Patch
        {
            static bool infiniteKeys = false;
            static bool currencyIncreaseOnly = false;
            static bool fixSemiAutoFireRate = true;
            static bool scouterHealthBars = false;
            static float currencyIncreaseMult = 1.5f;
            static float creditIncreaseMult = 2f;
            static int maxMagnificence = 1;
            static float initialCoolness = 10;
            static float chestChanceMult = 1f;
            static float reloadTimeMult = 1f;
            static float spreadMult = 0.75f;
            static GUI gui = new GUI();

            private static Dictionary<string, bool> firstRun = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "InfiniteKeys", true },
                { "Currency", true },
                { "ControllerFakeSemiAutoCooldown", true },
                { "Start", true },
                { "DetermineCurrentMagnificence", true },
                { "GetTargetQualityFromChances", true },
                { "RegisterStatChange", true },
                { "Gun.Update", true }
            };

            [HarmonyPatch(typeof(PlayerConsumables), "InfiniteKeys", MethodType.Setter)]
            [HarmonyPostfix]
            static void Postfix(ref bool ___m_infiniteKeys)
            {
                if (firstRun["InfiniteKeys"])
                {
                    logger.LogInfo("Loaded ETGPlugin PlayerConsumables InfiniteKeys setter");
                    firstRun["InfiniteKeys"] = false;
                }

                ___m_infiniteKeys = infiniteKeys;
            }


            [HarmonyPatch(typeof(PlayerConsumables), "Currency", MethodType.Setter)]
            [HarmonyPrefix]
            static bool Prefix(ref int value, int ___m_currency)
            {
                if (firstRun["Currency"])
                {
                    logger.LogInfo("Loaded ETGPlugin PlayerConsumables Currency setter");
                    firstRun["Currency"] = false;
                }

                // only allow increases
                if (currencyIncreaseOnly)
                {
                    if (value > ___m_currency)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (value > ___m_currency)
                {
                    // currency Mult
                    value = (int)(___m_currency + currencyIncreaseMult * (value - ___m_currency));
                }
                return true;
            }

            [HarmonyPatch(typeof(BraveInput), "ControllerFakeSemiAutoCooldown", MethodType.Getter)]
            [HarmonyPostfix]
            static void ControllerFakeSemiAutoCooldownPostfix(ref float __result)
            {
                if (firstRun["ControllerFakeSemiAutoCooldown"])
                {
                    logger.LogInfo("Loaded ETGPlugin BraveInput ControllerFakeSemiAutoCooldown getter");
                    firstRun["ControllerFakeSemiAutoCooldown"] = false;
                }

                if (fixSemiAutoFireRate)
                {
                    // uses player's current gun's cooldown
                    Gun currentGun = GameManager.Instance.PrimaryPlayer.CurrentGun;
                    __result = currentGun.GetPrimaryCooldown();
                }
            }


            [HarmonyPatch(typeof(PlayerController), "Start")]
            [HarmonyPrefix]
            static void Prefix(PlayerController __instance)
            {
                if (firstRun["Start"])
                {
                    logger.LogInfo("Loaded ETGPlugin PlayerController Start()");
                    firstRun["Start"] = false;
                }

                if (infiniteKeys)
                {
                    __instance.carriedConsumables.InfiniteKeys = true;
                }

                if (scouterHealthBars)
                {
                    gui.ToggleHealthBars(__instance, true);
                }

                PlayerStats stats = __instance.stats;
                stats.SetBaseStatValue(PlayerStats.StatType.Coolness, initialCoolness, __instance);
                // lower is better
                stats.SetBaseStatValue(PlayerStats.StatType.ReloadSpeed, reloadTimeMult * stats.GetBaseStatValue(PlayerStats.StatType.ReloadSpeed), __instance);
                stats.SetBaseStatValue(PlayerStats.StatType.Accuracy, spreadMult * stats.GetBaseStatValue(PlayerStats.StatType.Accuracy), __instance);
            }


            [HarmonyPatch(typeof(FloorRewardData), "DetermineCurrentMagnificence")]
            [HarmonyPrefix]
            static bool Prefix(ref float __result)
            {
                if (firstRun["DetermineCurrentMagnificence"])
                {
                    logger.LogInfo("Loaded ETGPlugin FloorRewardData DetermineCurrentMagnificence()");
                    firstRun["DetermineCurrentMagnificence"] = false;
                }

                // Magnificence table
                //0  0%
                //1  79.84%
                //2  95.53%
                //3  98.62%
                //4  99.23%
                //5  99.34% 

                // Set magnificence to 0 to always have the same chance of getting A or S tier loot
                if (maxMagnificence > 0)
                {
                    __result = Math.Min(maxMagnificence, __result);
                    return false;
                }
                return true;
            }


            [HarmonyPatch(typeof(FloorRewardData), "GetTargetQualityFromChances")]
            [HarmonyPrefix]
            static void Prefix(ref float fran, ref float dChance, ref float cChance, ref float bChance, ref float aChance, ref float sChance)
            {
                if (firstRun["GetTargetQualityFromChances"])
                {
                    logger.LogInfo("Loaded ETGPlugin FloorRewardData GetTargetQualityFromChances()");
                    firstRun["GetTargetQualityFromChances"] = false;
                }

                // More chance for higher quality
                fran = Math.Min(1, fran * chestChanceMult);
            }

            [HarmonyPatch(typeof(GameStatsManager), "RegisterStatChange")]
            [HarmonyPrefix]
            static void Prefix(GameStatsManager __instance, TrackedStats stat, ref float value)
            {
                if (firstRun["RegisterStatChange"])
                {
                    logger.LogInfo("Loaded ETGPlugin GameStatsManager RegisterStatChange()");
                    firstRun["RegisterStatChange"] = false;
                }

                if (stat.Equals(TrackedStats.META_CURRENCY))
                {
                    float currentHegemonyCredits = __instance.GetPlayerStatValue(TrackedStats.META_CURRENCY);
                    if (value > currentHegemonyCredits)
                    {
                        // Hegemony Credit increase
                        logger.LogInfo($"{currentHegemonyCredits} + {value - currentHegemonyCredits} * {creditIncreaseMult} = {(int)Math.Round(currentHegemonyCredits + creditIncreaseMult * (value - currentHegemonyCredits))}");
                        value = (int)(currentHegemonyCredits + creditIncreaseMult * (value - currentHegemonyCredits));
                    }
                }
            }

            [HarmonyPatch(typeof(Gun), "Update")]
            [HarmonyPostfix]
            static void Postfix(Gun __instance)
            {
                if (firstRun["Gun.Update"])
                {
                    logger.LogInfo("Loaded ETGPlugin Gun Update()");
                    firstRun["Gun.Update"] = false;
                }

                // gun automatically reloads when clip empty
                if (__instance.ClipShotsRemaining == 0)
                {
                    __instance.Reload();
                }
            }
        }
    }
}
