﻿using System.Collections.Generic;
using System.Linq;
using Common.Constants;
using Common.Extensions;
using Common.Helpers;
using mazing.common.Runtime.CameraProviders;
using mazing.common.Runtime.Constants;
using mazing.common.Runtime.Enums;
using mazing.common.Runtime.Extensions;
using mazing.common.Runtime.Helpers;
using mazing.common.Runtime.Managers;
using mazing.common.Runtime.Ticker;
using mazing.common.Runtime.Utils;
using RMAZOR.Managers;
using RMAZOR.Models;
using RMAZOR.Views.Utils;
using Shapes;
using TMPro;
using UnityEngine;

namespace RMAZOR.Views.UI
{
    public interface IViewUILevelsPanel : 
        IOnLevelStageChanged, 
        IInitViewUIItem,
        IViewUIGetRenderers
    {
        void ShowControls(bool _Show, bool _Instantly);
    }
    
    public class ViewUILevelsPanel : IViewUILevelsPanel, IUpdateTick
    {
        #region nonpublic members
        
        private static int AnimKeyCheckMarkPass => AnimKeys.Anim;
        private static int AnimKeyChekMarkSet   => AnimKeys.Anim2;
        private static int AnimKeyCheckMarkIdle => AnimKeys.Stop;
        
        private readonly List<Component> m_Renderers      = new List<Component>();
        private readonly List<Rectangle> m_CheckMarkBoxes = new List<Rectangle>();
        private readonly List<Animator>  m_CheckmarkAnims = new List<Animator>();
        
        private readonly Dictionary<Transform, float> m_LevelPanelItemsFinishPositionsY = 
            new Dictionary<Transform, float>();

        private TextMeshPro m_StageText;
        private TextMeshPro m_LevelText;
        private bool        m_HighlightCurrentLevelBox;
        private int         m_LevelIndexInGroup;
        private float       m_CurrentLevelBoxDashOffset;

        #endregion

        #region inject

        private GlobalGameSettings          GlobalGameSettings { get; }
        private IModelGame                  Model              { get; }
        private ICameraProvider             CameraProvider     { get; }
        private IViewGameTicker             GameTicker         { get; }
        private IManagersGetter             Managers           { get; }
        private IFontProvider               FontProvider       { get; }

        private ViewUILevelsPanel(
            GlobalGameSettings          _GlobalGameSettings,
            IModelGame                  _Model,
            ICameraProvider             _CameraProvider,
            IViewGameTicker             _GameTicker,
            IManagersGetter             _Managers,
            IFontProvider               _FontProvider)
        {
            GlobalGameSettings = _GlobalGameSettings;
            Model             = _Model;
            CameraProvider    = _CameraProvider;
            GameTicker        = _GameTicker;
            Managers          = _Managers;
            FontProvider      = _FontProvider;
        }

        #endregion

        #region api
        
