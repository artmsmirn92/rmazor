﻿// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PartialTypeWithSinglePart

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Common;
using Common.Constants;
using Common.Managers;
using Common.Utils;
using mazing.common.Runtime;
using mazing.common.Runtime.CameraProviders;
using mazing.common.Runtime.Constants;
using mazing.common.Runtime.Entities;
using mazing.common.Runtime.Enums;
using mazing.common.Runtime.Managers;
using mazing.common.Runtime.Providers;
using mazing.common.Runtime.Utils;
using RMAZOR;
using RMAZOR.Constants;
using RMAZOR.Controllers;
using RMAZOR.Managers;
using RMAZOR.Models;
using RMAZOR.Views.Common;
using RMAZOR.Views.InputConfigurators;
using SA.Android.App;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using static RMAZOR.Models.ComInComArg;
#if FIREBASE
using Firebase.Extensions;
#endif

namespace SRDebuggerCustomOptions
{
    public partial class SROptions
    {
        private const string CategoryMazeItems     = "Maze Items";
        private const string CategoryCharacter     = "Character";
        private const string CategoryCommon        = "Common";
        private const string CategoryLevels        = "Levels";
        private const string CategoryHaptics       = "Haptics";
        private const string CategoryAds           = "Ads";
        private const string CategoryAudio         = "Audio";
        private const string CategoryFps           = "Fps";
        private const string CategoryBackground    = "Background";
        private const string CategoryNotifications = "Notifications";

        private static int  _adsNetworkIdx;
        private static bool _debugConsoleVisible;

        private static long _levelIndex = 1;

        private static IColorProvider                      _ColorProvider;
        private static AdditionalColorsSetScriptableObject _AdditionalColorsPropsSetScrObj;
        private static AdditionalColorsPropsAssetItemsSet  _AdditionalColorsPropsSet;
        private static int                                 _CurrSetIdx;

        private static ModelSettings               _modelSettings;
        private static ViewSettings                _viewSettings;
        private static IModelLevelStaging          _levelStaging;
        private static IManagersGetter             _managers;
        private static IViewInputCommandsProceeder _commandsProceeder;
        private static ICameraProvider             _cameraProvider;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void ResetState()
        {
            _modelSettings  = null;
            _viewSettings   = null;
            _levelStaging   = null;
            _managers       = null;
            _cameraProvider = null;
            SRLauncher.Initialized += SRLauncherOnInitialized;
        }

        private static void SRLauncherOnInitialized()
        {
            _modelSettings = SRLauncher.ModelSettings;
            _viewSettings = SRLauncher.ViewSettings;
            _levelStaging = SRLauncher.LevelStaging;
            _managers = SRLauncher.Managers;
            _commandsProceeder = SRLauncher.CommandsProceeder;
            _cameraProvider = SRLauncher.CameraProvider;
            SRDebug.Instance.PanelVisibilityChanged += OnPanelVisibilityChanged;
        }

        private static void OnPanelVisibilityChanged(bool _Visible)
        {
            var commandsToLock = RmazorUtils.GetCommandsToLockInGameUiMenus();
            if (_Visible)
                _commandsProceeder.LockCommands(commandsToLock, nameof(SROptions));
            else
                _commandsProceeder.UnlockCommands(commandsToLock, nameof(SROptions));
        }

        #region model settings

        [Category(CategoryCharacter)]
        public float Speed
        {
            get => _modelSettings.characterSpeed;
            set => _modelSettings.characterSpeed = value;
        }

        [Category(CategoryCharacter)]
        public float MoveThreshold
        {
            get => _viewSettings.moveSwipeThreshold;
            set => _viewSettings.moveSwipeThreshold = value;
        }

        [Category(CategoryMazeItems)]
        public float Turret_Projectile_Speed
        {
            get => _modelSettings.turretProjectileSpeed;
            set => _modelSettings.turretProjectileSpeed = value;
        }

        #endregion

        #region view settings

