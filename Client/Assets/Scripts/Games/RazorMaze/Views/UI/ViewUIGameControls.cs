﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Constants;
using DI.Extensions;
using DialogViewers;
using Entities;
using Exceptions;
using GameHelpers;
using Games.RazorMaze.Models;
using Games.RazorMaze.Models.ItemProceeders;
using Games.RazorMaze.Views.Common;
using Games.RazorMaze.Views.ContainerGetters;
using Games.RazorMaze.Views.Helpers;
using Games.RazorMaze.Views.InputConfigurators;
using Shapes;
using Ticker;
using TMPro;
using UnityEngine;
using Utils;

namespace Games.RazorMaze.Views.UI
{
    public interface IViewUIGameControls : IInit, IOnLevelStageChanged
    {
        void OnMazeItemMoveStarted(MazeItemMoveEventArgs _Args);
        void OnMazeItemMoveFinished(MazeItemMoveEventArgs _Args);

    }
    
    public class ViewUIGameControls : ViewUIGameControlsBase
    {
        #region nonpublic members

        private static int AnimKeyCheckMarkPass      => AnimKeys.Anim;
        private static int AnimKeyChekMarkSet        => AnimKeys.Anim2;
        private static int AnimKeyCheckMarkIdle      => AnimKeys.Stop;
        private static int AnimKeyCongratsAnim       => AnimKeys.Anim;
        private static int AnimKeyCongratsIdle       => AnimKeys.Stop;
        private static int AnimKeyStartLogoAppear    => AnimKeys.Anim;
        private static int AnimKeyStartLogoDisappear => AnimKeys.Stop;
        private static int AnimKeyStartLogoHide      => AnimKeys.Stop2;

        private          ButtonOnRaycast              m_RotateClockwiseButton;
        private          ButtonOnRaycast              m_RotateCounterClockwiseButton;
        private          ButtonOnRaycast              m_ShopButton;
        private          ButtonOnRaycast              m_SettingsButton;
        private          TextMeshPro                  m_LevelText;
        private          TextMeshPro                  m_CompletedText;
        private          TextMeshPro                  m_CongratsText;
        private          Line                         m_CongratsLine;
        private          Animator                     m_CongratsAnim;
        private readonly List<Component>              m_Renderers  = new List<Component>();
        private readonly List<Animator>               m_CheckMarks = new List<Animator>();
        private          bool                         m_GameUiInitialized;
        private          Dictionary<string, Animator> m_StartLogoCharAnims;
        private          bool                         m_OnStart              = true;
        private readonly List<Component>              m_RotatingButtonShapes = new List<Component>();
        private readonly List<Component>              m_RotatingButtonShapes2 = new List<Component>();

        #endregion
        
        #region inject

        private ViewSettings             ViewSettings        { get; }
        private IViewUIPrompt            Prompt              { get; }
        private IModelGame               Model               { get; }
        private IContainersGetter        ContainersGetter    { get; }
        private IMazeCoordinateConverter CoordinateConverter { get; }
        private IViewInputTouchProceeder TouchProceeder      { get; }
        private IViewAppearTransitioner  AppearTransitioner  { get; }
        private IViewGameTicker              GameTicker          { get; }
        private ILevelsLoader            LevelsLoader        { get; }
        private ICameraProvider          CameraProvider      { get; }
        private IColorProvider           ColorProvider       { get; }
        private IManagersGetter          Managers            { get; }
        private IBigDialogViewer         BigDialogViewer     { get; }

        public ViewUIGameControls(
            ViewSettings _ViewSettings,
            IViewUIPrompt _Prompt,
            IModelGame _Model,
            IContainersGetter _ContainersGetter,
            IMazeCoordinateConverter _CoordinateConverter,
            IViewInputCommandsProceeder _CommandsProceeder,
            IViewInputTouchProceeder _TouchProceeder,
            IViewAppearTransitioner _AppearTransitioner,
            IViewGameTicker _GameTicker,
            ILevelsLoader _LevelsLoader,
            ICameraProvider _CameraProvider,
            IColorProvider _ColorProvider,
            IManagersGetter _Managers,
            IBigDialogViewer _BigDialogViewer) 
            : base(_CommandsProceeder)
        {
            ViewSettings = _ViewSettings;
            Prompt = _Prompt;
            Model = _Model;
            ContainersGetter = _ContainersGetter;
            CoordinateConverter = _CoordinateConverter;
            TouchProceeder = _TouchProceeder;
            AppearTransitioner = _AppearTransitioner;
            GameTicker = _GameTicker;
            LevelsLoader = _LevelsLoader;
            CameraProvider = _CameraProvider;
            ColorProvider = _ColorProvider;
            Managers = _Managers;
            BigDialogViewer = _BigDialogViewer;
        }

