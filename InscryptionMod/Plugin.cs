using BepInEx;
using DiskCardGame;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using HarmonyLib;
using BepInEx.Logging;
using System.Linq;
using APIPlugin;

namespace InscryptionMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("cyantist.inscryption.api", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private static readonly string ASSETS_PATH = "BepInEx/plugins/custom-art";
        private static readonly string ABILITY_ART_PATH = $"{ASSETS_PATH}/sigils";

        public static ManualLogSource logger;

        private void Awake()
        {
            // Plugin startup logic
            logger = Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            foreach (var method in harmony.GetPatchedMethods())
            {
                Logger.LogInfo($"Patched method: {method.Name}");
            }

            LoadAbility(typeof(ApocalypseBirdAbility), "Herald of the Apocalypse", "On death, if all three birds of the Black Forest are dead, summon Apocalypse Bird.", $"{ABILITY_ART_PATH}/apocalypse-bird.png");
        }

        //private void LoadAbility(Type abilityType, string name, string description, string texturePath)
        //{
        //    var abilityInfo = AbilityManager.New(PluginInfo.PLUGIN_GUID, name, description, abilityType, texturePath);
        //    abilityInfo.SetDefaultPart1Ability();
        //}

        private void LoadAbility(Type abilityType, string name, string description, string texturePath, string dialogue = "")
        {
            AbilityInfo abilityInfo = ScriptableObject.CreateInstance<AbilityInfo>();
            abilityInfo.powerLevel = 0;
            abilityInfo.rulebookName = name;
            Traverse.Create(abilityInfo).Field("rulebookDescription").SetValue(description);
            abilityInfo.metaCategories = new List<AbilityMetaCategory> { AbilityMetaCategory.Part1Modular, AbilityMetaCategory.Part1Rulebook };
            abilityInfo.opponentUsable = true;

            if (dialogue.Length > 0)
            {
                abilityInfo.abilityLearnedDialogue = new DialogueEvent.LineSet(
                    new List<DialogueEvent.Line> {
                        new DialogueEvent.Line { text = "dialogue" }
                    }
                );
            }

            abilityInfo.canStack = false;
            var texture = LoadTextureFromFile(texturePath);
            var abilityId = AbilityIdentifier.GetAbilityIdentifier(PluginInfo.PLUGIN_GUID, abilityInfo.rulebookName);
            var newAbility = new NewAbility(abilityInfo, abilityType, texture, abilityId);
            Traverse.Create(abilityType).Field("ability").SetValue(newAbility.ability);
        }

        private Texture2D LoadTextureFromFile(string path)
        {
            if (!File.Exists(path))
            {
                Logger.LogError($"Could not load texture at '{path}'! Current directory is '{Directory.GetCurrentDirectory()}'");
                return null;
            }
            var image = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.LoadImage(image);
            return texture;
        }

        public class CustomGameManager
        {
            public static List<PlayableCard> encounterGraveyard = new List<PlayableCard>();

            public static void OnStartGame(EncounterData encounterData)
            {
                encounterGraveyard.Clear();
            }

            public static void OnDie(PlayableCard instance, bool wasSacrifice, PlayableCard killer, bool playSound)
            {
                encounterGraveyard.Add(instance);
                logger.LogDebug($"Player graveyard: {string.Join(", ", encounterGraveyard.Where(card => !card.OpponentCard).Select(card => card.Info.DisplayedNameLocalized))}");
                logger.LogDebug($"Opponent graveyard: {string.Join(", ", encounterGraveyard.Where(card => card.OpponentCard).Select(card => card.Info.DisplayedNameLocalized))}");
            }
        }

        public class Patches_TurnManager
        {
            [HarmonyPatch(typeof(TurnManager), "StartGame", new Type[] { typeof(EncounterData) })]
            public class Patch_StartGame
            {
                static void Prefix(EncounterData encounterData)
                {
                    CustomGameManager.OnStartGame(encounterData);
                }
            }
        }

        public class Patches_PlayableCard
        {
            [HarmonyPatch(typeof(PlayableCard), "Die")]
            public class Patch_OnDie
            {
                static void Prefix(PlayableCard __instance, bool wasSacrifice, PlayableCard killer, bool playSound)
                {
                    CustomGameManager.OnDie(__instance, wasSacrifice, killer, playSound);
                }
            }
        }

        public class Patches_TriggerReceiver
        {
            [HarmonyPatch(typeof(TriggerReceiver), "OnDie")]
            public class Patch_OnDie
            {
                static void Prefix(bool wasSacrifice, PlayableCard killer)
                {
                }
            }
        }
    }
}