        public void Init(Vector4 _Offsets)
        {
            GameTicker.Register(this);
            InitLevelPanel();
            InitCheckMarks();
            ActivatePanel(false);
            CameraProvider.ActiveCameraChanged += OnActiveCameraChanged;
            Managers.LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        public IEnumerable<Component> GetRenderers()
        {
            return m_Renderers;
        }
        
        public void OnLevelStageChanged(LevelStageArgs _Args)
        {
            switch (_Args.LevelStage)
            {
                case ELevelStage.None:
                    ActivatePanel(false);
                    break;
                case ELevelStage.Loaded:
                    ActivatePanel(true);
                    EnableCurrentLevelBoxHighlighting(_Args);
                    break;
                case ELevelStage.Finished when _Args.PreviousStage != ELevelStage.Paused:
                    SetLevelCheckMarks(true);    
                    break;
            }
        }
        
        public void ShowControls(bool _Show, bool _Instantly)
        {
            if (!_Show)
                return;
            SetStageAndLevelTexts();
        }
        
        public void UpdateTick()
        {
            if (!m_HighlightCurrentLevelBox)
                return;
            m_CurrentLevelBoxDashOffset += GameTicker.DeltaTime * 0.3f;
            m_CurrentLevelBoxDashOffset = MathUtils.ClampInverse(m_CurrentLevelBoxDashOffset, 0f, 10f);
            m_CheckMarkBoxes[m_LevelIndexInGroup].SetDashOffset(m_CurrentLevelBoxDashOffset);
        }

        #endregion

        #region nonpublic methods
        
        private void OnLanguageChanged(ELanguage _Language)
        {
            SetStageAndLevelTexts();
        }
        
        private void OnActiveCameraChanged(Camera _Camera)
        {
            var parent = _Camera.transform;
            foreach (var transform in m_LevelPanelItemsFinishPositionsY.Keys)
            {
                float yPos = m_LevelPanelItemsFinishPositionsY[transform];
                transform.SetParentEx(parent).SetLocalPosY(yPos).SetLocalPosZ(10f);
            }
            var screenBounds = GraphicUtils.GetVisibleBounds(CameraProvider.Camera);
            m_StageText.transform.SetLocalPosX(screenBounds.center.x);
            m_LevelText.transform.SetLocalPosX(screenBounds.center.x);
            UpdateCheckMarks(_Camera);
        }
        
        private void ActivatePanel(bool _Activate)
        {
            m_LevelText.enabled = _Activate;
            m_StageText.enabled = _Activate;
            foreach (var checkMarkAnim in m_CheckmarkAnims)
                checkMarkAnim.SetGoActive(_Activate);
            foreach (var checkMarkBox in m_CheckMarkBoxes)
                checkMarkBox.SetGoActive(_Activate);
        }

        private void SetStageAndLevelTexts()
        {
            var locMan = Managers.LocalizationManager;
            var font = FontProvider.GetFont(ETextType.GameUI, locMan.GetCurrentLanguage());
            m_StageText.font = m_LevelText.font = font;
            string key = Model.LevelStaging.LevelStage == ELevelStage.Loaded
                ? CommonInputCommandArg.KeyNextLevelType
                : CommonInputCommandArg.KeyCurrentLevelType;
            string nextLevelType = (string)Model.LevelStaging.Arguments.GetSafe(key, out _);
            bool isNextLevelBonus = nextLevelType == CommonInputCommandArg.ParameterLevelTypeBonus;
            int levelsGroupIdx = isNextLevelBonus ?
                (int)Model.LevelStaging.LevelIndex * GlobalGameSettings.extraLevelEveryNStage + 1 
                : RmazorUtils.GetLevelsGroupIndex(Model.LevelStaging.LevelIndex);
            m_StageText.text = locMan.GetTranslation("stage") + " " + levelsGroupIdx;
            m_LevelText.text = isNextLevelBonus ?
                locMan.GetTranslation("extra_level")
                : locMan.GetTranslation("level") + " " + (Model.LevelStaging.LevelIndex + 1);
        }
        
        private void InitLevelPanel()
        {
            var screenBounds = GraphicUtils.GetVisibleBounds(CameraProvider.Camera);
            var parent = CameraProvider.Camera.transform;
            var goLevelText = Managers.PrefabSetManager.InitPrefab(
                parent, CommonPrefabSetNames.UiGame, "level_text");
            goLevelText.transform.SetLocalPosXY(Vector2.zero).SetLocalScaleXY(Vector2.one);
            m_StageText = goLevelText.GetCompItem<TextMeshPro>("stage_text");
            m_LevelText = goLevelText.GetCompItem<TextMeshPro>("level_text");
            m_StageText.sortingOrder = SortingOrders.GameUI;
            m_LevelText.sortingOrder = SortingOrders.GameUI;
            m_Renderers.AddRange(new [] {m_StageText, m_LevelText});
            m_StageText.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, screenBounds.size.x * 0.5f);
            m_LevelText.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, screenBounds.size.x * 0.5f);
            float yPos = screenBounds.max.y - 5f;
            m_StageText.transform
                .SetLocalPosX(screenBounds.center.x)
                .SetLocalPosY(yPos)
                .SetLocalPosZ(10f);
            m_LevelText.transform
                .SetLocalPosX(screenBounds.center.x)
                .SetLocalPosY(yPos - 2.5f)
                .SetLocalPosZ(10f);
            m_LevelPanelItemsFinishPositionsY.Add(m_StageText.transform, yPos);
            m_LevelPanelItemsFinishPositionsY.Add(m_LevelText.transform, yPos - 2.5f);
        }
        
        private void InitCheckMarks()
        {
            var screenBounds = GraphicUtils.GetVisibleBounds(CameraProvider.Camera);
            var goCheckMark = Managers.PrefabSetManager.GetPrefab(
                CommonPrefabSetNames.UiGame, "level_check_mark");
            float yPos = screenBounds.max.y - 8.5f;
            var parent = CameraProvider.Camera.transform;
            for (int i = 0; i < 10; i++)
            {
                var go = Object.Instantiate(goCheckMark, parent);
                m_CheckmarkAnims.Add(go.GetCompItem<Animator>("animator"));
                m_Renderers.AddRange(new Component[]
                {
                    go.GetCompItem<Rectangle>("body")  .SetSortingOrder(SortingOrders.GameUI),
                    go.GetCompItem<Line>("checkmark_1").SetSortingOrder(SortingOrders.GameUI),
                    go.GetCompItem<Line>("checkmark_2").SetSortingOrder(SortingOrders.GameUI)
                });
                m_CheckMarkBoxes.Add(go.GetCompItem<Rectangle>("body"));
                go.transform
                    .SetLocalPosX(screenBounds.center.x)
                    .SetLocalPosY(yPos)
                    .SetLocalPosZ(10f);
                m_LevelPanelItemsFinishPositionsY.Add(go.transform, yPos);
                var triggerer = go.GetCompItem<AnimationTriggerer>("triggerer");
                triggerer.Trigger1 = DisableCurrentLevelBoxHighlighting;
            }
            goCheckMark.DestroySafe();
        }
        
