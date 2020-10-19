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
            static bool currencyIncreaseDouble = true;

            private static Dictionary<string, bool> firstRun = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "InfiniteKeys", true },
                { "Currency", true },
                { "Start", true }
            };

            [HarmonyPatch(typeof(PlayerConsumables), "InfiniteKeys", MethodType.Setter)]
            [HarmonyPostfix]
            static void Postfix(bool ___m_infiniteKeys)
            {
                if (firstRun["InfiniteKeys"])
                {
                    logger.LogInfo("Loaded ETGPlugin PlayerConsumables InfiniteKeys setter");
                    firstRun["InfiniteKeys"] = false;
                }

                if (infiniteKeys)
                {
                    ___m_infiniteKeys = true;
                }
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

                // double increases
                if (currencyIncreaseDouble)
                {
                    if (value > ___m_currency)
                    {
                        value += (value - ___m_currency);
                    }
                    return true;
                }

                return true;
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

                __instance.stats.SetBaseStatValue(PlayerStats.StatType.Coolness, 20, __instance);
            }


            [HarmonyPatch(typeof(FloorRewardData), "DetermineCurrentMagnificence")]
            [HarmonyPrefix]
            static bool Prefix(float __result)
            {
                // Magnificence table
                //0  0%
                //1  79.84%
                //2  95.53%
                //3  98.62%
                //4  99.23%
                //5  99.34% 

                // Set magnificence to 0 to always have the same chance of getting A or S tier loot
                __result = 0;
                return false;
            }


            [HarmonyPatch(typeof(FloorRewardData), "GetTargetQualityFromChances")]
            [HarmonyPrefix]
            static void Prefix(ref float fran, ref float dChance, ref float cChance, ref float bChance, ref float aChance, ref float sChance)
            {
                // More chance for higher quality
                //fran += Math.Min(1, fran * 1.1f);
            }
        }
    }
}
