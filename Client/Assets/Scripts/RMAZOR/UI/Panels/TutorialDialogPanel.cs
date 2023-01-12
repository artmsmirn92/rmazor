﻿using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Constants;
using mazing.common.Runtime.CameraProviders;
using mazing.common.Runtime.Constants;
using mazing.common.Runtime.Entities.UI;
using mazing.common.Runtime.Enums;
using mazing.common.Runtime.Extensions;
using mazing.common.Runtime.Helpers;
using mazing.common.Runtime.Managers;
using mazing.common.Runtime.Providers;
using mazing.common.Runtime.Ticker;
using mazing.common.Runtime.UI;
using mazing.common.Runtime.Utils;
using RMAZOR.Managers;
using RMAZOR.Models;
using RMAZOR.Views.Common;
using RMAZOR.Views.InputConfigurators;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace RMAZOR.UI.Panels
{
    public class TutorialDialogPanelInfo
    {
        public string TutorialName               { get; }
        public string TitleLocalizationKey       { get; }
        public string DescriptionLocalizationKey { get; }
        public string VideoClipAssetKey          { get; }
        
        public TutorialDialogPanelInfo(
            string _TutorialName,
            string _TitleLocalizationKey, 
            string _DescriptionLocalizationKey,
            string _VideoClipAssetKey)
        {
            TutorialName               = _TutorialName;
            TitleLocalizationKey       = _TitleLocalizationKey;
            DescriptionLocalizationKey = _DescriptionLocalizationKey;
            VideoClipAssetKey          = _VideoClipAssetKey;
        }
    }
    
    public interface ITutorialDialogPanel : IDialogPanel
    {
        bool IsVideoReady { get; }
        void SetPanelInfo(TutorialDialogPanelInfo _Info);
        void PrepareVideo();
    }
    
    public class TutorialDialogPanel : DialogPanelBase, ITutorialDialogPanel
    {
        #region constants

        private const int CharacterSpritesCount = 5;

        #endregion
        
        #region nonpublic members

        private TutorialDialogPanelInfo m_Info;
        private TextMeshProUGUI         m_Title;
        private TextMeshProUGUI         m_Description;
        private VideoPlayer             m_VideoPlayer;
        private Button                  m_ButtonClose;
        private Animator                m_Animator;
        private Image                   m_CharacterIcon;
        private readonly List<Sprite>   m_CharacterSprites = new List<Sprite>();

        #endregion

        #region inject
        
        private IContainersGetter                   ContainersGetter               { get; }
        private IViewSwitchLevelStageCommandInvoker SwitchLevelStageCommandInvoker { get; }
        private IFontProvider                       FontProvider                   { get; }

        public TutorialDialogPanel(
            IManagersGetter                     _Managers,
            IUITicker                           _Ticker,
            ICameraProvider                     _CameraProvider,
            IColorProvider                      _ColorProvider,
            IViewTimePauser                     _TimePauser,
            IContainersGetter                   _ContainersGetter,
            IViewInputCommandsProceeder         _CommandsProceeder,
            IViewSwitchLevelStageCommandInvoker _SwitchLevelStageCommandInvoker,
            IFontProvider                       _FontProvider) 
            : base(
                _Managers, 
                _Ticker,
                _CameraProvider,
                _ColorProvider,
                _TimePauser,
                _CommandsProceeder)
        {
            ContainersGetter               = _ContainersGetter;
            SwitchLevelStageCommandInvoker = _SwitchLevelStageCommandInvoker;
            FontProvider                   = _FontProvider;
        }

        #endregion

        #region api

        public override int      DialogViewerId => DialogViewerIdsCommon.FullscreenCommon;
        public override Animator Animator       => m_Animator;

        public bool IsVideoReady => m_VideoPlayer.IsNotNull() && m_VideoPlayer.isPlaying;

        public void SetPanelInfo(TutorialDialogPanelInfo _Info)
        {
            m_Info = _Info;
        }

        public void PrepareVideo()
        {
            if (m_VideoPlayer.IsNull())
                InitVideoPlayer();
            m_VideoPlayer.clip = Managers.PrefabSetManager.GetObject<VideoClip>(
                "tutorial_clips", m_Info.VideoClipAssetKey);
            m_VideoPlayer.enabled = true;
            m_VideoPlayer.Play();
        }

        public override void LoadPanel(RectTransform _Container, ClosePanelAction _OnClose)
        {
            base.LoadPanel(_Container, _OnClose);
            var go = Managers.PrefabSetManager.InitUiPrefab(
                UIUtils.UiRectTransform(
                    _Container,
                    RectTransformLite.FullFill),
                CommonPrefabSetNames.DialogPanels,
                "tutorial_panel");
            PanelRectTransform = go.RTransform();
            var panel = go.GetCompItem<SimpleUiDialogPanelView>("panel");
            panel.Init(Ticker, Managers.AudioManager, Managers.LocalizationManager);
            m_Title         = go.GetCompItem<TextMeshProUGUI>("title");
            m_Description   = go.GetCompItem<TextMeshProUGUI>("description");
            m_ButtonClose   = go.GetCompItem<Button>("button_close");
            m_Animator      = go.GetCompItem<Animator>("animator");
            m_CharacterIcon = go.GetCompItem<Image>("character_icon");
            m_ButtonClose.onClick.AddListener(OnCloseButtonClick);
            for (int i = 1; i <= CharacterSpritesCount; i++)
            {
                var charSprite = Managers.PrefabSetManager.GetObject<Sprite>(
                    CommonPrefabSetNames.Views, $"tutorial_character_sprite_{i}");
                m_CharacterSprites.Add(charSprite);
            }
        }

        public override void OnDialogStartAppearing()
        {
            TimePauser.PauseTimeInGame();
            var font =  FontProvider.GetFont(ETextType.MenuUI, Managers.LocalizationManager.GetCurrentLanguage());
            m_Title.font = m_Description.font = font;
            m_Title.text = m_Description.text = string.Empty;
            int currentTutorialNumber = GetNumberOfFinishedTutorials() + 1;
            m_CharacterIcon.sprite = m_CharacterSprites[currentTutorialNumber % CharacterSpritesCount];
            base.OnDialogStartAppearing();
        }

        public override void OnDialogAppeared()
        {
            base.OnDialogAppeared();
            var locMan = Managers.LocalizationManager;
            string titleTextLocalized = locMan.GetTranslation(m_Info.TitleLocalizationKey)
                .FirstCharToUpper(CultureInfo.CurrentUICulture);
            string descriptionTextLocalized = locMan.GetTranslation(m_Info.DescriptionLocalizationKey)
                .FirstCharToUpper(CultureInfo.CurrentUICulture);
            string tutorialWordLocalized = locMan.GetTranslation("tutorial")
                .FirstCharToUpper(CultureInfo.CurrentUICulture);
            int currentTutorialNumber = GetNumberOfFinishedTutorials() + 1;
            titleTextLocalized = $"{tutorialWordLocalized} #{currentTutorialNumber}: {titleTextLocalized}";
            Cor.Run(PrintTutorialTextCoroutine(titleTextLocalized, descriptionTextLocalized));
        }

        public override void OnDialogDisappeared()
        {
            if (m_Info.TutorialName != "movement")
            {
                var saveKeyCurrentTutorial = SaveKeysRmazor.IsTutorialFinished(m_Info.TutorialName);
                SaveUtils.PutValue(saveKeyCurrentTutorial, true);
            }
            TimePauser.UnpauseTimeInGame();
            m_VideoPlayer.Stop();
            m_VideoPlayer.clip = null;
            m_VideoPlayer.enabled = false;
            SwitchLevelStageCommandInvoker.SwitchLevelStage(EInputCommand.UnPauseLevel);
            base.OnDialogDisappeared();
        }

        #endregion

        #region nonpublic methods

        private void OnCloseButtonClick()
        {
            OnClose(null);
        }

        private void InitVideoPlayer()
        {
            var vpParent = ContainersGetter.GetContainer(ContainerNamesCommon.Common);
            var vpGo = Managers.PrefabSetManager.InitPrefab(
                vpParent, "tutorials", "tutorial_video_player");
            vpGo.transform.SetLocalPosXY(Vector2.left * 1e3f);
            m_VideoPlayer = vpGo.GetCompItem<VideoPlayer>("video_player");
            m_VideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        private IEnumerator PrintTutorialTextCoroutine(string _Title, string _Description)
        {
            var titleCharArray = _Title.ToCharArray();
            var sb = new StringBuilder(titleCharArray.Length);
            float printTime = _Title.Length * 0.05f;
            yield return Cor.Lerp(Ticker, printTime, _OnProgress: _P =>
            {
                sb.Clear();
                int textLength = Mathf.RoundToInt(titleCharArray.Length * _P);
                for (int i = 0; i < textLength; i++)
                    sb.Append(titleCharArray[i]);
                m_Title.text = sb.ToString();
            });
            var descriptionCharArray = _Description.ToCharArray();
            printTime = _Description.Length * 0.05f;
            yield return Cor.Lerp(Ticker, printTime, _OnProgress: _P =>
            {
                sb.Clear();
                int textLength = Mathf.RoundToInt(descriptionCharArray.Length * _P);
                for (int i = 0; i < textLength; i++)
                    sb.Append(descriptionCharArray[i]);
                m_Description.text = sb.ToString();
            });
        }

        private static int GetNumberOfFinishedTutorials()
        {
            return SaveKeysRmazor.GetAllTutorialNames()
                .Select(SaveKeysRmazor.IsTutorialFinished)
                .Select(SaveUtils.GetValue)
                .Count(_TutorialFinished => _TutorialFinished);
        }
        
        
        #endregion
    }
}