        private void UpdateCheckMarks(Camera _Camera)
        {          
            foreach (var checkmarkAnim in m_CheckmarkAnims)
                checkmarkAnim.gameObject.SetActive(false);
            var screenBounds = GraphicUtils.GetVisibleBounds(_Camera);
            int groupIndex = RmazorUtils.GetLevelsGroupIndex(Model.LevelStaging.LevelIndex);
            string nextLevelType = (string)Model.LevelStaging.Arguments.GetSafe(
                CommonInputCommandArg.KeyNextLevelType, out _);
            bool isNextLevelBonus = nextLevelType == CommonInputCommandArg.ParameterLevelTypeBonus;
            int checkmarksCount = isNextLevelBonus ?
                1 :  RmazorUtils.GetLevelsInGroup(groupIndex);
            float yPos = screenBounds.max.y - 8.5f;
            for (int i = 0; i < checkmarksCount; i++)
            {
                var checkmarkAnim = m_CheckmarkAnims[i];
                checkmarkAnim.gameObject.SetActive(true);
                float i1 = i + 0.5f;
                const float c = 3f;
                float xIndent = (i1 - checkmarksCount * 0.5f) * c;
                checkmarkAnim.transform
                    .SetLocalPosX(screenBounds.center.x + xIndent)
                    .SetLocalPosY(yPos);
            }
            SetLevelCheckMarks(false);
        }
        
        private void SetLevelCheckMarks(bool _LevelPassedJustNow)
        {
            for (int i = 0; i < m_CheckmarkAnims.Count; i++)
                m_CheckmarkAnims[i].SetTrigger(AnimKeyCheckMarkIdle);
            string key = Model.LevelStaging.LevelStage == ELevelStage.Loaded
                ? CommonInputCommandArg.KeyNextLevelType
                : CommonInputCommandArg.KeyCurrentLevelType;
            string nextLevelType = (string)Model.LevelStaging.Arguments.GetSafe(
                key, out _);
            bool isNextLevelBonus = nextLevelType == CommonInputCommandArg.ParameterLevelTypeBonus;
            if (isNextLevelBonus)
            {
                if (_LevelPassedJustNow)
                    m_CheckmarkAnims[0].SetTrigger(AnimKeyCheckMarkPass);
                return;
            }
            int lastPassedLevelInGroupIndex = RmazorUtils.GetIndexInGroup(Model.LevelStaging.LevelIndex);
            for (int i = 0; i < lastPassedLevelInGroupIndex; i++)
                m_CheckmarkAnims[i].SetTrigger(AnimKeyChekMarkSet);
            m_CheckmarkAnims[lastPassedLevelInGroupIndex].SetTrigger(
                _LevelPassedJustNow ? AnimKeyCheckMarkPass : AnimKeyCheckMarkIdle);
        }

        private void EnableCurrentLevelBoxHighlighting(LevelStageArgs _Args)
        {
            foreach (var box in m_CheckMarkBoxes)
                box.SetDashed(false).SetDashOffset(0f);
            string nextLevelType = (string)Model.LevelStaging.Arguments.GetSafe(
                CommonInputCommandArg.KeyNextLevelType, out _);
            bool isNextLevelBonus = nextLevelType == CommonInputCommandArg.ParameterLevelTypeBonus;
            m_LevelIndexInGroup = isNextLevelBonus ? 0 : RmazorUtils.GetIndexInGroup(_Args.LevelIndex);
            m_HighlightCurrentLevelBox = true;
            m_CheckMarkBoxes[m_LevelIndexInGroup].SetDashed(true)
                .SetDashSize(8f)
                .SetDashType(DashType.Rounded)
                .SetDashSpace(DashSpace.FixedCount)
                .SetMatchDashSpacingToDashSize(false)
                .SetDashSpacing(0.3f);
        }

        private void DisableCurrentLevelBoxHighlighting()
        {
            Cor.Run(Cor.Lerp(
                GameTicker,
                1f / m_CheckmarkAnims.First().speed,
                _OnProgress: _P =>
                {
                    m_CheckMarkBoxes[m_LevelIndexInGroup].SetDashSpacing(0.3f * (1 - _P));
                },
                _OnFinish: () =>
                {
                    m_HighlightCurrentLevelBox = false;
                    m_CheckMarkBoxes[m_LevelIndexInGroup]
                        .SetDashed(false)
                        .SetDashOffset(m_CurrentLevelBoxDashOffset = 0f);
                }));
        }

        #endregion
    }
}