using System;
using BepInEx;
using Failbetter.Core.DataInterfaces;
using Failbetter.Core.Provider;
using HarmonyLib;
using Skyless.Assets.Code.Skyless.Game.Services;
using Skyless.Assets.Code.Skyless.UI.Controllers;
using Skyless.Assets.Code.Skyless.UI.Interfaces;
using Skyless.Assets.Code.Skyless.UI.Presenters.HUD;
using UniRx;
using UnityEngine;

namespace SunlessSkiesMod
{
    [BepInPlugin("com.sunless.skies.mod", "Sunless Skies Plug-In", "1.0.0.0")]
    public class SunlessSkiesPlugin : BaseUnityPlugin
    {
        private const string CinematicModeKey = "x"; // appreciate the scenery
        private const string SpeedModeKey = "z"; // the game is too slow
        private const string FreezeKey = "u"; // don't move
        private const string HealKey = ";"; // save the player from dying of attrition
        private const string ZoomInKey = "="; // cinematic mode makes physics wacky
        private const string ZoomOutKey = "-"; // cinematic mode makes physics wacky
        private const float MovementMultiplier = 20f;
        private const float CinematicModeAngularThrustFactor = 100000f; // cinematic mode makes physics wacky

        private void Awake()
        {
            Logger.LogInfo("Sunless Skies mod loaded!");
            var harmony = new Harmony("com.sunless.skies.mod");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(PlayerController), "EnablePlayer")]
        public class Patch_PlayerController_EnablePlayer
        {
            private static bool _isPatched;
            private static bool _spedUp;

            private static void Postfix(PlayerController __instance)
            {
                if (!_isPatched)
                {
                    _isPatched = true;
                    Debug.Log("PlayerController.EnablePlayer() postfix");
                    var pcTraverse = Traverse.Create(__instance);
                    var _hudController = pcTraverse.Field("_hudController").GetValue<IHUDController>();
                    Observable.EveryUpdate()
                        .Where(_ => Input.GetKeyDown(CinematicModeKey))
                        .Subscribe(x =>
                        {
                            Debug.Log($"angular thrust: {__instance.AngularThrust}");
                            if (__instance.PrefabContainer.gameObject.activeSelf)
                            {
                                __instance.PrefabContainer.gameObject.SetActive(false);
                                __instance.AngularThrust /= CinematicModeAngularThrustFactor;
                                _hudController.HideHUDUiObject();
                            }
                            else
                            {
                                __instance.PrefabContainer.gameObject.SetActive(true);
                                __instance.AngularThrust *= CinematicModeAngularThrustFactor;
                                _hudController.DisplayHUD();
                            }
                        });

                    Observable.EveryUpdate()
                        .Where(_ => Input.GetKeyDown(SpeedModeKey))
                        .Subscribe(x =>
                        {
                            if (_spedUp)
                            {
                                __instance.MaxSpeed /= MovementMultiplier;
                                __instance.ForwardThrust /= MovementMultiplier;
                                __instance.BackwardThrust /= MovementMultiplier;
                                __instance.LinearDrag /= MovementMultiplier;
                                __instance.AngularThrust /= MovementMultiplier;
                                __instance.AngularDrag /= MovementMultiplier;
                            }
                            else
                            {
                                __instance.MaxSpeed *= MovementMultiplier;
                                __instance.ForwardThrust *= MovementMultiplier;
                                __instance.BackwardThrust *= MovementMultiplier;
                                __instance.LinearDrag *= MovementMultiplier;
                                __instance.AngularThrust *= MovementMultiplier;
                                __instance.AngularDrag *= MovementMultiplier;
                            }

                            _spedUp = !_spedUp;
                        });

                    Observable.EveryUpdate()
                        .Where(_ => Input.GetKeyDown(FreezeKey))
                        .Subscribe(x =>
                        {
                            __instance.GetComponent<Rigidbody2D>().isKinematic =
                                !__instance.GetComponent<Rigidbody2D>().isKinematic;
                            __instance.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                        });

                    Observable.EveryUpdate()
                        .Where(_ => Input.GetKeyDown(HealKey))
                        .Subscribe(x =>
                        {
                            var currentCharacter = __instance._characterService.CurrentCharacter;
                            var wellKnownQualityProvider = __instance._wellKnownQualityProvider;
                            currentCharacter.AcquireQualityAtExplicitLevel(wellKnownQualityProvider.Hull(),
                                currentCharacter.GetEffectiveQualityLevel(wellKnownQualityProvider.MaxHull()));
                            currentCharacter.AcquireQualityAtExplicitLevel(wellKnownQualityProvider.Crew(),
                                currentCharacter.GetEffectiveQualityLevel(wellKnownQualityProvider.Quarters()));
                            currentCharacter.AcquireQualityAtExplicitLevel(wellKnownQualityProvider.Fuel(),
                                Math.Max(5,
                                    currentCharacter.GetEffectiveQualityLevel(wellKnownQualityProvider.Fuel())));
                            currentCharacter.AcquireQualityAtExplicitLevel(wellKnownQualityProvider.Supplies(),
                                Math.Max(5,
                                    currentCharacter.GetEffectiveQualityLevel(wellKnownQualityProvider.Supplies())));
                            currentCharacter.AcquireQualityAtExplicitLevel(wellKnownQualityProvider.Terror(), 0);
                        });
                }
            }
        }

