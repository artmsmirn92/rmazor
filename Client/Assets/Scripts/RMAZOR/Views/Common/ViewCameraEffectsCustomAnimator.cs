﻿using Common;
using Common.CameraProviders;
using Common.CameraProviders.Camera_Effects_Props;
using Common.Providers;
using RMAZOR.Models;

namespace RMAZOR.Views.Common
{
    public interface IViewCameraEffectsCustomAnimator : IOnLevelStageChanged
    {
        void AnimateCameraEffectsOnBetweenLevelTransition(bool _Appear);
    }
    
    public class ViewCameraEffectsCustomAnimator : IViewCameraEffectsCustomAnimator
    {
        #region inject
        
        private ViewSettings    ViewSettings   { get; }
        private ICameraProvider CameraProvider { get; }
        private IColorProvider  ColorProvider  { get; }

        public ViewCameraEffectsCustomAnimator(
            ViewSettings    _ViewSettings,
            ICameraProvider _CameraProvider,
            IColorProvider  _ColorProvider)
        {
            ViewSettings   = _ViewSettings;
            CameraProvider = _CameraProvider;
            ColorProvider  = _ColorProvider;
        }

        #endregion

        #region api

        public void OnLevelStageChanged(LevelStageArgs _Args)
        {
            SetColorGradingProps(_Args);
        }

        public void AnimateCameraEffectsOnBetweenLevelTransition(bool _Appear)
        {
            AnimateColorGradingPropsOnBetweenLevel(_Appear);
        }

        #endregion

        #region nonpublic methods

        private void SetColorGradingProps(LevelStageArgs _Args)
        {
            if (_Args.LevelStage != ELevelStage.Loaded)
                return;
            CameraProvider.EnableEffect(ECameraEffect.ColorGrading, true);
            CameraProvider.SetEffectProps(ECameraEffect.ColorGrading, new ColorGradingProps
            {
                Contrast = 0.35f,
                Blur = 0.2f,
                VignetteColor = ColorProvider.GetColor(ColorIds.PathFill),
                VignetteAmount = 0.05f
            });
        }

        private void AnimateColorGradingPropsOnBetweenLevel(bool _Appear)
        {
            var cgPropsFrom = new ColorGradingProps {VignetteSoftness = 0.001f};
            var cgPropsTo = new ColorGradingProps {VignetteSoftness = 0.5f};
            if (!_Appear)
                (cgPropsFrom, cgPropsTo) = (cgPropsTo, cgPropsFrom);
            CameraProvider.AnimateEffectProps(
                ECameraEffect.ColorGrading, 
                cgPropsFrom, 
                cgPropsTo, 
                ViewSettings.betweenLevelTransitionTime);
        }

        #endregion
    }
}