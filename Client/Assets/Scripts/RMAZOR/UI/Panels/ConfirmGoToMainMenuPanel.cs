﻿using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using mazing.common.Runtime.CameraProviders;
using mazing.common.Runtime.Entities;
using mazing.common.Runtime.Enums;
using mazing.common.Runtime.Extensions;
using mazing.common.Runtime.Providers;
using mazing.common.Runtime.Ticker;
using mazing.common.Runtime.UI;
using mazing.common.Runtime.Utils;
using RMAZOR.Managers;
using RMAZOR.Models;
using RMAZOR.UI.Utils;
using RMAZOR.Views.Common;
using RMAZOR.Views.Common.ViewLevelStageSwitchers;
using RMAZOR.Views.InputConfigurators;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static RMAZOR.Models.ComInComArg;

namespace RMAZOR.UI.Panels
{
    public interface IConfirmGoToMainMenuPanel : IDialogPanel { }
    
    public class ConfirmGoToMainMenuPanelFake : DialogPanelFake, IConfirmGoToMainMenuPanel { }
     
    public class ConfirmGoToMainMenuPanel : DialogPanelBase, IConfirmGoToMainMenuPanel
    {
        #region nonpublic members
        
        private TextMeshProUGUI 
            m_TitleText, 
            m_ButtonYesText, 
            m_ButtonNoText;
        private Button
            m_ButtonYes,
            m_ButtonNo;
        
        protected override string PrefabName => "confirm_go_to_main_menu_panel";
        
        #endregion
        
        #region inject

        private ViewSettings                ViewSettings           { get; }
        private IViewFullscreenTransitioner FullscreenTransitioner { get; }
        private IViewLevelStageSwitcher     LevelStageSwitcher     { get; }

        private ConfirmGoToMainMenuPanel(
            ViewSettings                _ViewSettings,
            IViewFullscreenTransitioner _FullscreenTransitioner,
            IManagersGetter             _Managers,
            IUITicker                   _Ticker,
            ICameraProvider             _CameraProvider,
            IColorProvider              _ColorProvider,
            IViewTimePauser             _TimePauser,
            IViewInputCommandsProceeder _CommandsProceeder,
            IViewLevelStageSwitcher     _LevelStageSwitcher)
            : base(
                _Managers,
                _Ticker,
                _CameraProvider, 
                _ColorProvider,
                _TimePauser,
                _CommandsProceeder)
        {
            ViewSettings           = _ViewSettings;
            FullscreenTransitioner = _FullscreenTransitioner;
            LevelStageSwitcher     = _LevelStageSwitcher;
        }

        #endregion

        #region api

        public override int DialogViewerId => DialogViewerIdsCommon.MediumCommon;

        #endregion

        #region nonpublic methods

        protected override void GetPrefabContentObjects(GameObject _Go)
        {
            m_ButtonYes     = _Go.GetCompItem<Button>("button_yes");
            m_ButtonNo      = _Go.GetCompItem<Button>("button_no");
            m_TitleText     = _Go.GetCompItem<TextMeshProUGUI>("title_text");
            m_ButtonYesText = _Go.GetCompItem<TextMeshProUGUI>("button_yes_text");
            m_ButtonNoText  = _Go.GetCompItem<TextMeshProUGUI>("button_no_text");
        }

        protected override void LocalizeTextObjectsOnLoad()
        {
            static string TextFormula(string _Text) => _Text.ToUpper(CultureInfo.CurrentUICulture);
            var locTextInfos = new[]
            {
                new LocTextInfo(m_TitleText,     ETextType.MenuUI_H1, "go_to_main_menu_question"),
                new LocTextInfo(m_ButtonNoText,  ETextType.MenuUI_H1, "back", TextFormula),
                new LocTextInfo(m_ButtonYesText, ETextType.MenuUI_H1, "yes", TextFormula)
            };
            foreach (var locTextInfo in locTextInfos)
                Managers.LocalizationManager.AddLocalization(locTextInfo);
        }

        protected override void SubscribeButtonEvents()
        {
            m_ButtonYes.onClick.AddListener(OnButtonYesClick);
            m_ButtonNo.onClick.AddListener(OnButtonNoClick);
        }

        private void OnButtonYesClick()
        {
            OnClose(LoadMainMenu);
        }

        private void OnButtonNoClick()
        {
            OnClose(() =>
            {
                LevelStageSwitcher.SwitchLevelStage(EInputCommand.UnPauseLevel);
            });
        }

        private void LoadMainMenu()
        {
            Cor.Run(LoadMainMenuCoroutine());
            PlayButtonClickSound();
        }
        
        private IEnumerator LoadMainMenuCoroutine()
        {
            FullscreenTransitioner.DoTextureTransition(true, ViewSettings.betweenLevelTransitionTime);
            yield return Cor.Delay(ViewSettings.betweenLevelTransitionTime, Ticker);
            var args = new Dictionary<string, object>
            {
                {KeyAdditionalCameraEffectAction, 
                    (UnityAction<bool, float>)AdditionalCameraEffectsActionDefaultCoroutine}
            };
            CallCommand(EInputCommand.MainMenuPanel, args);
            FullscreenTransitioner.DoTextureTransition(false, ViewSettings.betweenLevelTransitionTime);
            yield return Cor.Delay(ViewSettings.betweenLevelTransitionTime, Ticker);
            FullscreenTransitioner.Enabled = false;
        }
        
        private void AdditionalCameraEffectsActionDefaultCoroutine(bool _Appear, float _Time)
        {
            Cor.Run(MainMenuUtils.SubPanelsAdditionalCameraEffectsActionCoroutine(_Appear, _Time,
                CameraProvider, Ticker));
        }
        
        private void CallCommand(EInputCommand _Command, Dictionary<string, object> _Args = null)
        {
            CommandsProceeder.RaiseCommand(_Command, _Args);
        }

        #endregion
    }
}