﻿using DI.Extensions;
using DialogViewers;
using Entities;
using GameHelpers;
using Games.RazorMaze.Views.Common;
using LeTai.Asset.TranslucentImage;
using Ticker;
using UnityEngine;

namespace UI.Panels
{
    public abstract class DialogPanelBase : IDialogPanel
    {
        #region inject

        protected IManagersGetter   Managers         { get; }
        protected IUITicker         Ticker           { get; }
        protected IBigDialogViewer  DialogViewer     { get; }
        protected ICameraProvider   CameraProvider   { get; }
        protected IColorProvider    ColorProvider    { get; }

        protected DialogPanelBase(
            IManagersGetter _Managers, 
            IUITicker _Ticker, 
            IBigDialogViewer _DialogViewer,
            ICameraProvider _CameraProvider,
            IColorProvider _ColorProvider)
        {
            Managers = _Managers;
            Ticker = _Ticker;
            DialogViewer = _DialogViewer;
            CameraProvider = _CameraProvider;
            ColorProvider = _ColorProvider;
        }

        #endregion

        #region api

        public abstract EUiCategory Category { get; }
        public RectTransform Panel { get; protected set; }
        
        public virtual void LoadPanel()
        {
            Ticker.Register(this);
            ColorProvider.ColorChanged += OnColorChanged;
        }
        
        protected void SetTranslucentBackgroundSource(GameObject _Object)
        {
            var translBack = _Object.GetCompItem<TranslucentImage>("translucent_background");
            translBack.source = CameraProvider.MainCamera.GetComponent<TranslucentImageSource>();
        }
        
        protected virtual void OnColorChanged(int _ColorId, Color _Color) { }
        public virtual void OnDialogEnable() { }
        public virtual void OnDialogShow() { }
        public virtual void OnDialogHide() { }

        #endregion
    }
}