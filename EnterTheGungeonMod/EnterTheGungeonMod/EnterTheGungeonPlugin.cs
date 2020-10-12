using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using HarmonyLib.Tools;

namespace EnterTheGungeonMod
{
    [BepInPlugin("org.bepinex.plugins.enterthegungeonplugin", "Enter The Gungeon Plugin", "1.0.0.0")]
    public class EnterTheGungeonPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Enter The Gungeon plugin loaded!");
            HarmonyLib.Tools.Logger.ChannelFilter = HarmonyLib.Tools.Logger.LogChannel.Info;
            HarmonyFileLog.Enabled = true;
            HarmonyFileLog.FileWriterPath = "BepInEx/harmonyLogOutput.log";
            Harmony.CreateAndPatchAll(typeof(Patch));
        }
    }

    public class Patch
    {
        [HarmonyPatch(typeof(PlayerConsumables))]
        [HarmonyPatch("InfiniteKeys", MethodType.Setter)]
        [HarmonyPostfix]
        static void Postfix(PlayerConsumables __instance)
        {
            __instance.InfiniteKeys = true;
        }


        [HarmonyPatch(typeof(PlayerConsumables))]
        [HarmonyPatch("Currency", MethodType.Setter)]
        [HarmonyPrefix]
        static bool Prefix(int value, PlayerConsumables __instance)
        {
            Debug.Log("Loaded ETGPlugin PlayerConsumables Currency setter");
            if (value > __instance.Currency)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        [HarmonyPatch(typeof(PlayerController), "Start")]
        [HarmonyPostfix]
        static void Prefix(PlayerController __instance)
        {
            Debug.Log("Loaded ETGPlugin PlayerController Start()");
            __instance.carriedConsumables.InfiniteKeys = true;
        }
    }
}
