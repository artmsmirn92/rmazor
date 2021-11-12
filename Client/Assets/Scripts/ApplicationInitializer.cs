﻿using Constants;
using DI.Extensions;
using Entities;
using GameHelpers;
using Games.RazorMaze.Controllers;
using Managers;
using Mono_Installers;
using Network;
using Ticker;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;
using Zenject;

public class ApplicationInitializer : MonoBehaviour
{
    #region nonpublic members

    private static GameObject _debugReporter; 
    
    #endregion
    
    #region inject
    
    private IGameTicker          GameTicker          { get; set; }
    private IAdsManager          AdsManager          { get; set; }
    private IAnalyticsManager    AnalyticsManager    { get; set; }
    private ILocalizationManager LocalizationManager { get; set; }
    private ILevelsLoader        LevelsLoader        { get; set; }
    private IScoreManager        ScoreManager        { get; set; }

    [Inject] 
    public void Inject(
        IGameTicker _GameTicker,
        IAdsManager _AdsManager,
        IAnalyticsManager _AnalyticsManager,
        ILocalizationManager _LocalizationManager,
        ILevelsLoader _LevelsLoader,
        IScoreManager _ScoreManager)
    {
        GameTicker = _GameTicker;
        AdsManager = _AdsManager;
        AnalyticsManager = _AnalyticsManager;
        LocalizationManager = _LocalizationManager;
        LevelsLoader = _LevelsLoader;
        ScoreManager = _ScoreManager;
    }


    #endregion
    
    #region engine methods
    
    private void Start()
    {
        Application.targetFrameRate = 120;
        DataFieldsMigrator.InitDefaultDataFieldValues();
        InitGameManagers();
        GameTicker.ClearRegisteredObjects();
        LevelMonoInstaller.Release = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(SceneNames.Level);
    }
    
    private void OnSceneLoaded(Scene _Scene, LoadSceneMode _)
    {
        if (_Scene.name.EqualsIgnoreCase(SceneNames.Level))
        {
            GameClientUtils.GameId = 1; // если игра будет только одна, то и париться с GameId нет смысла
            InitGameController();
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion
    
    #region nonpublic methods
    
    private void InitGameManagers()
    {
        GameClient.Instance.Init();
        AdsManager.Init();
        AnalyticsManager.Init();
        LocalizationManager.Init();
    }
    
    private void InitGameController()
    {
        Coroutines.Run(Coroutines.WaitWhile(
            () => !AssetBundleManager.BundlesLoaded,
            () =>
            {
                var controller = GameController.CreateInstance();
                controller.Initialized += () =>
                {
                    int levelIndex = SaveUtils.GetValue<int>(SaveKey.CurrentLevelIndex);
                    var info = LevelsLoader.LoadLevel(1, levelIndex);
                    controller.Model.LevelStaging.LoadLevel(info, levelIndex);
                };
                controller.Init();
            }));
    }


    #endregion
}