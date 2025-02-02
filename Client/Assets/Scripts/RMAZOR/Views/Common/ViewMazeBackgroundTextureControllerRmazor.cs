﻿using System.Collections.Generic;
using System.Linq;
using Common.Constants;
using Common.Helpers;
using mazing.common.Runtime.CameraProviders;
using mazing.common.Runtime.Constants;
using mazing.common.Runtime.Extensions;
using mazing.common.Runtime.Helpers;
using mazing.common.Runtime.Managers;
using mazing.common.Runtime.Providers;
using mazing.common.Runtime.Utils;
using RMAZOR.Models;
using RMAZOR.Settings;
using RMAZOR.Views.Common.FullscreenTextureProviders;
using RMAZOR.Views.Common.ViewMazeBackgroundPropertySets;
using RMAZOR.Views.Utils;
using UnityEngine;

namespace RMAZOR.Views.Common
{
    public class ViewMazeBackgroundTextureControllerRmazor : ViewMazeBackgroundTextureControllerBase
    {
        #region nonpublic members

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        private readonly Dictionary<float, RenderTexture> m_RenderTexturesDict
            = new Dictionary<float, RenderTexture>();
            
        private IList<Triangles2TextureProps> m_Triangles2TextureSetItems;
        private List<ITextureProps>           m_TextureSetItems;
        private MeshRenderer                  m_MainRenderer;
        private Material                      m_MainRendererMaterial;
        private Camera                        m_RenderCamera;

        private bool m_IsFirstLoad = true;
        private bool m_IsLowPerformance;

        #endregion
        
        #region inject

        private ICameraProvider               CameraProvider     { get; }
        private IContainersGetter             ContainersGetter   { get; }
        private IBackgroundTextureProviderSet TextureProviderSet { get; }
        
        private ViewMazeBackgroundTextureControllerRmazor(
            GlobalGameSettings            _GlobalGameSettings,
            ViewSettings                  _ViewSettings,
            IRemotePropertiesRmazor       _RemoteProperties,
            IModelGame                    _Model,
            IColorProvider                _ColorProvider,
            IPrefabSetManager             _PrefabSetManager,
            IRetroModeSetting             _RetroModeSetting,
            ICameraProvider               _CameraProvider,
            IContainersGetter             _ContainersGetter,
            IBackgroundTextureProviderSet _TextureProviderSet)
            : base(
                _GlobalGameSettings,
                _ViewSettings,
                _RemoteProperties,
                _Model,
                _ColorProvider, 
                _PrefabSetManager,
                _RetroModeSetting)
        {
            CameraProvider        = _CameraProvider;
            ContainersGetter      = _ContainersGetter;
            TextureProviderSet    = _TextureProviderSet;
        }

        #endregion

        #region api

        public override void Init()
        {
            CommonDataRmazor.BackgroundTextureControllerRmazor = this;
            CameraProvider.ActiveCameraChanged += OnActiveCameraChanged;
            InitTextureProviders();
            SetTextureProvidersPosition();
            InitRenderTextures();
            InitRenderCamera();
            InitMainBackgroundRenderer();
            base.Init();
        }
        
        public override void OnLevelStageChanged(LevelStageArgs _Args)
        {
            base.OnLevelStageChanged(_Args);
            switch (_Args.LevelStage)
            {
                case ELevelStage.Loaded: OnLevelLoaded(_Args);      break;
                case ELevelStage.None:   OnExitLevelStaging(_Args); break;
            }
        }
        
        public void SetAdditionalInfo(AdditionalColorPropsAdditionalInfo _AdditionalInfo)
        {
            AdditionalInfo = _AdditionalInfo;
        }

        #endregion
        
        #region nonpublic methods
        
        private void OnActiveCameraChanged(Camera _Camera)
        {
            m_MainRenderer
                .gameObject.SetParent(_Camera.transform)
                .transform.SetLocalPosXY(Vector2.zero);
        }
        
        private void OnLevelLoaded(LevelStageArgs _Args)
        {
            m_RenderCamera.enabled = true;
            ActivateAndShowBackgroundTexture(_Args);
        }

        private void OnExitLevelStaging(LevelStageArgs _Args)
        {
            m_RenderCamera.enabled = false;
        }

        private void InitTextureProviders()
        {
            TextureProviderSet.Init();
            foreach (var provider in TextureProviderSet.GetSet())
            {
                provider.Renderer.gameObject.layer = LayerMask.NameToLayer(LayerNamesCommon.Mu);
                provider.Activate(false);
            }
        }

        private void SetTextureProvidersPosition()
        {
            foreach (var texProvider in TextureProviderSet.GetSet())
                texProvider.Renderer.transform.SetLocalPosXY(Vector2.left * 300f);
        }

        private void InitMainBackgroundRenderer()
        {
            var parent = ContainersGetter.GetContainer(ContainerNamesCommon.Background);
            var go = PrefabSetManager.InitPrefab(
                parent, "background", "background_texture");
            go.name = "Main Background Renderer";
            m_MainRenderer = go.GetCompItem<MeshRenderer>("renderer");
            m_MainRendererMaterial = PrefabSetManager.InitObject<Material>(
                "materials", "main_back_main_material");
            m_MainRenderer.sharedMaterial = m_MainRendererMaterial;
            ScaleTextureToViewport(m_MainRenderer);
            m_MainRenderer.sortingOrder = SortingOrders.BackgroundTexture;
        }
        
