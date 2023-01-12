﻿using System.Collections;
using Common.Constants;
using mazing.common.Runtime.CameraProviders;
using mazing.common.Runtime.Managers;
using mazing.common.Runtime.Ticker;
using mazing.common.Runtime.UI;
using mazing.common.Runtime.UI.DialogViewers;

namespace Common
{
    public interface IDialogViewerFullscreen2 : IDialogViewer { }
    
    public class DialogViewerFullscreen2Fake : DialogViewerMediumFake, IDialogViewerFullscreen2 { }
    
    public class DialogViewerFullscreen2 : DialogViewerCommonBase, IDialogViewerFullscreen2
    {
        protected override string PrefabName => "fullscreen_2";
        
        public DialogViewerFullscreen2(
            IViewUICanvasGetter _CanvasGetter,
            IUITicker           _Ticker,
            ICameraProvider     _CameraProvider,
            IPrefabSetManager   _PrefabSetManager)
            : base(
                _CanvasGetter,
                _Ticker, 
                _CameraProvider, 
                _PrefabSetManager) { }

        public override int    Id         => MazorCommonDialogViewerIds.Fullscreen2;
        public override string CanvasName => CommonCanvasNames.CommonCameraSpace;

        protected override IEnumerator DoTranslucentBackgroundTransition(bool _Disappear, float _Time)
        {
            yield return null;
        }
    }
}