        #endregion
        
        #region api
        
        public override void OnMazeItemMoveStarted(MazeItemMoveEventArgs _Args)
        {
            if (Prompt.InTutorial)
                return;
            base.OnMazeItemMoveStarted(_Args);
        }

        public override void OnMazeItemMoveFinished(MazeItemMoveEventArgs _Args)
        {
            if (Prompt.InTutorial)
                return;
            base.OnMazeItemMoveFinished(_Args);
        }
        
        public override void OnLevelStageChanged(LevelStageArgs _Args)
        {
            if (!m_GameUiInitialized)
            {
                Prompt.Init();
                InitGameUI();
                m_GameUiInitialized = true;
            }
            
            ShowRotatingButtons(_Args);
            ShowStartLogo(_Args);
            LockCommands(_Args);
            
            switch (_Args.Stage)
            {
                case ELevelStage.Loaded:
                    ConsiderCongratsPanelWhileAppearing(false);
                    ShowCongratsPanel(false);
                    SetLevelCheckMarks(_Args.LevelIndex, false);
                    ShowControls(true, false);
                    break;
                case ELevelStage.ReadyToStart:
                case ELevelStage.StartedOrContinued:
                    bool enableRotation = Model.GetAllProceedInfos().Any(
                        _Info => RazorMazeUtils.GravityItemTypes().Contains(_Info.Type));
                    if (!enableRotation)
                    {
                        CommandsProceeder.LockCommand(EInputCommand.RotateClockwise);
                        CommandsProceeder.LockCommand(EInputCommand.RotateCounterClockwise);
                    }
                    break;
                case ELevelStage.Finished:
                    if (_Args.PreviousStage != ELevelStage.Paused)
                    {
                        ConsiderCongratsPanelWhileAppearing(true);
                        SetCongratsString();
                        ShowCongratsPanel(true);
                        SetLevelCheckMarks(_Args.LevelIndex, true);    
                    }
                    break;
                case ELevelStage.ReadyToUnloadLevel:
                    m_CongratsAnim.SetTrigger(AnimKeyCongratsIdle);
                    ShowControls(false, false);
                    break;
            }
            Prompt.OnLevelStageChanged(_Args);
        }

        #endregion

        #region nonpublic methds

        private void InitGameUI()
        {
            InitTopButtons();
            InitLevelAndCongratsPanel();
            InitStartLogo();
            InitRotateButtons();
            GameTicker.Register(this);
            ColorProvider.ColorChanged += ColorProviderOnColorChanged;
        }

        private void ColorProviderOnColorChanged(int _ColorId, Color _Color)
        {
            if (_ColorId == ColorIds.UI)
            {
                foreach (var shapeComp in m_RotatingButtonShapes)
                {
                    if (shapeComp is ShapeRenderer rend)
                        rend.Color = _Color;
                }
                
                foreach (var shapeComp in m_RotatingButtonShapes2)
                {
                    if (shapeComp is ShapeRenderer rend)
                        rend.Color = _Color.SetA(0.2f);
                }

                foreach (var rendComp in m_Renderers)
                {
                    if (rendComp is ShapeRenderer rend1)
                        rend1.Color = _Color;
                    else if (rendComp is SpriteRenderer rend2)
                        rend2.color = _Color;
                    else if (rendComp is TMP_Text rend3)
                        rend3.color = _Color;
                }
            }
        }

