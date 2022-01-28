﻿using System;
using System.Collections.Generic;
using System.Linq;
using Common.CameraProviders;
using Common.Constants;
using Common.Extensions;
using Common.Ticker;
using Common.Utils;
using GameHelpers;
using RMAZOR.Models;
using RMAZOR.Views.Common;
using RMAZOR.Views.ContainerGetters;
using RMAZOR.Views.InputConfigurators;
using Shapes;
using UnityEngine;

namespace RMAZOR.Views.UI.StartLogo
{
    public abstract class ViewUIStartLogoBase : IViewUIStartLogo
    {
        #region nonpublic members

        private static int AnimKeyStartLogoAppear    => AnimKeys.Anim;
        private static int AnimKeyStartLogoDisappear => AnimKeys.Stop;
        private static int AnimKeyStartLogoHide      => AnimKeys.Stop2;

        protected virtual  float                     AnimationSpeed => 1f;
        protected abstract string                    PrefabName     { get; }
        protected abstract Dictionary<string, float> KeysAndDelays  { get; }
        
        protected GameObject                   StartLogoObj;
        protected bool                         LogoShowingAnimationPassed;
        private   Dictionary<string, Animator> m_StartLogoCharAnims;
        private   float                        m_TopOffset;
        private   bool                         m_OnStart = true;

        #endregion
        
        #region inject

        private   IViewInputCommandsProceeder CommandsProceeder { get; }
        protected ICameraProvider             CameraProvider    { get; }
        protected IContainersGetter           ContainersGetter  { get; }
        protected IColorProvider              ColorProvider     { get; }
        protected IViewGameTicker             GameTicker        { get; }
        protected IPrefabSetManager           PrefabSetManager  { get; }
        
        protected ViewUIStartLogoBase(
            ICameraProvider             _CameraProvider,
            IContainersGetter           _ContainersGetter,
            IViewInputCommandsProceeder _CommandsProceeder,
            IColorProvider              _ColorProvider,
            IViewGameTicker             _GameTicker,
            IPrefabSetManager           _PrefabSetManager)
        {
            CameraProvider    = _CameraProvider;
            ContainersGetter  = _ContainersGetter;
            CommandsProceeder = _CommandsProceeder;
            ColorProvider     = _ColorProvider;
            GameTicker        = _GameTicker;
            PrefabSetManager  = _PrefabSetManager;
        }

        #endregion

        #region api

        public void Init(Vector4 _Offsets)
        {
            CommandsProceeder.Command += OnCommand;
            m_TopOffset = _Offsets.w;
            InitStartLogo();
        }

        public void OnLevelStageChanged(LevelStageArgs _Args)
        {
            ShowStartLogo(_Args);
        }
        
        #endregion

        #region nonpublic members

        private void OnCommand(EInputCommand _Command, object[] _Args)
        {
            if (!m_OnStart ||
                !RazorMazeUtils.GetMoveCommands().ContainsAlt(_Command) &&
                !RazorMazeUtils.GetRotateCommands().ContainsAlt(_Command)) return;
            HideStartLogo();
            m_OnStart = false;
        }
        
        private void ShowStartLogo(LevelStageArgs _Args)
        {
            if (_Args.Stage == ELevelStage.ReadyToStart
                && _Args.PreviousStage == ELevelStage.Loaded
                && m_OnStart)
            {
                ShowStartLogo();
            }
        }

        protected virtual void InitStartLogo()
        {
            var screenBounds = GraphicUtils.GetVisibleBounds(CameraProvider.MainCamera);
            const float additionalOffset = 8f;
            var go = PrefabSetManager.InitPrefab(
                ContainersGetter.GetContainer(ContainerNames.GameUI),
                "ui_game",
                PrefabName);
            StartLogoObj = go;
            float yPos = screenBounds.max.y - m_TopOffset - additionalOffset;
            go.transform.SetLocalPosXY(
                screenBounds.center.x,
                yPos);
            go.transform.localScale = Vector3.one * GraphicUtils.AspectRatio * 7f;
            m_StartLogoCharAnims = KeysAndDelays.Keys
                .ToDictionary(
                    _C => _C, 
                    _C => go.GetCompItem<Animator>(_C));
            foreach (var anim in m_StartLogoCharAnims.Values)
                anim.SetTrigger(AnimKeyStartLogoHide);
            var shapeTypes = new [] {typeof(Line), typeof(Disc), typeof(Rectangle)};
            var color = ColorProvider.GetColor(ColorIds.UiStartLogo);
            shapeTypes.SelectMany(_Type => go
                    .GetComponentsInChildren(_Type, true))
                .Cast<ShapeRenderer>()
                .Except(GetExceptedLogoColorObjects())
                .ToList()
                .ForEach(_Shape => _Shape.Color = color.SetA(_Shape.Color.a));
        }
        

        protected virtual IEnumerable<ShapeRenderer> GetExceptedLogoColorObjects()
        {
            return new[] { (ShapeRenderer)null };
        }

        private void ShowStartLogo()
        {
            foreach (var kvp in m_StartLogoCharAnims)
            {
                ShowStartLogoItem(kvp.Key, KeysAndDelays[kvp.Key]);
                kvp.Value.speed = AnimationSpeed;
            }
        }

        private void HideStartLogo()
        {
            if (!LogoShowingAnimationPassed)
            {
                StartLogoObj.SetActive(false);
                return;
            }
            foreach (var anim in m_StartLogoCharAnims.Values)
                anim.speed = 3f;
            foreach (var anim in m_StartLogoCharAnims.Values)
                anim.SetTrigger(AnimKeyStartLogoDisappear);
        }
        
        private void ShowStartLogoItem(string _Key, float _Delay)
        {
            float time = GameTicker.Time;
            Cor.Run(Cor.WaitWhile(
                () => time + _Delay > GameTicker.Time,
                () => m_StartLogoCharAnims[_Key].SetTrigger(AnimKeyStartLogoAppear)));
        }
        
        #endregion
    }
}