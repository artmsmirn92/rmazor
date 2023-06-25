#if YANDEX_GAMES
using Common.Helpers;
using mazing.common.Runtime.Constants;
using mazing.common.Runtime.Ticker;
using mazing.common.Runtime.Utils;
using UnityEngine.Events;
using YG;

namespace Common.Managers.Advertising.AdBlocks
{
    public interface IYandexGamesInterstitialAd : IInterstitialAdBase { }
    
    public class YandexGamesInterstitialAd : InterstitialAdBase, IYandexGamesInterstitialAd
    {
        #region nonpublic members

        private bool m_Ready;
        
        protected override string AdSource => AdvertisingNetworks.YandexGames;
        protected override string AdType   => AdTypeInterstitial;

        #endregion
        
        #region inject
        
        private IYandexGameActionsProvider ActionsProvider { get; }
        private IYandexGameFacade          YandexGame      { get; }

        private YandexGamesInterstitialAd(
            GlobalGameSettings         _GlobalGameSettings,
            ICommonTicker              _CommonTicker,
            IYandexGameActionsProvider _ActionsProvider,
            IYandexGameFacade          _YandexGame)
            : base(
                _GlobalGameSettings, 
                _CommonTicker)
        {
            ActionsProvider = _ActionsProvider;
            YandexGame      = _YandexGame;
        }
        
        #endregion

        #region api

        public override bool Ready => m_Ready;

        public override void Init(string _AppId, string _UnitId)
        {
            m_Ready = true;
            ActionsProvider.OnOpenFullscreenAd  += OnAdShown;
            ActionsProvider.OnCloseFullscreenAd += OnAdClosed;
            ActionsProvider.OnErrorFullscreenAd += OnAdFailedToShow;
            base.Init(_AppId, _UnitId);
        }

        public override void LoadAd()
        {
            
        }

        public override void  ShowAd(
            UnityAction _OnShown,
            UnityAction _OnClicked,
            UnityAction _OnClosed, 
            UnityAction _OnFailedToShow)
        {
            if (!m_Ready)
                return;
            YandexGame.FullscreenShowDec();
            m_Ready = false;
            Cor.Run(Cor.Delay(2f, CommonTicker, () => m_Ready = true));
        }

        #endregion
    }
}
#endif