        private void InitTopButtons()
        {
            const float topOffset = 0f;
            const float horOffset = 1f;
            float scale = GraphicUtils.AspectRatio * 3f;
            var bounds = GraphicUtils.GetVisibleBounds(CameraProvider.MainCamera);
            float yPos = CoordinateConverter.GetScreenOffsets().w;
            var cont = GetGameUIContainer();
            var goShopButton = PrefabUtilsEx.InitPrefab(
                cont, "ui_game", "shop_button");
            var goSettingsButton = PrefabUtilsEx.InitPrefab(
                cont, "ui_game", "settings_button");
            m_Renderers.AddRange( new Component[]
            {
                goShopButton.GetCompItem<SpriteRenderer>("button"),
                goSettingsButton.GetCompItem<SpriteRenderer>("button")
            });
            goShopButton.transform.localScale = scale * Vector3.one;
            goShopButton.transform.SetPosXY(
                new Vector2(bounds.min.x, yPos)
                + Vector2.right * horOffset + Vector2.down * topOffset);
            goSettingsButton.transform.localScale = scale * Vector3.one;
            goSettingsButton.transform.SetPosXY(
                new Vector2(bounds.max.x, yPos)
                + Vector2.left * horOffset + Vector2.down * topOffset);
            m_ShopButton = goShopButton.GetCompItem<ButtonOnRaycast>("button");
            m_SettingsButton = goSettingsButton.GetCompItem<ButtonOnRaycast>("button");
            m_ShopButton.Init(
                CommandShop, 
                () => Model.LevelStaging.LevelStage, 
                CameraProvider,
                Managers.HapticsManager);
            m_SettingsButton.Init(
                CommandSettings, 
                () => Model.LevelStaging.LevelStage,
                CameraProvider,
                Managers.HapticsManager);
            goShopButton.SetActive(false);
            goSettingsButton.SetActive(false);
        }

        private void InitLevelAndCongratsPanel()
        {
            var bounds = GraphicUtils.GetVisibleBounds(CameraProvider.MainCamera);
            var mazeBounds = CoordinateConverter.GetMazeBounds();
            float yPos = CoordinateConverter.GetScreenOffsets().w;
            var cont = GetGameUIContainer();
            var goLevelText = PrefabUtilsEx.InitPrefab(
                cont, "ui_game", "level_text");
            m_LevelText = goLevelText.GetCompItem<TextMeshPro>("text");
            m_LevelText.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, mazeBounds.size.x);
            m_LevelText.transform.position = new Vector3(
                mazeBounds.center.x, 
                yPos);
            var goLevelCheckMark = PrefabUtilsEx.GetPrefab(
                "ui_game", "level_check_mark");
            yPos -= 2f;
            for (int i = 0; i < RazorMazeUtils.LevelsInGroup; i++)
            {
                var go = Object.Instantiate(goLevelCheckMark, cont);
                m_CheckMarks.Add(go.GetCompItem<Animator>("animator"));
                m_Renderers.AddRange(new Component[]
                {
                    go.GetCompItem<Rectangle>("body"),
                    go.GetCompItem<Line>("checkmark_1"),
                    go.GetCompItem<Line>("checkmark_2")
                });
                float xCoeff1 = 8f * ((i + 0.5f) / RazorMazeUtils.LevelsInGroup - 0.5f);
                go.transform.SetLocalPosXY(
                    mazeBounds.center.x + xCoeff1,
                    yPos);
            }
            yPos -= 3.5f;
            m_Renderers.Add(m_LevelText);
            goLevelCheckMark.DestroySafe();
            var goCongrads = PrefabUtilsEx.InitPrefab(
                cont, "ui_game", "congratulations_panel");
            goCongrads.transform.SetPosXY(new Vector2(bounds.center.x, yPos));
            m_CompletedText = goCongrads.GetCompItem<TextMeshPro>("text_completed");
            m_CongratsText = goCongrads.GetCompItem<TextMeshPro>("text_congrats");
            m_CongratsLine = goCongrads.GetCompItem<Line>("line");
            m_CongratsAnim = goCongrads.GetCompItem<Animator>("animator");
        }

        private void InitStartLogo()
        {
            const float topOffset = 12f;
            var go = PrefabUtilsEx.InitPrefab(
                GetGameUIContainer(),
                "ui_game",
                "start_logo");
            var mazeBounds = CoordinateConverter.GetMazeBounds();
            float yPos = CoordinateConverter.GetScreenOffsets().w - topOffset;
            go.transform.SetLocalPosXY(
                mazeBounds.center.x,
                yPos);
            go.transform.localScale = Vector3.one * 4f;
            m_StartLogoCharAnims = new Dictionary<string, Animator>
            {
                {"R1", go.GetCompItem<Animator>("R1")},
                {"M", go.GetCompItem<Animator>("M")},
                {"A", go.GetCompItem<Animator>("A")},
                {"Z", go.GetCompItem<Animator>("Z")},
                {"O", go.GetCompItem<Animator>("O")},
                {"R2", go.GetCompItem<Animator>("R2")}
            };
            foreach (var anim in m_StartLogoCharAnims.Values)
                anim.SetTrigger(AnimKeyStartLogoHide);
            var trigerrer1 = go.GetCompItem<AnimationTriggerer>("trigerrer_1");
            var trigerrer2 = go.GetCompItem<AnimationTriggerer>("trigerrer_2");
            trigerrer1.Trigger1 += () =>  CommandsProceeder.LockAllCommands();
            trigerrer2.Trigger1 += () => CommandsProceeder.UnlockAllCommands();
            var eye1 = go.GetCompItem<Rectangle>("eye_1");
            var eye2 = go.GetCompItem<Rectangle>("eye_2");
            eye1.Color = eye2.Color = ColorProvider.GetColor(ColorIds.Background);
        }
        
