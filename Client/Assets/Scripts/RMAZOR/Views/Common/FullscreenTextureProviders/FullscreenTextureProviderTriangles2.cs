﻿using Common.Helpers;
using Common.Managers;
using mazing.common.Runtime.CameraProviders;
using mazing.common.Runtime.Helpers;
using mazing.common.Runtime.Managers;
using mazing.common.Runtime.Providers;
using mazing.common.Runtime.Ticker;
using RMAZOR.Views.Common.ViewMazeBackgroundPropertySets;
using UnityEngine;

namespace RMAZOR.Views.Common.FullscreenTextureProviders
{
    public interface IFullscreenTextureProviderTriangles2
        : IFullscreenTextureProvider
    {
        void SetProperties(Triangles2TextureProps _Item);
    }
    
    public class FullscreenTextureProviderTriangles2 :
        FullscreenTextureProviderBase,
        IFullscreenTextureProviderTriangles2
    {
        #region nonpublic members
        
        private static readonly int
            SizeId        = Shader.PropertyToID("_Size"),
            RatioId       = Shader.PropertyToID("_Ratio"),
            AId           = Shader.PropertyToID("_A"),
            BId           = Shader.PropertyToID("_B"),
            CId           = Shader.PropertyToID("_C"),
            DId           = Shader.PropertyToID("_D"),
            EId           = Shader.PropertyToID("_E"),
            FId           = Shader.PropertyToID("_F"),
            SmoothId      = Shader.PropertyToID("_Smooth"),
            MirrorId      = Shader.PropertyToID("_Mirror"),
            TruncId       = Shader.PropertyToID("_Trunc"),
            TruncColor2Id = Shader.PropertyToID("_TruncColor2");

        #endregion

        #region inject

        private FullscreenTextureProviderTriangles2(
            IPrefabSetManager _PrefabSetManager, 
            IContainersGetter _ContainersGetter, 
            ICameraProvider   _CameraProvider, 
            IViewGameTicker   _Ticker,
            IColorProvider    _ColorProvider)
            : base(
                _PrefabSetManager,
                _ContainersGetter,
                _CameraProvider,
                _ColorProvider,
                _Ticker) { }

        #endregion

        #region api

        public override float Quality => 1f;
        
        public void SetProperties(Triangles2TextureProps _Item)
        {
            Material.SetFloat(SizeId, _Item.size);
            Material.SetFloat(RatioId, _Item.ratio);
            Material.SetFloat(AId, _Item.a);
            Material.SetFloat(BId, _Item.b);
            Material.SetFloat(CId, _Item.c);
            Material.SetFloat(DId, _Item.d);
            Material.SetFloat(EId, _Item.e);
            Material.SetFloat(FId, _Item.f);
            Material.SetFloat(SmoothId, _Item.smooth ? 1f : 0f);
            Material.SetFloat(MirrorId, _Item.mirror ? 1f : 0f);
            Material.SetFloat(TruncId, _Item.trunc ? 1f : 0f);
            Material.SetFloat(TruncColor2Id, _Item.truncColor2 ? 1f : 0f);
            Material.SetFloat(DirectionId, _Item.direction);
        }

        #endregion

    }
}