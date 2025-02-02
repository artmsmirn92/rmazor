﻿#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Constants;
using mazing.common.Runtime;
using mazing.common.Runtime.Constants;
using mazing.common.Runtime.Entities;
using mazing.common.Runtime.Exceptions;
using mazing.common.Runtime.Extensions;
using mazing.common.Runtime.Providers;
using mazing.common.Runtime.Utils;
using RMAZOR.Controllers;
using RMAZOR.Models.MazeInfos;
using RMAZOR.Views.Common;
using RMAZOR.Views.MazeItems;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using static RMAZOR.Models.ComInComArg;

namespace RMAZOR
{
    [InitializeOnLoad]
    public class LevelDesigner : MonoBehaviour
    {
        #region singleton

        public static LevelDesigner Instance =>
            _instance.IsNotNull() ? _instance : _instance = FindObjectOfType<LevelDesigner>();

        private static LevelDesigner _instance;

        #endregion

        #region serialized fields

        [HideInInspector] public HeapReorderableList    levelsList;
        [SerializeField]  public List<ViewMazeItemProt> maze;

        [HideInInspector] public int        width;
        [HideInInspector] public int        height;
        [HideInInspector] public string     pathLengths;
        [HideInInspector] public float      aParam;
        [HideInInspector] public bool       valid;
        [HideInInspector] public V2Int      size;
        [HideInInspector] public int        loadedLevelIndex     = -1;
        [HideInInspector] public int        loadedLevelHeapIndex = -1;
        [HideInInspector] public GameObject mazeObject;

        #endregion

        #region nonpublic members

        private static MazeInfo MazeInfo
        {
            get => SaveUtilsInEditor.GetValue(SaveKeysInEditor.DesignerMazeInfo);
            set => SaveUtilsInEditor.PutValue(SaveKeysInEditor.DesignerMazeInfo, value);
        }


        #endregion

        #region static ctor

        static LevelDesigner()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        #endregion

        #region api

        public static UnityAction SceneUnloaded;

        public static int LevelDesignerHeapIndex
        {
            get => SaveUtilsInEditor.GetValue(SaveKeysInEditor.DesignerHeapIndex);
            set => SaveUtilsInEditor.PutValue(SaveKeysInEditor.DesignerHeapIndex, value);
        }

        public MazeInfo GetLevelInfoFromScene()
        {
            mazeObject = GameObject.Find(ContainerNamesMazor.MazeItems);
            maze = new List<ViewMazeItemProt>();
            foreach (Transform mazeObj in mazeObject.transform)
            {
                var mazeItem = mazeObj.gameObject.GetComponent<ViewMazeItemProt>();
                if (mazeItem.IsNotNull())
                    maze.Add(mazeItem);
            }
            var protItemStart = maze.FirstOrDefault(_Item => _Item.Props.IsStartNode);
            if (protItemStart == null)
                Dbg.LogWarning("Maze must contain start item");
            var pathItems = maze
                .Where(_Item => _Item.Props.IsNode && !_Item.Props.IsStartNode)
                .Select(_Item => new PathItem {Blank = _Item.Props.Blank, Position = _Item.Props.Position})
                .ToList();
            if (protItemStart.IsNotNull())
            {
                pathItems.Insert(
                    0,
                    new PathItem
                    {
                        Blank = protItemStart!.Props.Blank,
                        Position = protItemStart!.Props.Position
                    });
            }
            var mazeProtItems = maze
                .Where(_Item => !_Item.Props.IsNode)
                .Select(_Item => _Item.Props).ToList();
            int maxX = mazeProtItems.Any() ? mazeProtItems.Max(_Item => _Item.Position.X + 1) : 0;
            maxX = pathItems.Any() ? Math.Max(maxX, pathItems.Max(_Item => _Item.Position.X + 1)) : maxX;
            int maxY = mazeProtItems.Any() ? mazeProtItems.Max(_Item => _Item.Position.Y + 1) : 0;
            maxY = pathItems.Any() ? Math.Max(maxY, pathItems.Max(_Item => _Item.Position.Y + 1)) : maxY;
            var mazeSize = new V2Int(maxX, maxY);
            return new MazeInfo
            {
                Size = mazeSize,
                PathItems = pathItems,
                MazeItems = mazeProtItems
                    .SelectMany(_Item =>
                    {
                        var dirs = _Item.Directions.Clone();
                        if (!dirs.Any())
                            dirs.Add(V2Int.Zero);
                        return dirs.Select(_Dir =>
                            new MazeItem
                            {
                                Directions = new List<V2Int> {_Dir},
                                Pair     = _Item.Pair,
                                Path     = _Item.Path,
                                Type     = _Item.Type,
                                Position = _Item.Position,
                                Blank    = _Item.Blank,
                                Args     = _Item.Args
                            });
                    }).ToList()
            };
        }

        public V2Int GetEnteredMazeSize()
        {
            return new V2Int(width, height);
        }

        #endregion

        #region nonpublic methods

        private static void OnPlayModeStateChanged(PlayModeStateChange _Change)
        {
            if (!SceneManager.GetActiveScene().name.Contains(SceneNames.Prototyping))
                return;
            switch (_Change)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    MazeInfo = Instance.GetLevelInfoFromScene();
                    CommonDataMazor.Release = false;
                    SceneManager.sceneLoaded -= OnSceneLoaded;
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    Cor.Run(LoadSceneLevel());
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }

        private static IEnumerator LoadSceneLevel()
        {
            var @params = new LoadSceneParameters(LoadSceneMode.Single);
            var op = SceneManager.LoadSceneAsync(SceneNames.Level, @params);
            while (!op.isDone)
                yield return null;
        }

        private static void OnSceneUnloaded(PlayModeStateChange _State)
        {
#if UNITY_EDITOR
            SceneUnloaded?.Invoke();
            EditorApplication.playModeStateChanged -= OnSceneUnloaded;
#endif
        }

        private static void OnSceneLoaded(Scene _Scene, LoadSceneMode _Mode)
        {
            if (_Scene.name != SceneNames.Level)
                return;
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnSceneUnloaded;
#endif
            var compLogo = FindObjectOfType<CompanyLogoMonoBeh>();
            if (compLogo.IsNotNull())
                compLogo.EnableLogo(false);
            var colorProvider = FindObjectOfType<ColorProvider>();
            colorProvider.SetColor(ColorIds.Character2, Color.black);
            Application.targetFrameRate = GraphicUtils.GetTargetFps();
            var controller = GameControllerMVC.CreateInstance();
            controller.Initialize += () =>
            {
                int selectedLevel = SaveUtilsInEditor.GetValue(SaveKeysInEditor.DesignerSelectedLevel);
                var args = new Dictionary<string, object>
                {
                    {KeyGameMode,      ParameterGameModeMain},
                    {KeyNextLevelType, ParameterLevelTypeDefault},
                };
                controller.Model.LevelStaging.LoadLevel(MazeInfo, selectedLevel, args);
            };
            controller.Init();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #endregion

        #region engine methods

        [RuntimeInitializeOnLoadMethod]
        public static void ResetState()
        {
            _instance = null;
        }

        #endregion
    }
}

#endif