        private void InitRotateButtons()
        {
            const float horOffset = 1f;
            var cont = GetGameUIContainer();
            var goRCb = PrefabUtilsEx.InitPrefab(
                cont, "ui_game", "rotate_clockwise_button");
            var goRCCb = PrefabUtilsEx.InitPrefab(
                cont, "ui_game", "rotate_counter_clockwise_button");
            float scale = CoordinateConverter.Scale * 0.2f;
            var bounds = GraphicUtils.GetVisibleBounds();
            float yPos = CoordinateConverter.GetScreenOffsets().z;
            var rcbDisc = goRCb.GetCompItem<Disc>("button");
            goRCb.transform.localScale = scale * Vector3.one;
            goRCb.transform.SetPosXY(
                new Vector2(bounds.center.x, yPos) 
                + (Vector2.right + Vector2.up) * scale * rcbDisc.Radius 
                + Vector2.right * horOffset);
            goRCCb.transform.localScale = scale * Vector3.one;
            goRCCb.transform.SetPosXY(
                new Vector2(bounds.center.x, yPos)
                + (Vector2.left + Vector2.up) * scale * rcbDisc.Radius
                + Vector2.left * horOffset);
            m_RotateClockwiseButton = goRCb.GetCompItem<ButtonOnRaycast>("button");
            m_RotateCounterClockwiseButton = goRCCb.GetCompItem<ButtonOnRaycast>("button");
            m_RotateClockwiseButton.Init(
                CommandRotateClockwise, 
                () => Model.LevelStaging.LevelStage, 
                CameraProvider,
                Managers.HapticsManager);
            m_RotateCounterClockwiseButton.Init(
                CommandRotateCounterClockwise, 
                () => Model.LevelStaging.LevelStage,
                CameraProvider,
                Managers.HapticsManager);
            m_RotatingButtonShapes.AddRange(new ShapeRenderer[]
            {
                goRCb.GetCompItem<Disc>("outer_disc"),
                goRCb.GetCompItem<Disc>("line"),
                goRCb.GetCompItem<Line>("arrow_part_1"), 
                goRCb.GetCompItem<Line>("arrow_part_2"), 
                goRCCb.GetCompItem<Disc>("outer_disc"), 
                goRCCb.GetCompItem<Disc>("line"),
                goRCCb.GetCompItem<Line>("arrow_part_1"), 
                goRCCb.GetCompItem<Line>("arrow_part_2")
            });
            
            m_RotatingButtonShapes2.Add(goRCb.GetCompItem<Disc>("inner_disc"));
            m_RotatingButtonShapes2.Add(goRCCb.GetCompItem<Disc>("inner_disc"));
            
            goRCb.SetActive(false);
            goRCCb.SetActive(false);
        }

        private void ShowStartLogo(LevelStageArgs _Args)
        {
            switch (_Args.Stage)
            {
                case ELevelStage.ReadyToStart when _Args.PreviousStage != ELevelStage.Paused && m_OnStart:
                    ShowStartLogo();
                    break;
                case ELevelStage.StartedOrContinued when m_OnStart:
                    HideStartLogo();
                    m_OnStart = false;
                    break;
            }
        }

        private void ShowStartLogo()
        {
            foreach (var anim in m_StartLogoCharAnims.Values)
                anim.speed = 1.5f;
            ShowStartLogoItem("R1", 0f);
            ShowStartLogoItem("M", 0.1f);
            ShowStartLogoItem("A", 0.2f);
            ShowStartLogoItem("Z", 0.3f);
            ShowStartLogoItem("O", 0.5f);
            ShowStartLogoItem("R2", 0.4f);
        }