        [Category(CategoryCommon)]
        public float Maze_Rotation_Speed
        {
            get => _viewSettings.mazeRotationSpeed;
            set => _viewSettings.mazeRotationSpeed = value;
        }

        // [Category(CategoryCommon)] public int Money { get; set; }

        // [Category(CategoryCommon)]
        // public bool Set_Money
        // {
        //     get => false;
        //     set
        //     {
        //         if (!value)
        //             return;
        //         var savedGame = new SavedGame
        //         {
        //             FileName = MazorCommonData.SavedGameFileName,
        //             Money = Money,
        //             Level = _levelStaging.LevelIndex
        //         };
        //         _managers.ScoreManager.SaveGameProgress(savedGame, false);
        //     }
        // }

        #endregion

        #region levels

        [Category(CategoryLevels)] public int Level_Index { get; set; }

        [Category(CategoryLevels)]
        public bool Load_By_Index
        {
            get => false;
            set
            {
                if (!value)
                    return;
                var args = new Dictionary<string, object>
                {
                    {KeyGameMode,      ParameterGameModeMain},
                    {KeyNextLevelType, ParameterLevelTypeDefault},
                    {KeyLevelIndex,    Level_Index - 1}
                };
                _commandsProceeder.RaiseCommand(EInputCommand.LoadLevel, args, true);
            }
        }

        [Category(CategoryLevels)]
        public bool Load_Next_Level
        {
            get => false;
            set
            {
                if (!value)
                    return;
                var args = new Dictionary<string, object>
                {
                    {KeyGameMode,      ParameterGameModeMain},
                    {KeyNextLevelType, ParameterLevelTypeDefault},
                    {KeyLevelIndex,    _levelStaging.LevelIndex + 1}
                };
                _commandsProceeder.RaiseCommand(EInputCommand.LoadLevel, args, true);
            }
        }

        [Category(CategoryLevels)]
        public bool Load_Previous_Level
        {
            get => false;
            set
            {
                if (!value)
                    return;
                long levelIndex = _levelStaging.LevelIndex;
                var args = new Dictionary<string, object>
                {
                    {KeyGameMode,      ParameterGameModeMain},
                    {KeyNextLevelType, ParameterLevelTypeDefault},
                    {KeyLevelIndex,    levelIndex - 1}
                };
                _commandsProceeder.RaiseCommand(EInputCommand.LoadLevel, args, true);
            }
        }

        [Category(CategoryLevels)]
        public bool Reload_Current_Level
        {
            get => false;
            set
            {
                if (!value)
                    return;
                long levelIndex = _levelStaging.LevelIndex;
                var args = new Dictionary<string, object>
                {
                    {KeyGameMode,      ParameterGameModeMain},
                    {KeyNextLevelType, ParameterLevelTypeDefault},
                    {KeyLevelIndex,    levelIndex}
                };
                _commandsProceeder.RaiseCommand(EInputCommand.LoadLevel, args, true);
            }
        }

