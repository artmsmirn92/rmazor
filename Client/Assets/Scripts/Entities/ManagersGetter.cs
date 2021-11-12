﻿using Controllers;
using Managers;
using Zenject;

namespace Entities
{
    public delegate void SoundManagerHandler(IAudioManager Manager);
    public delegate void AdsManagerHandler(IAdsManager Manager);
    public delegate void AnalyticsManagerHandler(IAnalyticsManager Manager);
    public delegate void ShopManagerHandler(IShopManager Manager);
    public delegate void LocalizationManagerHandler(ILocalizationManager Manager);
    public delegate void ScoreManagerHandler(IScoreManager Manager);
    public delegate void VibrationManagerHandler(IVibrationManager Manager);
    
    public interface IManagersGetter
    {
        IAudioManager        AudioManager        { get; }
        IAnalyticsManager    AnalyticsManager    { get; }
        IAdsManager          AdsManager          { get; } 
        IShopManager         ShopManager         { get; }
        ILocalizationManager LocalizationManager { get; }
        IScoreManager        ScoreManager        { get; }
        IVibrationManager    VibrationManager    { get; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        IDebugManager        DebugManager { get; }
#endif
        void Notify(
            SoundManagerHandler _OnSoundManager = null,
            AnalyticsManagerHandler _OnAnalyticsManager = null,
            AdsManagerHandler _OnAdsManager = null,
            ShopManagerHandler _OnShopManager = null,
            LocalizationManagerHandler _OnLocalizationManager = null,
            ScoreManagerHandler _OnScoreManager = null,
            VibrationManagerHandler _OnVibrationManager = null);
    }

    public class ManagersGetter : IManagersGetter
    {
        #region inject
        
        public IAudioManager        AudioManager        { get; }
        public IAnalyticsManager    AnalyticsManager    { get; }
        public IAdsManager          AdsManager          { get; } 
        public IShopManager         ShopManager         { get; }
        public ILocalizationManager LocalizationManager { get; }
        public IScoreManager        ScoreManager        { get; }
        public IVibrationManager    VibrationManager    { get; }

        public ManagersGetter(
            IAudioManager        _AudioManager,
            IAnalyticsManager    _AnalyticsManager,
            IAdsManager          _AdsManager,
            IShopManager         _ShopManager,
            ILocalizationManager _LocalizationManager,
            IScoreManager        _ScoreManager,
            IVibrationManager    _VibrationManager)
        {
            AudioManager        = _AudioManager;
            AnalyticsManager    = _AnalyticsManager;
            AdsManager          = _AdsManager;
            ShopManager         = _ShopManager;
            LocalizationManager = _LocalizationManager;
            ScoreManager        = _ScoreManager;
            VibrationManager    = _VibrationManager;
        }
        
        #endregion

        #region api
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        [Inject] public IDebugManager DebugManager { get; }
#endif

        public void Notify(
            SoundManagerHandler           _OnSoundManager = null,
            AnalyticsManagerHandler       _OnAnalyticsManager = null,
            AdsManagerHandler             _OnAdsManager = null,
            ShopManagerHandler             _OnShopManager = null,
            LocalizationManagerHandler    _OnLocalizationManager = null,
            ScoreManagerHandler           _OnScoreManager = null,
            VibrationManagerHandler       _OnVibrationManager = null)
        {
            _OnSoundManager         ?.Invoke(AudioManager);
            _OnAnalyticsManager     ?.Invoke(AnalyticsManager);
            _OnAdsManager           ?.Invoke(AdsManager);
            _OnShopManager          ?.Invoke(ShopManager);
            _OnLocalizationManager  ?.Invoke(LocalizationManager);
            _OnScoreManager         ?.Invoke(ScoreManager);
            _OnVibrationManager     ?.Invoke(VibrationManager);
        }

        #endregion
    }
}