        [HarmonyPatch(typeof(ChartController), "BindHotkeys")]
        public class Patch_ChartController_BindHotkeys
        {
            private static bool _isPatched;
            private static bool _footerHidden;

            private static void Postfix(ChartController __instance)
            {
                if (!_isPatched)
                {
                    _isPatched = true;
                    Debug.Log("ChartController.BindHotkeys() postfix");
                    var ccTraverse = Traverse.Create(__instance);
                    var _footerController = ccTraverse.Field("_footerController").GetValue<IFooterController>();
                    Observable.EveryUpdate()
                        .Where(_ => Input.GetKeyDown(CinematicModeKey))
                        .Subscribe(x =>
                        {
                            if (_footerHidden)
                                _footerController.DisplayFooter();
                            else
                                _footerController.HideFooterObject();

                            _footerHidden = !_footerHidden;
                        });
                }
            }
        }

        [HarmonyPatch(typeof(OfficerHudController), MethodType.Constructor,
            typeof(IPresenterFactory<IOfficerHudPresenter>), typeof(ICharacterProgressionController),
            typeof(ICharacterProgressionService), typeof(IWellKnownQualityProvider), typeof(ICharacterRepository),
            typeof(ICharacterService), typeof(ICharacterObservable), typeof(IGameState))]
        public class Patch_OfficerHudController_OfficerHudController
        {
            private static bool _isPatched;
            private static bool _officerHudHidden;

            private static void Postfix(OfficerHudController __instance)
            {
                if (!_isPatched)
                {
                    _isPatched = true;
                    Debug.Log("OfficerHudController.OfficerHudController() postfix");
                    Observable.EveryUpdate()
                        .Where(_ => Input.GetKeyDown(CinematicModeKey))
                        .Subscribe(x =>
                        {
                            if (_officerHudHidden)
                                __instance.DisplayHud(null);
                            else
                                __instance.ClearOutOfficerHudPresenter();

                            _officerHudHidden = !_officerHudHidden;
                        });
                }
            }
        }

        [HarmonyPatch(typeof(PlayerCamera), "LateUpdate")]
        public class Patch_PlayerCamera_LateUpdate
        {
            private static bool _isPatched;
            private static float _minZoom;

            private static void Postfix(PlayerCamera __instance)
            {
                if (!_isPatched)
                {
                    _isPatched = true;
                    var playerCameraTraverse = Traverse.Create(__instance);
                    var minZoomTraverse = playerCameraTraverse.Field("_configuration").Field("MinZoom");
                    _minZoom = minZoomTraverse.GetValue<float>();
                    Observable.EveryUpdate()
                        .Where(_ => Input.GetKeyDown(ZoomInKey))
                        .Subscribe(x =>
                        {
                            Debug.Log($"previous zoom: ${minZoomTraverse.GetValue<float>()}, mod zoom: {_minZoom}");
                            _minZoom -= 100;
                            minZoomTraverse.SetValue(_minZoom);
                        });
                    Observable.EveryUpdate()
                        .Where(_ => Input.GetKeyDown(ZoomOutKey))
                        .Subscribe(x =>
                        {
                            Debug.Log($"previous zoom: ${minZoomTraverse.GetValue<float>()}, mod zoom: {_minZoom}");
                            _minZoom += 100;
                            minZoomTraverse.SetValue(_minZoom);
                        });
                }
            }
        }
    }
}