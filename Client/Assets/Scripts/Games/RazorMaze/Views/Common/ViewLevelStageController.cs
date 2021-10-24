﻿using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Exceptions;
using Games.RazorMaze.Models;
using Games.RazorMaze.Views.Characters;
using Games.RazorMaze.Views.InputConfigurators;
using Games.RazorMaze.Views.MazeItemGroups;
using Games.RazorMaze.Views.MazeItems;
using Mono_Installers;
using Ticker;
using Utils;

namespace Games.RazorMaze.Views.Common
{
    public interface IViewLevelStageController : IOnLevelStageChanged
    {
        void RegisterProceeders(IEnumerable<IOnLevelStageChanged> _Proceeders);
        void OnAllPathProceed(V2Int _LastPath);
    }

    public class ViewLevelStageController : IViewLevelStageController
    {
        #region constants

        private const string SoundClipNameLevelStart = "level_start";
        private const string SoundClipNameLevelComplete = "level_complete";

        #endregion
        
        #region nonpublic members

        private readonly List<IOnLevelStageChanged> m_Proceeders = new List<IOnLevelStageChanged>();

        #endregion

        #region inject

        private IGameTicker GameTicker { get; }
        private IModelGame Model { get; }
        private IManagersGetter Managers { get; }
        private IViewCharacter Character { get; }
        private IViewInputConfigurator InputConfigurator { get; }

        public ViewLevelStageController(
            IGameTicker _GameTicker,
            IModelGame _Model,
            IManagersGetter _Managers,
            IViewCharacter _Character,
            IViewInputConfigurator _InputConfigurator)
        {
            GameTicker = _GameTicker;
            Model = _Model;
            Managers = _Managers;
            Character = _Character;
            InputConfigurator = _InputConfigurator;
        }

        #endregion

        #region api

        public void RegisterProceeders(IEnumerable<IOnLevelStageChanged> _Proceeders)
        {
            m_Proceeders.Clear();
            m_Proceeders.AddRange(_Proceeders);
        }

        public void OnAllPathProceed(V2Int _LastPath)
        {
            if (LevelMonoInstaller.Release)
                Model.LevelStaging.FinishLevel();
        }

        public void OnLevelStageChanged(LevelStageArgs _Args)
        {
            foreach (var proceeder in m_Proceeders)
                proceeder.OnLevelStageChanged(_Args);
            ProceedMazeItemGroups(_Args);
            ProceedSounds(_Args);
            ProceedTime(_Args);
        }

        #endregion

        #region nonpublic methods

        private void ProceedMazeItemGroups(LevelStageArgs _Args)
        {
            var mazeItems = new List<IViewMazeItem>();
            var mazeItemGroups = m_Proceeders
                .Select(_P => _P as IViewMazeItemGroup)
                .Where(_P => _P != null);
            foreach (var group in mazeItemGroups)
                mazeItems.AddRange(group.GetActiveItems());
            var pathItemsGroup = m_Proceeders
                .First(_P => _P is IViewMazePathItemsGroup) as IViewMazePathItemsGroup;
            mazeItems.AddRange(pathItemsGroup.PathItems);
            switch (_Args.Stage)
            {
                case ELevelStage.Loaded:
                    Character.Appear(true);
                    foreach (var mazeItem in mazeItems)
                        mazeItem.Appear(true);
                    Coroutines.Run(Coroutines.WaitWhile(() =>
                        {
                            return Character.AppearingState != EAppearingState.Appeared
                                   || mazeItems.Any(_Item => _Item.AppearingState != EAppearingState.Appeared);
                        },
                        () => Model.LevelStaging.ReadyToStartOrContinueLevel()));
                    break;
                case ELevelStage.ReadyToUnloadLevel:
                    foreach (var mazeItem in mazeItems)
                        mazeItem.Appear(false);
                    Coroutines.Run(Coroutines.WaitWhile(() =>
                        {
                            return mazeItems.Any(_Item => _Item.AppearingState != EAppearingState.Dissapeared);
                        },
                        () =>
                        {
                        Model.LevelStaging.UnloadLevel();
                        }));
                    break;
                case ELevelStage.Unloaded:
#if !UNITY_EDITOR
                    InputConfigurator.RaiseCommand(InputCommands.LoadNextLevel, null);
#endif
                    break;
            }
        }

        private void ProceedTime(LevelStageArgs _Args)
        {
            GameTicker.Pause = _Args.Stage == ELevelStage.Paused;
        }

        private void ProceedSounds(LevelStageArgs _Args)
        {
            if (_Args.Stage == ELevelStage.Loaded)
                Managers.Notify(_SM => _SM.PlayClip(SoundClipNameLevelStart));
            else if (_Args.Stage == ELevelStage.Finished)
                Managers.Notify(_SM => _SM.PlayClip(SoundClipNameLevelComplete));
        }
        
        #endregion
    }
}