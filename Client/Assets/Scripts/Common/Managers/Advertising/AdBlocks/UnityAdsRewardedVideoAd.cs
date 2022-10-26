﻿#if UNITY_ADS_API

using System.Text;
using Common.Helpers;
using Common.Ticker;
using UnityEngine.Advertisements;
using UnityEngine.Events;

namespace Common.Managers.Advertising.AdBlocks
{
    public interface IUnityAdsRewardedAd :
        IRewardedAdBase,
        IUnityAdsLoadListener, 
        IUnityAdsShowListener { }
    
    public class UnityAdsRewardedAd : RewardedAdBase, IUnityAdsRewardedAd
    {
        #region nonpublic members
        
        protected override string AdSource => AdvertisingNetworks.UnityAds;
        protected override string AdType   => AdTypeRewarded;
        
        private bool m_IsReady;

        #endregion

        #region inject
        
        public UnityAdsRewardedAd(
            GlobalGameSettings _Settings,
            ICommonTicker      _CommonTicker)
            : base(_Settings, _CommonTicker) { }

        #endregion

        #region api
        
        public override bool Ready => m_IsReady;
        
        public override void LoadAd()
        {
            m_IsReady = false;
            Advertisement.Load(UnitId, this);
        }

        public override void ShowAd(
            UnityAction _OnShown, 
            UnityAction _OnClicked, 
            UnityAction _OnReward,
            UnityAction _OnClosed)
        {
            OnShown   = _OnShown;
            OnClicked = _OnClicked;
            OnReward  = _OnReward;
            OnClosed  = _OnClosed;
            Advertisement.Show(UnitId, this);
        }

        public virtual void OnUnityAdsAdLoaded(string _PlacementId)
        {
            m_IsReady = true;
            OnAdLoaded();
        }

        public virtual void OnUnityAdsFailedToLoad(
            string            _PlacementId,
            UnityAdsLoadError _Error,
            string            _Message)
        {
            m_IsReady = false;
            OnAdFailedToLoad();
            var sb = new StringBuilder();
            sb.AppendLine($"message: {_Message}");
            sb.AppendLine($"error: {_Error}");
            Dbg.LogWarning(sb);
        }

        public virtual void OnUnityAdsShowFailure(
            string            _PlacementId,
            UnityAdsShowError _Error,
            string            _Message)
        {
            m_IsReady = false;
            OnAdFailedToShow();
            var sb = new StringBuilder();
            sb.AppendLine($"message: {_Message}");
            sb.AppendLine($"error: {_Error}");
            Dbg.LogWarning(sb);
        }

        public virtual void OnUnityAdsShowStart(string _PlacementId)
        {
            Dbg.Log($"Unity Ads: {AdType} ad show start");
            m_IsReady = false;
        }

        public virtual void OnUnityAdsShowClick(string _PlacementId)
        {
            m_IsReady = false;
            OnAdClicked();
        }

        public virtual void OnUnityAdsShowComplete(
            string                      _PlacementId,
            UnityAdsShowCompletionState _ShowCompletionState)
        {
            if (_ShowCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                OnAdRewardGot();
            }
            m_IsReady = false;
            Dbg.Log($"Completion state: {_ShowCompletionState}");
        }

        #endregion
    }
}

#endif