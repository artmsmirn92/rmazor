#if YANDEX_GAMES
using System.Collections;
using mazing.common.Runtime;
using mazing.common.Runtime.Extensions;
using mazing.common.Runtime.Helpers;
using mazing.common.Runtime.Utils;
using YG;

namespace Common.Helpers
{
    public interface IYandexGameFacade : IInit
    {
        YandexGame.JsonEnvironmentData GetEnvironmentData();
        
        void     GetPaymentsDec();
        Purchase GetPurchaseById(string _Id);
        void     BuyPayments(string     _Id);
        void     ReviewShowDec(bool     _AuthDialog);
        void     RewVideoShowDec(int    _Id);
        void     FullscreenShowDec();
        void     NewLeaderboardScoresDec(string _Id, int _Score);
    }
    
    public class YandexGameFacade : InitBase, IYandexGameFacade
    {
        #region inject

        private IYandexGameActionsProvider ActionsProvider { get; }
        
        private YandexGameFacade(IYandexGameActionsProvider _ActionsProvider)
        {
            ActionsProvider = _ActionsProvider;
        }

        #endregion

        #region api

        public override void Init()
        {
            if (Initialized)
                return;
            Cor.Run(SubscribeEventsCoroutine());
            base.Init();
        }

        public YandexGame.JsonEnvironmentData GetEnvironmentData()        => YandexGame.EnvironmentData;

        public Purchase                       GetPurchaseById(string _Id) => YandexGame.PurchaseByID(_Id);

        public void GetPaymentsDec()        => YandexGame.GetPayments();
        public void BuyPayments(string _Id) => YandexGame.BuyPayments(_Id);
        
        public void ReviewShowDec(bool  _AuthDialog) => YandexGame.ReviewShow(_AuthDialog);
        public void RewVideoShowDec(int _Id)         => YandexGame.RewVideoShow(_Id);
        public void FullscreenShowDec()              => YandexGame.FullscreenShow();
        
        public void NewLeaderboardScoresDec(string _Id, int _Score) => YandexGame.NewLeaderboardScores(_Id, _Score);

        #endregion

        #region nonpublic members

        private IEnumerator SubscribeEventsCoroutine()
        {
            while (YandexGame.Instance.IsNull())
                yield return null;
            YandexGame.Instance.ResolvedAuthorization.AddListener(OnResolvedAuthorization);
            YandexGame.Instance.RejectedAuthorization.AddListener(OnRejectedAuthorization);
            YandexGame.Instance.ResolvedAuthorization.AddListener(ActionsProvider.OnResolveAuthorization);
            YandexGame.Instance.RejectedAuthorization.AddListener(ActionsProvider.OnRejectedAuthorization);
            YandexGame.Instance.OpenFullscreenAd     .AddListener(ActionsProvider.OnOpenFullscreenAd);
            YandexGame.Instance.CloseFullscreenAd    .AddListener(ActionsProvider.OnCloseFullscreenAd);
            YandexGame.Instance.ErrorFullscreenAd    .AddListener(ActionsProvider.OnErrorFullscreenAd);
            YandexGame.Instance.OpenVideoAd          .AddListener(ActionsProvider.OnOpenVideoAd);
            YandexGame.Instance.CloseVideoAd         .AddListener(ActionsProvider.OnCloseVideoAd);
            YandexGame.Instance.RewardVideoAd        .AddListener(ActionsProvider.OnRewardVideoAd);
            YandexGame.Instance.ErrorVideoAd         .AddListener(ActionsProvider.OnErrorVideoAd);
            YandexGame.Instance.PromptDo             .AddListener(ActionsProvider.OnPromptDo);
            YandexGame.Instance.PromptFail           .AddListener(ActionsProvider.OnPromptFail);
            YandexGame.Instance.ReviewDo             .AddListener(ActionsProvider.OnReviewDo);
            YandexGame.GetPaymentsEvent     += () => ActionsProvider.OnGetPayments?.Invoke();
            YandexGame.PurchaseSuccessEvent += _Id => ActionsProvider.OnPurchaseSuccess?.Invoke(_Id);
            YandexGame.PurchaseFailedEvent  += _Id => ActionsProvider.OnPurchaseFailed?.Invoke(_Id);
        }

        private static void OnResolvedAuthorization()
        {
            Dbg.Log("Yandex Games initialization success!");
        }

        private static void OnRejectedAuthorization()
        {
            Dbg.LogError("Yandex Games initialization fail!");
            
        }

        #endregion
    }
}
#endif