        [Category(CategoryLevels)]
        public bool Finish_Current_Level
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _commandsProceeder.RaiseCommand(EInputCommand.FinishLevel, null, true);
            }
        }

        #endregion

        #region haptics

        [Category(CategoryHaptics)] public float Amplitude { get; set; }

        [Category(CategoryHaptics)] public float Frequency { get; set; }

        [Category(CategoryHaptics)] public float Duration { get; set; }

        [Category(CategoryHaptics)]
        public bool Play_Constant
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _managers.HapticsManager.Play(Amplitude, Frequency, Duration);
            }
        }

        [Category(CategoryHaptics)]
        public bool Play_Emphasis
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _managers.HapticsManager.Play(Amplitude, Frequency);
            }
        }

        [Category(CategoryHaptics)]
        public bool Play_Random_Preset
        {
            get => false;
            set
            {
                if (!value)
                    return;
                var preset = (EHapticsPresetType) Mathf.FloorToInt(Random.value * 8.99f);
                Dbg.Log("Haptics preset: " + Enum.GetName(typeof(EHapticsPresetType), preset));
                _managers.HapticsManager.PlayPreset(preset);
            }
        }

        #endregion

        #region ads

        [Category(CategoryAds)]
        public int AdsProviderIndex
        {
            get => _adsNetworkIdx;
            set => _adsNetworkIdx = value;
        }

        [Category(CategoryAds)]
        public bool Show_Rewarded_Ad
        {
            get => false;
            set
            {
                if (!value)
                    return;
                string adsNetworkName = GetAdsNetworkProviderName(_adsNetworkIdx);
                _managers.AdsManager.ShowRewardedAd(
                    _OnShown: () => Dbg.Log("Rewarded ad was shown."),
                    _OnClosed: () => Dbg.Log("Rewarded ad was closed."),
                    _AdsNetwork: adsNetworkName,
                    _Forced: true);
            }
        }

        [Category(CategoryAds)]
        public bool Show_Interstitial_Ad
        {
            get => false;
            set
            {
                if (!value)
                    return;
                string adsNetworkName = GetAdsNetworkProviderName(_adsNetworkIdx);
                _managers.AdsManager.ShowInterstitialAd(
                    _OnShown: () => Dbg.Log("Interstitial ad was shown."),
                    _OnClosed: () => Dbg.Log("Interstitial ad was closed."),
                    _AdsNetwork: adsNetworkName,
                    _Forced: true);
            }
        }

#if APPODEAL_3
        [Category(CategoryAds)]
        public bool Show_Appodeal_Test_Screen
        {
            get => false;
            set
            {
                if (!value)
                    return;
                AppodealStack.Monetization.Api.Appodeal.ShowTestScreen();
            }
        }
#endif

