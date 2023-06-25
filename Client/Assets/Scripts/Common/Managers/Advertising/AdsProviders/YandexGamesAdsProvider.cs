#if YANDEX_GAMES
using Common.Helpers;
using Common.Managers.Advertising.AdBlocks;
using mazing.common.Runtime.Constants;
using UnityEngine.Events;
using YG;

namespace Common.Managers.Advertising.AdsProviders
{
    public interface IYandexGamesAdsProvider : IAdsProvider { }
    
    public class YandexGamesAdsProvider : AdsProviderCommonBase, IYandexGamesAdsProvider
    {
        #region nonpublic members

        protected override string AppId              => string.Empty;
        protected override string InterstitialUnitId => string.Empty;
        protected override string RewardedUnitId     => string.Empty;

        #endregion
        
        #region inject

        private IYandexGameActionsProvider ActionsProvider { get; }

        private YandexGamesAdsProvider(
            IYandexGamesInterstitialAd _InterstitialAd,
            IYandexGamesRewardedAd     _RewardedAd,
            IYandexGameActionsProvider _ActionsProvider)
            : base(_InterstitialAd, _RewardedAd)
        {
            ActionsProvider = _ActionsProvider;
        }

        #endregion

        #region api

        public override string Source => AdvertisingNetworks.YandexGames;
        
        #endregion

        #region nonpublic methods

        protected override void InitConfigs(UnityAction _OnSuccess)
        {
            ActionsProvider.OnResolveAuthorization += _OnSuccess;
        }

        #endregion
    }
}
#endif