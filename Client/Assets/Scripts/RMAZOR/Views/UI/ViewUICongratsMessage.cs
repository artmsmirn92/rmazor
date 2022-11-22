﻿using System.Collections.Generic;
using System.Linq;
using Common;
using Common.CameraProviders;
using Common.Constants;
using Common.Extensions;
using Common.Managers;
using Common.Providers;
using Common.Utils;
using RMAZOR.Managers;
using RMAZOR.Models;
using Shapes;
using TMPro;
using UnityEngine;

namespace RMAZOR.Views.UI
{
    public interface IViewUICongratsMessage : 
        IOnLevelStageChanged,
        IInitViewUIItem, 
        IViewUIGetRenderers { }
    
    public class ViewUICongratsMessage : IViewUICongratsMessage
    {

        #region nonpublic members

        private static int AnimKeyCongratsAnim => AnimKeys.Anim;
        private static int AnimKeyCongratsIdle => AnimKeys.Stop;
        
        private readonly List<Component> m_Renderers = new List<Component>();

        private GameObject  m_CongratsGo;
        private TextMeshPro m_CongratsText;
        private TextMeshPro m_CompletedText;
        private Line        m_CongratsLine;
        private Animator    m_CongratsAnim;
        private float       m_TopOffset;

        #endregion

        #region inject

        private ViewSettings      ViewSettings     { get; }
        private IModelGame        Model            { get; }
        private ICameraProvider   CameraProvider   { get; }
        private IManagersGetter   Managers         { get; }
        private IColorProvider    ColorProvider    { get; }

        private ViewUICongratsMessage(
            ViewSettings      _ViewSettings,
            IModelGame        _Model,
            ICameraProvider   _CameraProvider,
            IManagersGetter   _Managers,
            IColorProvider    _ColorProvider)
        {
            ViewSettings     = _ViewSettings;
            Model            = _Model;
            CameraProvider   = _CameraProvider;
            Managers         = _Managers;
            ColorProvider    = _ColorProvider;
        }

        #endregion

        #region api
        
        public void Init(Vector4 _Offsets)
        {
            m_TopOffset = _Offsets.w;
            InitCongratsMessage();
            CameraProvider.ActiveCameraChanged += OnActiveCameraChanged;
        }

        public IEnumerable<Component> GetRenderers()
        {
            return m_Renderers;
        }

        public void OnLevelStageChanged(LevelStageArgs _Args)
        {
            switch (_Args.LevelStage)
            {
                case ELevelStage.Loaded:
                    ConsiderCongratsPanelWhileAppearing(false);
                    ShowCongratsPanel(false);
                    break;
                case ELevelStage.Finished when _Args.PreviousStage != ELevelStage.Paused: 
                    ConsiderCongratsPanelWhileAppearing(true);
                    SetCongratsString();
                    ShowCongratsPanel(true);
                    break;
                case ELevelStage.ReadyToUnloadLevel:
                    m_CongratsAnim.SetTrigger(AnimKeyCongratsIdle);
                    break;
            }
        }

        #endregion

        #region nonpublic methods
        
        private void OnActiveCameraChanged(Camera _Camera)
        {
            var parent = CameraProvider.Camera.transform;
            m_CongratsGo.SetParent(parent);
            var screenBounds = GraphicUtils.GetVisibleBounds(CameraProvider.Camera);
            m_CongratsGo.transform
                .SetLocalPosX(screenBounds.center.x)
                .SetLocalPosY(screenBounds.max.y - m_TopOffset - 5f);
        }

        private void InitCongratsMessage()
        {
            var parent = CameraProvider.Camera.transform;
            var goCongrads = Managers.PrefabSetManager.InitPrefab(
                parent, CommonPrefabSetNames.UiGame, "congratulations_panel");
            m_CongratsGo = goCongrads;
            m_CompletedText = m_CongratsGo.GetCompItem<TextMeshPro>("text_completed");
            m_CongratsText  = m_CongratsGo.GetCompItem<TextMeshPro>("text_congrats");
            m_CongratsLine  = m_CongratsGo.GetCompItem<Line>("line");
            m_CongratsAnim  = m_CongratsGo.GetCompItem<Animator>("animator");
        }
        
        private void SetCongratsString()
        {
            Managers.LocalizationManager.AddTextObject(new LocalizableTextObjectInfo(
                m_CompletedText, 
                ETextType.GameUI,
                "completed"));
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
            Managers.LocalizationManager.AddTextObject(new LocalizableTextObjectInfo(
                m_CongratsText, 
                ETextType.GameUI,
                congradsKey,
                _Text => _Text.ToUpperInvariant()));
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