#if ADMOB_API
        [Category(CategoryAds)]
        public bool Show_Admob_Test_Screen
        {
            get => false;
            set
            {
                if (!value)
                    return;
                GoogleMobileAds.Api.MobileAds.OpenAdInspector(_AdInspectorError =>
                {
                    if (_AdInspectorError == null)
                    {
                        Dbg.Log("Ad inspector error is null");
                        return;
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine("Code: " + _AdInspectorError.GetCode());
                    sb.AppendLine("Cause: " + _AdInspectorError.GetCause());
                    sb.AppendLine("Domain: " + _AdInspectorError.GetDomain());
                    sb.AppendLine("Message: " + _AdInspectorError.GetMessage());
                    Dbg.LogError(sb.ToString());
                });
            }
        }
#endif

        private static string GetAdsNetworkProviderName(int _ProviderIndex)
        {
            Dbg.Log("Reference: 1 => Admob, 2 => Appodeal, 3 => Unity, other => auto");
            return _ProviderIndex switch
            {
                1 => AdvertisingNetworks.Admob,
                2 => AdvertisingNetworks.Appodeal,
                3 => AdvertisingNetworks.UnityAds,
                _ => null
            };
        }

        #endregion

        #region common

        [Category(CategoryCommon)]
        public bool Rate_Game
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _managers.ShopManager.RateGame();
            }
        }

        [Category(CategoryCommon)]
        public bool Clear_Cache
        {
            get => false;
            set
            {
                if (!value)
                    return;
                Caching.ClearCache();
            }
        }

        [Category(CategoryCommon)]
        public bool Show_Locked_Groups_With_Rotation
        {
            get => false;
            set
            {
                if (!value)
                    return;
                var groupNames = (_commandsProceeder as ViewInputCommandsProceeder)?
                    .LockedCommands
                    .Where(_Kvp => _Kvp.Value.Contains(
                                       EInputCommand.RotateClockwise)
                                   || _Kvp.Value.Contains(EInputCommand.RotateCounterClockwise))
                    .Select(_Kvp => _Kvp.Key).ToList();
                var sb = new StringBuilder();
                sb.Append(nameof(Show_Locked_Groups_With_Rotation) + "\n");
                if (groupNames != null)
                    foreach (string gName in groupNames)
                        sb.Append(gName + "\n");
                Dbg.Log(sb.ToString());
            }
        }

        [Category(CategoryCommon)]
        public bool Show_Locked_Groups_With_Movement
        {
            get => false;
            set
            {
                if (!value)
                    return;
                var groupNames = (_commandsProceeder as ViewInputCommandsProceeder)?
                    .LockedCommands
                    .Where(_Kvp => _Kvp.Value.Contains(
                                       EInputCommand.RotateClockwise)
                                   || _Kvp.Value.Contains(EInputCommand.MoveRight))
                    .Select(_Kvp => _Kvp.Key).ToList();
                var sb = new StringBuilder();
                sb.Append(nameof(Show_Locked_Groups_With_Rotation) + "\n");
                if (groupNames != null)
                    foreach (string gName in groupNames)
                        sb.Append(gName + "\n");
                Dbg.Log(sb.ToString());
            }
        }

        [Category(CategoryCommon)]
        public bool Get_Score_Test
        {
            get => false;
            set
            {
                if (!value)
                    return;
                var entity = _managers.ScoreManager.GetScoreFromLeaderboard(
                    DataFieldIds.Level,
                    false);
                Cor.Run(Cor.WaitWhile(() => entity.Result == EEntityResult.Pending,
                    () =>
                    {
                        if (entity.Result == EEntityResult.Fail)
                        {
                            Dbg.LogError(nameof(Get_Score_Test) + ": " + entity.Result);
                            return;
                        }

                        var firstScore = entity.GetFirstScore();
                        if (firstScore.HasValue)
                            Dbg.Log("First score: " + firstScore.Value);
                    }));
            }
        }

        // [Category(CategoryCommon)]
        // public bool Sava_Game_Test
        // {
        //     get => false;
        //     set
        //     {
        //         if (!value)
        //             return;
        //         var savedData = new SavedGame
        //         {
        //             FileName = MazorCommonData.SavedGameFileName,
        //             Money = 10000,
        //             Level = _levelStaging.LevelIndex
        //         };
        //         _managers.ScoreManager.SaveGameProgress(savedData, false);
        //     }
        // }

        [Category(CategoryCommon)]
        public bool Show_Advertising_Id
        {
            get => false;
            set
            {
                if (!value)
                    return;
                var idfaEntity = MazorCommonUtils.GetIdfa();
                Cor.Run(Cor.WaitWhile(() => idfaEntity.Result == EEntityResult.Pending,
                    () => Dbg.Log(idfaEntity.Value)));
            }
        }

