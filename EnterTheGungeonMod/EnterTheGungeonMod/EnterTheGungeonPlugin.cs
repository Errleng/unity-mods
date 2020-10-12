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
            [HarmonyPostfix]
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
        }
    }
}
