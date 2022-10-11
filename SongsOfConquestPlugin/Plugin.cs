using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Lavapotion.Networking;
using SongsOfConquest.Common;
using SongsOfConquest.Common.Adventure;
using SongsOfConquest.Common.Gamestate;
using static SongsOfConquest.Common.Adventure.AddResourceCommand;

namespace SongsOfConquestPlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource log;

        private void Awake()
        {
            // Plugin startup logic
            log = Logger;
            log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} starts loading");
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            foreach (var method in harmony.GetPatchedMethods())
            {
                log.LogInfo($"Patched method {method.ReflectedType.FullName}.{method.Name}");
            }
            log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} finishes oading");
        }
    }

    [HarmonyPatch(typeof(AddResourceCommand.ResponseExecutor))]
    class Patches_ResponseExecutor
    {
        [HarmonyPrefix]
        [HarmonyPatch("Execute")]
        private static bool Prefix_Execute(ICommonAdventureFacade ____adventureFacade, Response response, ExecutorInfo info)
        {
            var team = ____adventureFacade.Teams.Get(response.TeamId);
            if (!team.GetIsNeutral() && !team.IsAiPlayer())
            {
                int curVal = response.Amount;
                int newVal = curVal * 2;
                team.Resources.AddResource(response.ResourceType, newVal);
                // Plugin.log.LogInfo($"{response.ResourceType} income changed from {curVal} to {newVal}");
                return false;
            }
            return true;
        }
    }
}