// #if FIREBASE
//         [Category(CategoryCommon)]
//         public bool Get_FCM_Token
//         {
//             get => false;
//             set
//             {
//                 if (!value)
//                     return;
//                 Firebase.Messaging.FirebaseMessaging.GetTokenAsync()
//                     .ContinueWithOnMainThread(_Task =>
//                     {
//                         string token = _Task.Result;
//                         Dbg.Log("FCM Token: " + token);
//                         CommonUtils.CopyToClipboard(token);
//                     });
//             }
//         }
// #endif

        // [Category(CategoryCommon)]
        // public bool Show_Money
        // {
        //     get => false;
        //     set
        //     {
        //         if (!value)
        //             return;
        //
        //         static void DisplaySavedGameMoney(bool _FromCache)
        //         {
        //             string str = _FromCache ? "server" : "cache";
        //             var entity =
        //                 _managers.ScoreManager.GetSavedGameProgress(MazorCommonData.SavedGameFileName, _FromCache);
        //             Cor.Run(Cor.WaitWhile(
        //                 () => entity.Result == EEntityResult.Pending,
        //                 () =>
        //                 {
        //                     bool castSuccess = entity.Value.CastTo(out SavedGame savedGame);
        //                     if (entity.Result == EEntityResult.Fail || !castSuccess)
        //                     {
        //                         Dbg.LogWarning($"Failed to load saved game from {str}, entity value: {entity.Value}");
        //                         return;
        //                     }
        //
        //                     Dbg.Log($"{str}: Money: {savedGame.Money}, Level: {savedGame.Level}");
        //                 }));
        //         }
        //
        //         DisplaySavedGameMoney(false);
        //         DisplaySavedGameMoney(true);
        //     }
        // }

        [Category(CategoryCommon)]
        public bool Internet_Connection_Available
        {
            get => false;
            set
            {
                if (!value)
                    return;
                bool res = NetworkUtils.IsInternetConnectionAvailable();
                Dbg.Log("Internet connection available: " + res + " " + Application.internetReachability);
            }
        }

        [Category(CategoryCommon)]
        public bool ClearSaves
        {
            get => false;
            set
            {
                if (!value)
                    return;
                if (System.IO.File.Exists(SaveUtils.SavesPath))
                    System.IO.File.Delete(SaveUtils.SavesPath);
            }
        }

        [Category(CategoryCommon)]
        public bool GC_Collect
        {
            get => false;
            set
            {
                if (!value)
                    return;
                GC.Collect();
            }
        }


        [Category(CategoryCommon)]
        public bool Running_Coroutines_Count
        {
            get => false;
            set
            {
                if (!value)
                    return;
                Dbg.Log("Running coroutines count: " + Cor.GetRunningCoroutinesCount());
            }
        }

        [Category(CategoryCommon)]
        public bool Send_Test_Analytic
        {
            get => false;
            set
            {
                if (!value)
                    return;
                var eventData = new Dictionary<string, object>
                {
                    {AnalyticIds.Parameter1ForTestAnalytic, Mathf.RoundToInt(Random.value * 100)}
                };
                _managers.AnalyticsManager.SendAnalytic(AnalyticIdsRmazor.TestAnalytic, eventData);
            }
        }

        [Category(CategoryCommon)]
        public bool Throw_Test_Exception
        {
            get => false;
            set
            {
                if (!value)
                    return;
                throw new Exception("test exception, ignore this");
            }
        }

        [Category(CategoryCommon)]
        public bool EnableColorGrading
        {
            get => false;
            set
            {
                if (!value)
                    return;
                bool enabled = _cameraProvider.IsEffectEnabled(ECameraEffect.ColorGrading);
                _cameraProvider.EnableEffect(ECameraEffect.ColorGrading, !enabled);
            }
        }

        [Category(CategoryCommon)]
        public bool EnableBloom
        {
            get => false;
            set
            {
                if (!value)
                    return;
                bool enabled = _cameraProvider.IsEffectEnabled(ECameraEffect.Bloom);
                _cameraProvider.EnableEffect(ECameraEffect.Bloom, !enabled);
            }
        }

        [Category(CategoryCommon)]
        public bool ShowHideDebugConsole
        {
            get => false;
            set
            {
                if (!value)
                    return;
                if (_debugConsoleVisible)
                    _managers.DebugManager.HideDebugConsole();
                else
                    _managers.DebugManager.ShowDebugConsole();
                _debugConsoleVisible = !_debugConsoleVisible;
            }
        }

        #endregion

        #region audio

        [Category(CategoryAudio)]
        public bool MuteMusic
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _managers.AudioManager.MuteAudio(EAudioClipType.Music);
            }
        }

        [Category(CategoryAudio)]
        public bool UnmuteMusic
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _managers.AudioManager.UnmuteAudio(EAudioClipType.Music);
            }
        }

        [Category(CategoryAudio)]
        public bool MuteGameSounds
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _managers.AudioManager.MuteAudio(EAudioClipType.GameSound);
            }
        }

        [Category(CategoryAudio)]
        public bool UnmuteGameSounds
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _managers.AudioManager.UnmuteAudio(EAudioClipType.GameSound);
            }
        }

        [Category(CategoryAudio)]
        public bool MuteUiSounds
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _managers.AudioManager.MuteAudio(EAudioClipType.UiSound);
            }
        }

        [Category(CategoryAudio)]
        public bool UnmuteUiSounds
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _managers.AudioManager.UnmuteAudio(EAudioClipType.UiSound);
            }
        }

        #endregion

        #region background

        [Category(CategoryBackground)]
        public bool SetNextBackground
        {
            get => false;
            set
            {
                if (!value)
                    return;
                var gameController = Object.FindObjectOfType<GameControllerMVC>();

                static LevelStageArgs GetNextLevelStageArgsForBackgroundTexture()
                {
                    var settings = new PrefabSetManager(new AssetBundleManagerFake()).GetObject<ViewSettings>(
                        CommonPrefabSetNames.Configs, "view_settings");
                    int group = RmazorUtils.GetLevelsGroupIndex(_levelIndex);
                    int levels = RmazorUtils.GetLevelsInGroup(group);
                    _levelIndex = MathUtils.ClampInverse(
                        _levelIndex + levels,
                        0,
                        400 - 1);
                    var args = new Dictionary<string, object>()
                    {
                        { KeySetBackgroundFromEditor, true}
                    };
                    var fakeArgs = new LevelStageArgs(
                        _levelIndex,
                        ELevelStage.Loaded,
                        ELevelStage.Unloaded,
                        ELevelStage.ReadyToUnloadLevel,
                        ELevelStage.Finished,
                        float.PositiveInfinity,
                        args);
                    return fakeArgs;
                }

                var args = GetNextLevelStageArgsForBackgroundTexture();
                gameController.View.Background.OnLevelStageChanged(args);
                gameController.View.AdditionalBackground.OnLevelStageChanged(args);
            }
        }

        [Category(CategoryBackground)]
        public bool SetNextColorSet
        {
            get => false;
            set
            {
                if (!value)
                    return;

                static void LoadColorsProvider(bool _Forced)
                {
                    if ((_ColorProvider == null || _Forced)
                        && Application.isPlaying
                        && SceneManager.GetActiveScene().name.Contains(SceneNames.Level))
                    {
                        _ColorProvider = Object.FindObjectOfType<ColorProvider>();
                    }
                }

                static void SetNextOrPreviousAdditionalColorSet(bool _Previous)
                {
                    int addict = _Previous ? -1 : 1;

                    void AddAddict()
                    {
                        _CurrSetIdx = MathUtils.ClampInverse(
                            _CurrSetIdx + addict, 0, _AdditionalColorsPropsSet.Count - 1);
                    }

                    AddAddict();
                    while (!_AdditionalColorsPropsSet[_CurrSetIdx].inUse)
                        AddAddict();
                }

                static void LoadSets()
                {
                    if (_AdditionalColorsPropsSet != null
                        && _AdditionalColorsPropsSetScrObj != null)
                    {
                        return;

                    }

                    var manager = new PrefabSetManager(new AssetBundleManagerFake());
                    _AdditionalColorsPropsSetScrObj = manager.GetObject<AdditionalColorsSetScriptableObject>(
                        CommonPrefabSetNames.Configs, "additional_colors_set");
                    _AdditionalColorsPropsSet = _AdditionalColorsPropsSetScrObj.set;
                }

                static void SetAdditionalColorSetColors()
                {
                    if (!Application.isPlaying)
                    {
                        Dbg.LogWarning("This option is available only in play mode");
                        return;
                    }

                    if (_ColorProvider == null)
                    {
                        Dbg.LogError("Color provider is null");
                        return;
                    }

                    var props = _AdditionalColorsPropsSet[_CurrSetIdx];
                    _ColorProvider.SetColor(ColorIds.Main, props.main);
                    _ColorProvider.SetColor(ColorIds.Background1, props.bacground1);
                    _ColorProvider.SetColor(ColorIds.Background2, props.bacground2);
                    _ColorProvider.SetColor(ColorIds.PathItem, props.GetColor(props.pathItemFillType));
                    _ColorProvider.SetColor(ColorIds.PathBackground, props.GetColor(props.pathBackgroundFillType));
                    _ColorProvider.SetColor(ColorIds.PathFill, props.GetColor(props.pathFillFillType));
                    _ColorProvider.SetColor(ColorIds.Character2, props.GetColor(props.characterBorderFillType));
                    _ColorProvider.SetColor(ColorIds.UiBackground, props.GetColor(props.uiBackgroundFillType));
                    CommonDataRmazor.CameraEffectsCustomAnimator?.SetBloom(props.bloom);
                    CommonDataRmazor.BackgroundTextureControllerRmazor?.SetAdditionalInfo(props.additionalInfo);
                }

                LoadColorsProvider(false);
                LoadSets();
                SetNextOrPreviousAdditionalColorSet(false);
                SetAdditionalColorSetColors();
            }
        }

        #endregion

        #region fps

        [Category(CategoryFps)] public float FpsRecordDuration { get; set; }

        [Category(CategoryFps)]
        public bool RecordFps
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _managers.DebugManager.FpsCounter.Record(FpsRecordDuration);
            }
        }

        [Category(CategoryFps)]
        public bool ShowFpsRecord
        {
            get => false;
            set
            {
                if (!value)
                    return;
                var recording = _managers.DebugManager.FpsCounter.GetRecording();
                var sb = new StringBuilder();
                sb.Append("Min Fps: " + recording.FpsMin.ToString("F1"));
                sb.Append("; ");
                sb.AppendLine("Max Fps: " + recording.FpsMax.ToString("F1"));
                sb.AppendLine("Values: ");
                foreach (float fpsValue in recording.FpsValues)
                    sb.AppendLine(fpsValue.ToString("F1"));
                Dbg.Log(sb.ToString());
            }
        }

        #endregion

        #region notifications

        [Category(CategoryNotifications)]
        public bool Test_Notification
        {
            get => false;
            set
            {
                if (!value)
                    return;
                _managers.NotificationsManager.SendNotification(
                    "Come back! We are waiting for you!",
                    "Body",
                    TimeSpan.FromSeconds(10d),
                    _SmallIcon: "small_notification_icon",
                    _LargeIcon: "large_notification_icon");
                
                _managers.NotificationsManager.SendNotification(
                    "Come back! We are waiting for you!",
                    "Body",
                    TimeSpan.FromHours(1d),
                    _SmallIcon: "small_notification_icon",
                    _LargeIcon: "large_notification_icon");
                
                _managers.NotificationsManager.SendNotification(
                    "Come back! We are waiting for you!",
                    "Body",
                    TimeSpan.FromHours(3d),
                    _SmallIcon: "small_notification_icon",
                    _LargeIcon: "large_notification_icon");
                
                _managers.NotificationsManager.SendNotification(
                    "Come back! We are waiting for you!",
                    "Body",
                    TimeSpan.FromHours(6d),
                    _SmallIcon: "small_notification_icon",
                    _LargeIcon: "large_notification_icon");
                
                _managers.NotificationsManager.SendNotification(
                    "Come back! We are waiting for you!",
                    "Body",
                    TimeSpan.FromHours(12d),
                    _SmallIcon: "small_notification_icon",
                    _LargeIcon: "large_notification_icon");
                _managers.NotificationsManager.SendNotification(
                    "Come back! We are waiting for you!",
                    "Body",
                    TimeSpan.FromHours(24d),
                    _SmallIcon: "small_notification_icon",
                    _LargeIcon: "large_notification_icon");
// #endif

            }
        }

        [Category(CategoryNotifications)]
        public bool Show_Notification_Channels
        {
            get => false;
            set
            {
                if (!value)
                    return;
                static void PrintChannelInfo(AN_NotificationChannel channel)
                {
                    Debug.Log("channel.Id: " + channel.Id);
                    Debug.Log("channel.Name: " + channel.Name);
                    Debug.Log("channel.Description: " + channel.Description);
                    Debug.Log("channel.Importance: " + channel.Importance);
                }

                var channels = AN_NotificationManager.GetNotificationChannels();
                foreach (var stsChannel in channels)
                {
                    PrintChannelInfo(stsChannel);
                }
            }
        }

        #endregion
    }
}