        private void ShowStartLogoItem(string _Key, float _Delay)
        {
            float time = GameTicker.Time;
            Coroutines.Run(Coroutines.WaitWhile(
                () => time + _Delay > GameTicker.Time,
                () => m_StartLogoCharAnims[_Key].SetTrigger(AnimKeyStartLogoAppear)));
        }

        private void HideStartLogo()
        {
            foreach (var anim in m_StartLogoCharAnims.Values)
                anim.speed = 3f;
            foreach (var anim in m_StartLogoCharAnims.Values)
                anim.SetTrigger(AnimKeyStartLogoDisappear);
        }

        private void ShowControls(bool _Show, bool _Instantly)
        {
            if (_Show)
            {
                Managers.LocalizationManager.AddTextObject(
                    m_LevelText,
                    "level", 
                    _Text => _Text + " " + (Model.Data.LevelIndex + 1));
            }
            
            if (_Instantly && !_Show || _Show)
            {
                m_ShopButton.SetGoActive(_Show);
                m_SettingsButton.SetGoActive(_Show);
            }
            
            m_ShopButton.enabled = _Show;
            m_SettingsButton.enabled = _Show;
            
            if (_Instantly)
                return;
            AppearTransitioner.DoAppearTransition(
                _Show, 
                new Dictionary<IEnumerable<Component>, System.Func<Color>>
                {
                    {m_Renderers, () => ColorProvider.GetColor(ColorIds.UI)}
                },
                _Type: EAppearTransitionType.WithoutDelay);
        }

        private void LockCommands(LevelStageArgs _Args)
        {
            switch (_Args.Stage)
            {
                case ELevelStage.Loaded:
                case ELevelStage.Paused:
                case ELevelStage.ReadyToUnloadLevel:
                case ELevelStage.Unloaded:
                case ELevelStage.CharacterKilled:
                    CommandsProceeder.LockAllCommands();
                    CommandsProceeder.UnlockCommand(EInputCommand.ShopMenu);
                    CommandsProceeder.UnlockCommand(EInputCommand.SettingsMenu);
                    break;
                case ELevelStage.Finished:
                    CommandsProceeder.LockAllCommands();
                    CommandsProceeder.UnlockCommand(EInputCommand.ShopMenu);
                    CommandsProceeder.UnlockCommand(EInputCommand.SettingsMenu);
                    CommandsProceeder.UnlockCommand(EInputCommand.ReadyToUnloadLevel);
                    break;
                case ELevelStage.ReadyToStart:
                case ELevelStage.StartedOrContinued:
                    CommandsProceeder.UnlockAllCommands();
                    break;
                default:
                    throw new SwitchCaseNotImplementedException(_Args.Stage);
            }
        }

        private void ShowRotatingButtons(LevelStageArgs _Args)
        {
            bool MazeContainsGravityItems()
            {
                return Model.GetAllProceedInfos()
                    .Any(_Info => RazorMazeUtils.GravityItemTypes().Contains(_Info.Type));
            }

            switch (_Args.Stage)
            {
                case ELevelStage.Loaded when MazeContainsGravityItems():
                    ShowRotatingButtons(true, false);
                    break;
                case ELevelStage.Loaded:
                    ShowRotatingButtons(false, true);
                    break;
                case ELevelStage.ReadyToStart:
                    ShowRotatingButtons(MazeContainsGravityItems(), true);
                    break;
            }
        }
        
        private void ShowRotatingButtons(bool _Show, bool _Instantly)
        {
            if (_Instantly && !_Show || _Show)
            {
                m_RotateClockwiseButton.SetGoActive(_Show);
                m_RotateCounterClockwiseButton.SetGoActive(_Show);
                m_RotateClockwiseButton.enabled = _Show;
                m_RotateCounterClockwiseButton.enabled = _Show;
            }
            if (_Instantly)
                return;
            AppearTransitioner.DoAppearTransition(_Show, 
                new Dictionary<IEnumerable<Component>, System.Func<Color>>
                {
                    {m_RotatingButtonShapes, () => ColorProvider.GetColor(ColorIds.UI)},
                    {m_RotatingButtonShapes2, () => ColorProvider.GetColor(ColorIds.UI).SetA(0.2f)},
                }, _Type: EAppearTransitionType.WithoutDelay);
        }
        