        private void ScaleTextureToViewport(Component _Renderer)
        {
            var camera = CameraProvider.Camera;
            var tr = _Renderer.transform;
            tr.position = camera.transform.position.PlusZ(20f);
            var bds = GraphicUtils.GetVisibleBounds();
            tr.localScale = new Vector3(bds.size.x, 1f, bds.size.y) * 0.1f;
        }

        private void InitRenderTextures()
        {
            var scrSize = GraphicUtils.ScreenSize;
            foreach (float quality in TextureProviderSet.GetSet()
                .Select(_P => _P.Quality)
                .Distinct())
            {
                var renderTexture = new RenderTexture(
                    (int) (scrSize.x * quality),
                    (int) (scrSize.y * quality), 0);
                m_RenderTexturesDict.Add(quality, renderTexture);
            }
        }

        private void InitRenderCamera()
        {
            var camGo = new GameObject("Background Texture Camera");
            var container = ContainersGetter.GetContainer(ContainerNamesCommon.Background);
            camGo.SetParent(container);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.transform.SetLocalPosXY(Vector2.left * 300f).SetLocalPosZ(-1f);
            cam.depth = 2f;
            cam.orthographicSize = CameraProvider.Camera.orthographicSize;
            cam.cullingMask = LayerMask.GetMask(LayerNamesCommon.Mu);
            m_RenderCamera = cam;
            m_RenderCamera.enabled = false;
        }

        protected override void LoadSets()
        {
            base.LoadSets();
            m_Triangles2TextureSetItems = RemoteProperties.Tria2TextureSet;
            if (m_Triangles2TextureSetItems.NullOrEmpty())
            {
                var triangles2TextureSet = PrefabSetManager.GetObject<Triangles2TexturePropsSetScriptableObject>
                    (CommonPrefabSetNames.Configs, "triangles2_texture_set");
                m_Triangles2TextureSetItems = triangles2TextureSet.set;
            }
            static int CalculateTextureHash(string _Value)
            {
                ulong hashedValue = 3074457345618258791ul;
                for(int i=0; i<_Value.Length; i++)
                {
                    hashedValue += _Value[i];
                    hashedValue *= 3074457345618258799ul;
                }
                return (int)hashedValue;
            }
            m_TextureSetItems = m_Triangles2TextureSetItems
                .Cast<ITextureProps>()
                .OrderBy(_Item => CalculateTextureHash(
                    _Item.ToString(null, null)))
                
                .Where(_Props => _Props.InUse)
                .ToList();
        }

        protected override void ActivateAndShowBackgroundTexture(LevelStageArgs _Args)
        {
            long levelIndex = _Args.LevelIndex;
            ActivateConcreteBackgroundTexture(levelIndex, out var provider);
            int group = RmazorUtils.GetLevelsGroupIndex(levelIndex);
            long firstLevInGroup = RmazorUtils.GetFirstLevelInGroupIndex(group);
            bool predicate = levelIndex == firstLevInGroup || m_IsFirstLoad;
            var colFrom1 = predicate ? BackCol1Current : BackCol1Prev;
            var colFrom2 = predicate ? BackCol2Current : BackCol2Prev;
            m_IsFirstLoad = false;
            provider.Show(
                MathUtils.Epsilon, 
                colFrom1, 
                colFrom2, 
                BackCol1Current, 
                BackCol2Current);
        }

        private void ActivateConcreteBackgroundTexture(
            long                           _LevelIndex,
            out IFullscreenTextureProvider _Provider)
        {
            var set = TextureProviderSet.GetSet();
            foreach (var texProvider in set)
                texProvider.Activate(false);
            if (RenderingMustBeDisabled())
            {
                _Provider = TextureProviderSet.GetProvider("empty");
                m_MainRenderer.sharedMaterial = _Provider.Renderer.material;
            }
            else
            {
                m_MainRenderer.sharedMaterial = m_MainRendererMaterial;
                int group = RmazorUtils.GetLevelsGroupIndex(_LevelIndex);
                _Provider = TextureProviderSet.GetProvider(AdditionalInfo.backgroundName);
                switch (_Provider)
                {
                    case IFullscreenTextureProviderTriangles2 triangles2:
                        int idx2 = group % m_TextureSetItems.Count;
                        var setItem = m_TextureSetItems[idx2];
                        triangles2.Activate(true);
                        triangles2.SetProperties((Triangles2TextureProps)setItem);
                        break;
                }
            }
            _Provider.Activate(true);
            var renderTexture = m_RenderTexturesDict[_Provider.Quality];
            m_RenderCamera.targetTexture = renderTexture;
            m_MainRendererMaterial.SetTexture(MainTexId, renderTexture);
        }

        private static bool RenderingMustBeDisabled()
        {
            return false;
            // return CommonDataRmazor.IsBigLevel;
        }
        
        #endregion
    }
}