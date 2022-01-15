using Assets.Code.Actor;
using Assets.Code.Combat;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace DarkestDungeon2Mod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static ManualLogSource logger;
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            logger = Logger;
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(ActorInstance), "GetStatValue")]
        public class Patch_ActorInstance_GetStatValue
        {
            static void Postfix(ActorInstance __instance, Team ___m_Team, ref float __result, ActorStatType actorStatType)
            {
                if (__instance.ActorName.Length == 0 || __result <= 0)
                {
                    return;
                }
                if (actorStatType == ActorStatType.CRIT_CHANCE)
                {
                    float newResult = __result * 2f;
                    logger.LogInfo($"{__instance.ActorName} crit chance: {__result} -> {newResult}");
                    __result = newResult;
                }
                else if (actorStatType == ActorStatType.DEATHS_DOOR_CHANCE)
                {
                    float newResult = __result * 2f;
                    logger.LogInfo($"{__instance.ActorName} death's door chance: {__result} -> {newResult}");
                    __result = newResult;
                }
            }
        }
    }
}