        private void CommandRotateClockwise()
        {
            if (BigDialogViewer.IsShowing || BigDialogViewer.IsInTransition)
                return;
            CommandsProceeder.RaiseCommand(EInputCommand.RotateClockwise, null);
            Coroutines.Run(ButtonAnimCoroutine(m_RotateClockwiseButton.transform));
        }
        
        private void CommandRotateCounterClockwise()
        {
            if (BigDialogViewer.IsShowing || BigDialogViewer.IsInTransition)
                return;
            CommandsProceeder.RaiseCommand(EInputCommand.RotateCounterClockwise, null);
            Coroutines.Run(ButtonAnimCoroutine(m_RotateCounterClockwiseButton.transform));
        }

        private void CommandShop()
        {
            if (BigDialogViewer.IsShowing || BigDialogViewer.IsInTransition)
                return;
            Managers.AnalyticsManager.SendAnalytic(AnalyticIds.ShopButtonPressed);
            CommandsProceeder.RaiseCommand(EInputCommand.ShopMenu, null);
        }

        private void CommandSettings()
        {
            if (BigDialogViewer.IsShowing || BigDialogViewer.IsInTransition)
                return;
            Managers.AnalyticsManager.SendAnalytic(AnalyticIds.SettingsButtonPressed);
            CommandsProceeder.RaiseCommand(EInputCommand.SettingsMenu, null);
        }

        private IEnumerator ButtonAnimCoroutine(Transform _Button)
        {
            var startScale = _Button.transform.localScale;
            const float minScale = 0.7f;
            yield return Coroutines.Lerp(
                0f,
                1f,
                0.1f,
                _P => _Button.localScale = startScale * _P,
                GameTicker,
                _ProgressFormula: _P =>
                {
                    if (_P < 0.5f)
                        return minScale + (1f - _P * 2f) * (1f - minScale);
                    else
                        return minScale + (_P * 2f - 1f) * (1f - minScale);
                });
        }

        private Transform GetGameUIContainer()
        {
            return ContainersGetter.GetContainer(ContainerNames.GameUI);
        }

        private void SetLevelCheckMarks(int _Level, bool _Passed)
        {
            int levelIndexInGroup = _Level % RazorMazeUtils.LevelsInGroup;
            for (int i = 0; i < levelIndexInGroup; i++)
                m_CheckMarks[i].SetTrigger(AnimKeyChekMarkSet);
            m_CheckMarks[levelIndexInGroup].SetTrigger(_Passed ? AnimKeyCheckMarkPass : AnimKeyCheckMarkIdle);
            for (int i = levelIndexInGroup; i < m_CheckMarks.Count; i++)
                m_CheckMarks[i].SetTrigger(AnimKeyCheckMarkIdle);
        }

        private void SetCongratsString()
        {
            Managers.LocalizationManager.AddTextObject(m_CompletedText, "completed");
            float levelTime = Model.LevelStaging.LevelTime;
            int diesCount = Model.LevelStaging.DiesCount;
            int pathesCount = Model.PathItemsProceeder.PathProceeds.Count;
            float coeff = (float) pathesCount / (diesCount + 1);
            string congradsKey;
            if (levelTime < coeff * ViewSettings.FinishTimeExcellent)
                congradsKey = "awesome";
            else if (levelTime < coeff * ViewSettings.FinishTimeGood)
                congradsKey = "good_job";
            else
                congradsKey = "not_bad";
            Managers.LocalizationManager.AddTextObject(
                m_CongratsText, 
                congradsKey, 
                _Text => _Text.ToUpperInvariant());
        }

        private void ConsiderCongratsPanelWhileAppearing(bool _Consider)
        {
            var congratRenderers = new Behaviour[]
            {
                m_CompletedText,
                m_CongratsText,
                m_CongratsLine
            };

            if (_Consider)
            {
                if (!m_Renderers.Contains(congratRenderers.First()))
                    m_Renderers.AddRange(congratRenderers);
            }
            else
            {
                foreach (var rend in congratRenderers)
                    m_Renderers.Remove(rend);
            }
        }
        
        private void ShowCongratsPanel(bool _Show)
        {
            var col = ColorProvider.GetColor(ColorIds.UI);
            if (!_Show)
                col = col.SetA(0f);
            m_CompletedText.color = m_CongratsText.color = m_CongratsLine.Color = col;
            m_CongratsAnim.SetTrigger(_Show ? AnimKeyCongratsAnim : AnimKeyCongratsIdle);
        }

        #endregion
    }
}