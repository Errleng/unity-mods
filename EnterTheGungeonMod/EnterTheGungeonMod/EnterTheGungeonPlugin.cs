using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace EnterTheGungeonMod
{
    [BepInPlugin("org.bepinex.plugins.enterthegungeonplugin", "Enter The Gungeon Plugin", "1.0.0.0")]
    public class EnterTheGungeonPlugin : BaseUnityPlugin
    {
        static string[] junkanCombatSpeech;

        const string minorPatchHeading = "General";
        const string majorPatchHeading = "Major";

        static ManualLogSource logger;
        static Utilities util;

        static bool infiniteKeys = false;
        static bool currencyIncreaseOnly = false;
        static bool fixSemiAutoFireRate = true;
        static bool scouterHealthBars = false;
        static float currencyDropMult = 1.5f;
        static float creditDropMult = 2f;
        static int maxMagnificence = 0;
        static float initialCoolness = 10;
        static float chestChanceMult = 1f;

        private static ConfigFile config;
        private static ConfigEntry<float> configReloadTimeMult;
        private static ConfigEntry<float> configSpreadMult;
        private static ConfigEntry<float> configRateOfFireMult;
        private static ConfigEntry<int> configJunkanSpeakChance;

        private void Awake()
        {
            logger = Logger;
            logger.LogInfo("Enter The Gungeon plugin loaded!");

            ReadFiles();

            config = Config;
            initConfig();

            Harmony.CreateAndPatchAll(typeof(MinorPatches));
            Harmony.CreateAndPatchAll(typeof(PlayerProjectilePatches));
            Harmony.CreateAndPatchAll(typeof(CompanionPatches));
        }

        private static void initConfig()
        {
            configReloadTimeMult = config.Bind(minorPatchHeading, "reloadTimeMult", 1f, "Player reload cooldown multiplier");
            configSpreadMult = config.Bind(minorPatchHeading, "spreadMult", 1f, "Player shooting spread multiplier");
            configRateOfFireMult = config.Bind(minorPatchHeading, "rateOfFireMult", 1f, "Player rate of fire multiplier");
            configJunkanSpeakChance = config.Bind(majorPatchHeading, "junkanSpeakChance", 100, "Ser Junkan combat speak chance");
        }

        private static void ReadFiles()
        {
            logger.LogInfo($"Current directory: {Directory.GetCurrentDirectory()}");
            junkanCombatSpeech = File.ReadAllLines("junkan-combat-speech.txt");
        }

        private static void SpawnItems()
        {
            PlayerController player = GameManager.Instance.PrimaryPlayer;
            PickupObject junk = PickupObjectDatabase.GetById(GlobalItemIds.Junk);
            junk.Pickup(player);
            PickupObject junkan = PickupObjectDatabase.GetById(GlobalItemIds.SackKnightBoon);
            junkan.Pickup(player);
        }

        private static Dictionary<string, bool> firstRun = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "InfiniteKeys", true },
            { "Currency", true },
            { "ControllerFakeSemiAutoCooldown", true },
            { "Start", true },
            { "DetermineCurrentMagnificence", true },
            { "GetTargetQualityFromChances", true },
            { "SpawnCurrency", true },
            { "Gun.Update", true },
            { "Projectile.Update", true }
        };


        public class MinorPatches
        {

            [HarmonyPatch(typeof(PlayerController), "HandlePlayerInput")]
            [HarmonyPostfix]
            static void Postfix(PlayerController __instance)
            {
                if (__instance.AcceptingNonMotionInput)
                {
                    BraveInput instanceForPlayer = BraveInput.GetInstanceForPlayer(__instance.PlayerIDX);
                    bool isKeyboard = instanceForPlayer.IsKeyboardAndMouse(false);
                    if (isKeyboard)
                    {
                        if (Input.GetKeyDown(KeyCode.H))
                        {
                            util.ToggleHealthBars();
                            logger.LogInfo($"Toggled health bars to {util.healthBars}");
                        }
                        if (Input.GetKeyDown(KeyCode.Z))
                        {
                            util.ToggleAutoBlank();
                            logger.LogInfo($"Toggled auto blank to {util.autoBlank}");
                        }
                        if (Input.GetKeyDown(KeyCode.G))
                        {
                            SilencerInstance.DestroyBulletsInRange(GameManager.Instance.PrimaryPlayer.CenterPosition, 10000, true, true);
                        }
                        if (Input.GetKeyDown(KeyCode.I))
                        {
                            ReadFiles();
                        }
                        if (Input.GetKeyDown(KeyCode.U))
                        {
                            SpawnItems();
                        }
                    }
                }
            }

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
                    //Gun currentGun = GameManager.Instance.PrimaryPlayer.CurrentGun;
                    //__result = currentGun.GetPrimaryCooldown();
                    __result = 0;
                }
            }


            [HarmonyPatch(typeof(PlayerController), "Start")]
            [HarmonyPrefix]
            static void Prefix(PlayerController __instance)
            {
                util = new Utilities(__instance);

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
                    util.ToggleHealthBars(true);
                }

                PlayerStats stats = __instance.stats;
                stats.SetBaseStatValue(PlayerStats.StatType.Coolness, initialCoolness, __instance);
                // lower is better
                stats.SetBaseStatValue(PlayerStats.StatType.ReloadSpeed, configReloadTimeMult.Value * stats.GetBaseStatValue(PlayerStats.StatType.ReloadSpeed), __instance);
                stats.SetBaseStatValue(PlayerStats.StatType.Accuracy, configSpreadMult.Value * stats.GetBaseStatValue(PlayerStats.StatType.Accuracy), __instance);
                stats.SetBaseStatValue(PlayerStats.StatType.RateOfFire, configRateOfFireMult.Value * stats.GetBaseStatValue(PlayerStats.StatType.RateOfFire), __instance);
            }


            [HarmonyPatch(typeof(GameManager), "DelayedLoadCustomLevel")]
            [HarmonyPostfix]
            static void Postfix()
            {

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


            [HarmonyPatch(typeof(LootEngine), "SpawnCurrency",
                new Type[] { typeof(Vector2), typeof(int), typeof(bool), typeof(Vector2), typeof(float), typeof(float), typeof(float) })]
            [HarmonyPrefix]
            static void Prefix(bool isMetaCurrency, ref int amountToDrop)
            {
                if (firstRun["SpawnCurrency"])
                {
                    logger.LogInfo("Loaded ETGPlugin LootEngine SpawnCurrency()");
                    firstRun["SpawnCurrency"] = false;
                }

                int pastVal = amountToDrop;
                if (isMetaCurrency)
                {
                    amountToDrop = (int)(amountToDrop * creditDropMult);
                }
                else
                {
                    amountToDrop = (int)(amountToDrop * currencyDropMult);
                }

                logger.LogInfo($"Currency: {pastVal} -> {amountToDrop}");
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

        public class PlayerProjectilePatches
        {
            const bool SPLIT_SHOTS = true;
            const int SHOTS_PER_LIFE = 4;
            const float MAX_DEVIATION = 45f;
            static Dictionary<Projectile, float> splitProjs = new Dictionary<Projectile, float>();

            [HarmonyPatch(typeof(Projectile), "Update")]
            [HarmonyPostfix]
            static void Postfix(Projectile __instance)
            {
                if (firstRun["Projectile.Update"])
                {
                    logger.LogInfo("Loaded ETGPlugin Projectile Update()");
                    firstRun["Projectile.Update"] = false;
                }


                if (__instance.Owner is PlayerController)
                {
                    //__instance.ResetDistance();
                    //BounceProjModifier bounceMod = __instance.gameObject.GetOrAddComponent<BounceProjModifier>();
                    //if (bounceMod != null)
                    //{
                    //    bounceMod.numberOfBounces = 1;
                    //}

                    //HomingModifier homingMod = __instance.gameObject.GetOrAddComponent<HomingModifier>();
                    //if (homingMod != null)
                    //{
                    //    homingMod.AngularVelocity = 90;
                    //    homingMod.HomingRadius = 100;
                    //}

                    if (SPLIT_SHOTS)
                    {
                        float lifetime = __instance.baseData.range / __instance.Speed;
                        if (splitProjs.ContainsKey(__instance))
                        {

                            float remainingTime = splitProjs[__instance];
                            if (remainingTime <= 0)
                            {
                                float projectileAngle = BraveMathCollege.Atan2Degrees(__instance.Direction);
                                float fuzzyAngle = projectileAngle + UnityEngine.Random.Range(-MAX_DEVIATION, MAX_DEVIATION);
                                splitProjs[__instance] = lifetime;
                                ShootSingleProjectile(__instance, __instance.transform.position, fuzzyAngle);
                            }
                            else
                            {
                                splitProjs[__instance] = remainingTime - BraveTime.DeltaTime;
                            }
                        }
                        else
                        {
                            Dictionary<Projectile, float> aliveProjs = (from kvp in splitProjs
                                                                        where kvp.Key != null
                                                                        select kvp).ToDictionary(kv => kv.Key, kv => kv.Value);
                            splitProjs = aliveProjs;
                            splitProjs.Add(__instance, lifetime / (1 + SHOTS_PER_LIFE));
                        }
                    }
                }
            }

            private static void ShootSingleProjectile(Projectile projectile, Vector2 spawnPosition, float angle)
            {
                GameObject gameObject = SpawnManager.SpawnProjectile(projectile.gameObject, spawnPosition.ToVector3ZUp(spawnPosition.y), Quaternion.Euler(0f, 0f, angle), true);
                Projectile component = gameObject.GetComponent<Projectile>();
                component.Owner = projectile.Owner;
                component.Shooter = component.Owner.specRigidbody;
                if (component.Owner is PlayerController)
                {
                    PlayerStats stats = (component.Owner as PlayerController).stats;
                    component.baseData.damage *= stats.GetStatValue(PlayerStats.StatType.Damage);
                    component.baseData.speed *= stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
                    component.baseData.force *= stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
                    (component.Owner as PlayerController).DoPostProcessProjectile(component);
                }
            }
        }


        public class TextBoxSeries : MonoBehaviour
        {
            private const int TEXTBOX_MAX_CHARS = 500;

            private float TEXT_REVEAL_SPEED
            {
                get
                {
                    switch (GameManager.Options.TextSpeed)
                    {
                        case GameOptions.GenericHighMedLowOption.LOW:
                            return 27f;
                        case GameOptions.GenericHighMedLowOption.MEDIUM:
                            return 100f;
                        case GameOptions.GenericHighMedLowOption.HIGH:
                            return float.MaxValue;
                        default:
                            return 100f;
                    }
                }
            }

            public List<string> texts { get; set; }

            public void Init(string text)
            {
                texts = new List<string>();
                string[] words = text.Split(' ');
                string curText = "";
                foreach (string word in words)
                {
                    if ((curText + word).Length >= TEXTBOX_MAX_CHARS)
                    {
                        texts.Add(curText);
                        curText = word;
                    }
                    else
                    {
                        if (curText.Length == 0)
                        {
                            curText = word;
                        }
                        else
                        {
                            curText += " " + word;
                        }
                    }
                }
                texts.Add(curText);
            }


            public float ShowTextTime(string text)
            {
                return 1 + text.Length / TEXT_REVEAL_SPEED;
            }

            private IEnumerator ShowTextBoxes(AIActor actor, Vector3 offset)
            {
                foreach (string text in texts)
                {
                    float time = ShowTextTime(text);
                    TextBoxManager.ShowTextBox(actor.transform.position + offset, actor.transform, time, text, "", false, TextBoxManager.BoxSlideOrientation.NO_ADJUSTMENT, false, false);
                    yield return new WaitForSeconds(time);
                    TextBoxManager.ClearTextBox(actor.transform);
                }
                yield return new WaitForSeconds(1f);
                yield break;
            }

            public float TotalSpeakLength()
            {
                float sum = 0;
                foreach (string text in texts)
                {
                    sum += ShowTextTime(text);
                }
                return sum;
            }

            public void Speak(AIActor actor, Vector3 offset)
            {
                StartCoroutine(ShowTextBoxes(actor, offset));
            }
        }


        public class CompanionPatches
        {
            static System.Random random = new System.Random();
            static float junkanSpeechTimer;


            [HarmonyPatch(typeof(SackKnightController), "Update")]
            [HarmonyPostfix]
            static void UpdatePostfix(SackKnightController __instance)
            {
                junkanSpeechTimer -= BraveTime.DeltaTime;
            }

            [HarmonyPatch(typeof(SackKnightController), "Start")]
            [HarmonyPostfix]
            static void StartPostfix(SackKnightController __instance)
            {
                __instance.aiActor.MovementSpeed *= 5;
            }

            [HarmonyPatch(typeof(SackKnightAttackBehavior), "CurrentFormCooldown", MethodType.Getter)]
            [HarmonyPostfix]
            static void Postfix(ref float __result)
            {
                __result /= 10;
            }


            [HarmonyPatch(typeof(SackKnightAttackBehavior), "DoAttack")]
            [HarmonyPostfix]
            static void Postfix(AIActor ___m_aiActor)
            {
                int speechRoll = random.Next(100);
                if (junkanSpeechTimer <= 0 && speechRoll <= configJunkanSpeakChance.Value)
                {
                    Vector3 offset = new Vector3(0.75f, 1f, 0f);
                    GameObject gameObj = new GameObject("PlayerController");
                    TextBoxSeries quip = gameObj.AddComponent<TextBoxSeries>();
                    int chosenSpeech = random.Next(junkanCombatSpeech.Length);
                    quip.Init(junkanCombatSpeech[chosenSpeech]);
                    junkanSpeechTimer = quip.TotalSpeakLength();
                    logger.LogInfo($"JUNKAN: {junkanSpeechTimer}, {chosenSpeech}, {speechRoll}");
                    quip.Speak(___m_aiActor, offset);
                }
            }
        }
    }
}
