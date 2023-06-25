using UnityEngine.Events;

namespace Common.Helpers
{
    public interface IYandexGameActionsProvider
    {
        UnityAction         OnResolveAuthorization  { get; set; }
        UnityAction         OnRejectedAuthorization { get; set; }
        UnityAction         OnOpenFullscreenAd      { get; set; }
        UnityAction         OnCloseFullscreenAd     { get; set; }
        UnityAction         OnErrorFullscreenAd     { get; set; }
        UnityAction         OnOpenVideoAd           { get; set; }
        UnityAction         OnCloseVideoAd          { get; set; }
        UnityAction         OnRewardVideoAd         { get; set; }
        UnityAction         OnErrorVideoAd          { get; set; }
        UnityAction<string> OnPurchaseSuccess       { get; set; }
        UnityAction<string> OnPurchaseFailed        { get; set; }
        UnityAction         OnPromptDo              { get; set; }
        UnityAction         OnPromptFail            { get; set; }
        UnityAction         OnReviewDo              { get; set; }
        UnityAction         OnGetPayments           { get; set; }
    }
    
    public class YandexGameActionsProvider : IYandexGameActionsProvider
    {
        public UnityAction         OnResolveAuthorization  { get; set; } = () => { };
        public UnityAction         OnRejectedAuthorization { get; set; } = () => { };
        public UnityAction         OnOpenFullscreenAd      { get; set; } = () => { };
        public UnityAction         OnCloseFullscreenAd     { get; set; } = () => { };
        public UnityAction         OnErrorFullscreenAd     { get; set; } = () => { };
        public UnityAction         OnOpenVideoAd           { get; set; } = () => { };
        public UnityAction         OnCloseVideoAd          { get; set; } = () => { };
        public UnityAction         OnRewardVideoAd         { get; set; } = () => { };
        public UnityAction         OnErrorVideoAd          { get; set; } = () => { };
        public UnityAction<string> OnPurchaseSuccess       { get; set; } = _Id => { };
        public UnityAction<string> OnPurchaseFailed        { get; set; } = _Id => { };
        public UnityAction         OnPromptDo              { get; set; } = () => { };
        public UnityAction         OnPromptFail            { get; set; } = () => { };
        public UnityAction         OnReviewDo              { get; set; } = () => { };
        public UnityAction         OnGetPayments           { get; set; } = () => { };
    }
}