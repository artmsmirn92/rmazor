﻿using System.Collections;
using System.Collections.Generic;
using Common;
using Common.Constants;
using Common.Entities;
using Common.Helpers;
using mazing.common.Runtime;
using mazing.common.Runtime.CameraProviders;
using mazing.common.Runtime.Entities;
using mazing.common.Runtime.Entities.UI;
using mazing.common.Runtime.Enums;
using mazing.common.Runtime.Extensions;
using mazing.common.Runtime.Helpers;
using mazing.common.Runtime.Managers;
using mazing.common.Runtime.Providers;
using mazing.common.Runtime.Ticker;
using mazing.common.Runtime.UI;
using mazing.common.Runtime.Utils;
using RMAZOR.Constants;
using RMAZOR.Managers;
using RMAZOR.Models;
using RMAZOR.Views.Common;
using RMAZOR.Views.InputConfigurators;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RMAZOR.UI.Panels
{
    public interface ICharacterDiedDialogPanel : IDialogPanel
    {
        void ReturnFromShopPanel();
    }
    
    public class CharacterDiedDialogPanel : DialogPanelBase, ICharacterDiedDialogPanel
    {
        #region constants

        private const float CountdownTime = 5f;

        #endregion
        
        #region nonpublic members

        private Animator           
            m_PanelAnimator,
            m_AnimLoadingAds;
        private AnimationTriggerer m_Triggerer;
        private Image
            m_Background,
            m_MoneyInBankIcon,
            m_MoneyIconInPayButton,
            m_IconWatchAds,
            m_Countdown,
            m_CountdownBackground;
        private TextMeshProUGUI    
            m_TextYouHaveMoney,
            m_MoneyInBankText,
            m_TextContinue,
            m_TextPayMoneyCount,
            m_TextRevive;
        private Button
            m_ButtonWatchAds,
            m_ButtonPayMoney;
        private bool
            m_AdsWatched,
            m_MoneyPayed,
            m_PanelShowing,
            m_WentToShopPanel;
        private long   m_MoneyCount;
        private float  m_CountdownValue;

        #endregion

        #region inject

        private GlobalGameSettings                  GlobalGameSettings             { get; }
        private IModelGame                          Model                          { get; }
        private IViewBetweenLevelAdShower           BetweenLevelAdShower           { get; }
        private IViewSwitchLevelStageCommandInvoker SwitchLevelStageCommandInvoker { get; }
        private IFontProvider                       FontProvider                   { get; }

        private CharacterDiedDialogPanel(
            GlobalGameSettings                  _GlobalGameSettings,
            IModelGame                          _Model,
            IManagersGetter                     _Managers,
            IUITicker                           _UITicker,
            ICameraProvider                     _CameraProvider,
            IViewTimePauser                     _TimePauser,
            IColorProvider                      _ColorProvider,
            IViewInputCommandsProceeder         _CommandsProceeder,
            IViewBetweenLevelAdShower           _BetweenLevelAdShower,
            IViewSwitchLevelStageCommandInvoker _SwitchLevelStageCommandInvoker,
            IFontProvider                       _FontProvider)
            : base(
                _Managers,
                _UITicker, 
                _CameraProvider,
                _ColorProvider,
                _TimePauser,
                _CommandsProceeder)
        {
            GlobalGameSettings             = _GlobalGameSettings;
            Model                          = _Model;
            BetweenLevelAdShower           = _BetweenLevelAdShower;
            SwitchLevelStageCommandInvoker = _SwitchLevelStageCommandInvoker;
            FontProvider = _FontProvider;
        }
        
        #endregion
        
        #region api

        public override int      DialogViewerId => DialogViewerIdsCommon.MediumCommon;
        public override Animator Animator       => m_PanelAnimator;

        public override void LoadPanel(RectTransform _Container, ClosePanelAction _OnClose)
        {
            base.LoadPanel(_Container, _OnClose);
            var go = Managers.PrefabSetManager.InitUiPrefab(
                UIUtils.UiRectTransform(
                    _Container,
                    RectTransformLite.FullFill),
                CommonPrefabSetNames.DialogPanels, "character_died_panel");
            PanelRectTransform = go.RTransform();
            PanelRectTransform.SetGoActive(false);
            GetPrefabContentObjects(go);
            LocalizeTextObjectsOnLoad();
            m_Triggerer.Trigger1 = () => Cor.Run(StartCountdown());
            m_ButtonWatchAds.onClick.AddListener(OnWatchAdsButtonClick);
            m_ButtonPayMoney.onClick.AddListener(OnPayMoneyButtonClick);
            var moneyIconSprite = Managers.PrefabSetManager.GetObject<Sprite>(
                "icons", "icon_coin_ui");
            m_MoneyInBankIcon.sprite = moneyIconSprite;
            m_MoneyIconInPayButton.sprite = moneyIconSprite;
            m_Countdown.color = ColorProvider.GetColor(ColorIds.UiBorder);
            m_CountdownBackground.color = Color.black;
        }

        public void ReturnFromShopPanel()
        {
            var savedGameEntity = Managers.ScoreManager.GetSavedGameProgress(
                MazorCommonData.SavedGameFileName, 
                true);
            Cor.Run(Cor.WaitWhile(() => savedGameEntity.Result == EEntityResult.Pending,
                () =>
                {
                    bool castSuccess = savedGameEntity.Value.CastTo(out SavedGame savedGame);
                    if (savedGameEntity.Result == EEntityResult.Fail || !castSuccess)
                    {
                        Dbg.LogError("Failed to load money count entity when " +
                                     "return from shop panel to character died panel");
                        return;
                    }
                    m_MoneyCount = savedGame.Money;
                    m_MoneyInBankText.text = m_MoneyCount.ToString();
                }));
            m_AdsWatched      = false;
            m_MoneyPayed      = false;
            m_WentToShopPanel = false;
            m_PanelShowing    = true;
            Cor.Run(StartCountdown());
        }

        public override void OnDialogStartAppearing()
        {
            m_TextPayMoneyCount.text  = GlobalGameSettings.payToContinueMoneyCount.ToString();
            m_TextPayMoneyCount.font = FontProvider.GetFont(
                ETextType.MenuUI, Managers.LocalizationManager.GetCurrentLanguage());
            TimePauser.PauseTimeInGame();
            m_CountdownValue = 1f;
            m_AdsWatched       = false;
            m_MoneyPayed       = false;
            m_WentToShopPanel  = false;
            m_PanelShowing     = true;
            m_Countdown          .fillAmount = 1f;
            m_CountdownBackground.fillAmount = 1f;
            IndicateAdsLoading(true);
            Cor.Run(Cor.WaitWhile(
                () => !Managers.AdsManager.RewardedAdReady,
                () => IndicateAdsLoading(false),
                () => !m_PanelShowing));
            var savedGameEntity = Managers.ScoreManager.GetSavedGameProgress(
                MazorCommonData.SavedGameFileName, 
                true);
            Cor.Run(Cor.WaitWhile(
                () => savedGameEntity.Result == EEntityResult.Pending,
                () =>
                {
                    bool castSuccess = savedGameEntity.Value.CastTo(out SavedGame savedGame);
                    if (savedGameEntity.Result == EEntityResult.Fail || !castSuccess)
                    {
                        Dbg.LogError("Failed to load money count entity");
                        return;
                    }
                    m_MoneyCount = savedGame.Money;
                    m_MoneyInBankText.text = m_MoneyCount.ToString();
                    SetBankIsLoaded();
                }));
            base.OnDialogStartAppearing();
        }

        public override void OnDialogDisappeared()
        {
            m_PanelShowing = false;
            base.OnDialogDisappeared();
            CommandsProceeder.UnlockCommands(RmazorUtils.MoveAndRotateCommands, "all");
        }
        
        #endregion
        
        #region nonpublic methods

        protected override void OnColorChanged(int _ColorId, Color _Color)
        {
            base.OnColorChanged(_ColorId, _Color);
            if (_ColorId == ColorIds.UiBorder)
                m_Countdown.color = _Color;
        }
        
        private void OnWatchAdsButtonClick()
        {
            Managers.AnalyticsManager.SendAnalytic(AnalyticIdsRmazor.WatchAdInCharacterDiedPanelPressed);
            void OnAdReward()
            {
                m_AdsWatched = true;
            }
            void OnBeforeAdShown()
            {
                TimePauser.PauseTimeInUi();
            }
            void OnAdClosed()
            {
                TimePauser.UnpauseTimeInGame();
                TimePauser.UnpauseTimeInUi();
            }
            Managers.AdsManager.ShowRewardedAd(
                _OnReward:       OnAdReward, 
                _OnBeforeShown:  OnBeforeAdShown,
                _OnClosed:       OnAdClosed,
                _OnFailedToShow: OnAdClosed);
        }

        private void OnPayMoneyButtonClick()
        {
            bool isMoneyEnough = m_MoneyCount >= GlobalGameSettings.payToContinueMoneyCount;
            if (!isMoneyEnough)
            {
                var args = new Dictionary<string, object>
                {
                    {CommonInputCommandArg.KeyLoadShopPanelFromCharacterDiedPanel, true}
                };
                CommandsProceeder.RaiseCommand(EInputCommand.ShopPanel, args, true);
                m_WentToShopPanel = true;
            }
            else
            {
                var savedGame = new SavedGame
                {
                    FileName = MazorCommonData.SavedGameFileName,
                    Money = m_MoneyCount - GlobalGameSettings.payToContinueMoneyCount,
                    Level = Model.LevelStaging.LevelIndex
                };
                Managers.ScoreManager.SaveGameProgress(savedGame, false);
                m_MoneyPayed = true;
            }
        }
        
        private void GetPrefabContentObjects(GameObject _GameObject)
        {
            var go = _GameObject;
            m_PanelAnimator        = go.GetCompItem<Animator>("animator");
            m_Triggerer            = go.GetCompItem<AnimationTriggerer>("triggerer");
            m_ButtonWatchAds       = go.GetCompItem<Button>("watch_ads_button");
            m_ButtonPayMoney       = go.GetCompItem<Button>("pay_money_button");
            m_TextYouHaveMoney     = go.GetCompItem<TextMeshProUGUI>("you_have_text");
            m_MoneyInBankText      = go.GetCompItem<TextMeshProUGUI>("money_count_text");
            m_TextPayMoneyCount    = go.GetCompItem<TextMeshProUGUI>("pay_money_count_text");
            m_TextContinue         = go.GetCompItem<TextMeshProUGUI>("continue_text");
            m_TextRevive           = go.GetCompItem<TextMeshProUGUI>("revive_text");
            m_AnimLoadingAds       = go.GetCompItem<Animator>("loading_ads_anim");
            m_Background           = go.GetCompItem<Image>("background");
            m_MoneyInBankIcon      = go.GetCompItem<Image>("money_icon_1");
            m_MoneyIconInPayButton = go.GetCompItem<Image>("money_icon_2");
            m_IconWatchAds         = go.GetCompItem<Image>("watch_ads_icon");
            m_Countdown            = go.GetCompItem<Image>("round_filled_border");
            m_CountdownBackground  = go.GetCompItem<Image>("round_filled_border_back");
        }
        
        private void LocalizeTextObjectsOnLoad()
        {
            var locMan = Managers.LocalizationManager;
            locMan.AddTextObject(
                new LocalizableTextObjectInfo(m_TextYouHaveMoney, ETextType.MenuUI, "you_have",
                    _T => _T.ToUpper()));
            locMan.AddTextObject(
                new LocalizableTextObjectInfo(m_TextContinue,     ETextType.MenuUI, "continue",
                    _T => _T.ToUpper()));
            locMan.AddTextObject(
                new LocalizableTextObjectInfo(m_TextRevive,       ETextType.MenuUI, "revive?",
                    _T => _T.ToUpper()));
        }
        
        private void IndicateAdsLoading(bool _Indicate)
        {
            if (!m_PanelShowing)
                return;
            m_AnimLoadingAds.SetGoActive(_Indicate);
            m_IconWatchAds.enabled        = !_Indicate;
            m_ButtonWatchAds.interactable = !_Indicate;
        }

        private void SetBankIsLoaded()
        {
            m_MoneyInBankIcon.enabled      = true;
            m_MoneyInBankText.enabled      = true;
            m_MoneyIconInPayButton.enabled = true;
            m_TextPayMoneyCount.enabled    = true;
            m_ButtonPayMoney.interactable  = true;
        }

        private IEnumerator StartCountdown()
        {
            yield return Cor.Lerp(
                Ticker,
                CountdownTime * m_CountdownValue,
                m_CountdownValue,
                0f,
                _P =>
                {
                    (m_CountdownValue, m_Countdown.fillAmount, m_CountdownBackground.fillAmount) = (_P, _P, _P);
                },
                _BreakPredicate: () => m_AdsWatched || m_MoneyPayed || m_WentToShopPanel,
                _OnFinishEx: (_Broken, _) =>
                {
                    if (_Broken && m_WentToShopPanel)
                        return;
                    OnClose(() =>
                    {
                        if (_Broken)
                            RaiseContinueCommand();
                        else 
                            RaiseLoadFirstLevelInGroupCommand();
                    });
                });
        }
        
        private void RaiseLoadFirstLevelInGroupCommand()
        {
            TimePauser.UnpauseTimeInGame();
            TimePauser.UnpauseTimeInUi();
            BetweenLevelAdShower.ShowAdEnabled = false;
            var arguments = new Dictionary<string, object>
            {
                {CommonInputCommandArg.KeySource, CommonInputCommandArg.ParameterCharacterDiedPanel}
            };
            SwitchLevelStageCommandInvoker.SwitchLevelStage(EInputCommand.StartUnloadingLevel, arguments);
        }

        private void RaiseContinueCommand()
        {
            TimePauser.UnpauseTimeInGame();
            TimePauser.UnpauseTimeInUi();
            CommandsProceeder.RaiseCommand(
                EInputCommand.ReadyToStartLevel,
                Model.LevelStaging.Arguments, 
                true);
        }

        #endregion
    }
}