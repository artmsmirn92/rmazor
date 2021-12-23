﻿using System.Collections;
using System.Text;
using Constants;
using DI.Extensions;
using Entities;
using GameHelpers;
using Games.RazorMaze.Models;
using Games.RazorMaze.Views.Common;
using Games.RazorMaze.Views.ContainerGetters;
using Games.RazorMaze.Views.InputConfigurators;
using Shapes;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace Games.RazorMaze.Views.UI
{
    public enum ETutorialType
    {
        Movement,
        Rotation
    }
    
    public interface IViewUITutorial : IOnLevelStageChanged, IInitViewUIItem
    {
        event UnityAction<ETutorialType> TutorialStarted;
        event UnityAction<ETutorialType> TutorialFinished;
    }
    
    public class ViewUITutorial : IViewUITutorial
    {
        #region constants

        #endregion

        #region nonpublic members

        private static int AkMoveLeftPrompt         => AnimKeys.Anim;
        private static int AkMoveRightPrompt        => AnimKeys.Anim2;
        private static int AkMoveDownPrompt         => AnimKeys.Anim3;
        private static int AkMoveUpPrompt           => AnimKeys.Anim4;
        private static int AkRotateClockwise        => AnimKeys.Anim;
        private static int AkRotateCounterClockwise => AnimKeys.Anim2;
        private static int AkIdlePrompt             => AnimKeys.Stop;
        
        private bool     m_MovementTutorialStarted;
        private bool     m_RotationTutorialStarted;
        private bool     m_MovementTutorialFinished;
        private bool     m_RotationTutorialFinished;
        private bool     m_ReadyToSecondMovementStep;
        private bool     m_ReadyToThirdMovementStep;
        private bool     m_ReadyToFourthMovementStep;
        private bool     m_ReadyToFinishMovementTutorial;
        private bool     m_ReadyToSecondRotationStep;
        private bool     m_ReadyToFinishRotationTutorial;
        private Vector4  m_Offsets;
        private Animator m_MovePrompt;
        private Animator m_RotatePrompt;

        #endregion

        #region inject

        private IModelGame                  Model               { get; }
        private IPrefabSetManager           PrefabSetManager    { get; }
        private IContainersGetter           ContainersGetter    { get; }
        private IMazeCoordinateConverter    CoordinateConverter { get; }
        private IViewInputCommandsProceeder CommandsProceeder   { get; }
        private ICameraProvider             CameraProvider      { get; }
        private IColorProvider              ColorProvider       { get; }

        public ViewUITutorial(
            IModelGame _Model,
            IPrefabSetManager _PrefabSetManager,
            IContainersGetter _ContainersGetter,
            IMazeCoordinateConverter _CoordinateConverter,
            IViewInputCommandsProceeder _CommandsProceeder,
            ICameraProvider _CameraProvider,
            IColorProvider _ColorProvider)
        {
            Model = _Model;
            PrefabSetManager = _PrefabSetManager;
            ContainersGetter = _ContainersGetter;
            CoordinateConverter = _CoordinateConverter;
            CommandsProceeder = _CommandsProceeder;
            CameraProvider = _CameraProvider;
            ColorProvider = _ColorProvider;
        }

        #endregion

        #region api
        
        public event UnityAction<ETutorialType> TutorialStarted;
        public event UnityAction<ETutorialType> TutorialFinished;
        
        public void Init(Vector4 _Offsets)
        {
            m_Offsets = _Offsets;
            m_MovementTutorialFinished = SaveUtils.GetValue(SaveKeys.MovementTutorialFinished);
            m_RotationTutorialFinished = SaveUtils.GetValue(SaveKeys.RotationTutorialFinished);
            CommandsProceeder.Command += OnCommand;
        }
        
        public void OnLevelStageChanged(LevelStageArgs _Args)
        {
            if (m_MovementTutorialStarted)
                m_MovePrompt.speed = _Args.Stage == ELevelStage.Paused ? 0f : 1f;
            if (m_RotationTutorialStarted)
                m_RotatePrompt.speed = _Args.Stage == ELevelStage.Paused ? 0f : 1f;
            switch (_Args.Stage)
            {
                case ELevelStage.Loaded:
                    switch (Model.Data.Info.Comment)
                    {
                        case "movement tutorial": StartMovementTutorial(); break;
                        case "rotation tutorial": StartRotationTutorial(); break;
                    }
                    break;
            }
        }

        #endregion

        #region nonpublic methods
        
        private void OnCommand(EInputCommand _Command, object[] _Args)
        {
            if (m_MovementTutorialStarted && !m_MovementTutorialFinished)
                OnMovementCommand(_Command);
            if (m_RotationTutorialStarted && !m_RotationTutorialFinished)
                OnRotationCommand(_Command);
        }

        private void OnMovementCommand(EInputCommand _Command)
        {
            switch (_Command)
            {
                case EInputCommand.MoveRight:
                    m_ReadyToSecondMovementStep = true;
                    break;
                case EInputCommand.MoveUp:
                    m_ReadyToThirdMovementStep = true;
                    break;
                case EInputCommand.MoveLeft:
                    m_ReadyToFourthMovementStep = true;
                    break;
                case EInputCommand.MoveDown:
                    m_ReadyToFinishMovementTutorial = true;
                    break;
            }
        }

        private void OnRotationCommand(EInputCommand _Command)
        {
            switch (_Command)
            {
                case EInputCommand.RotateCounterClockwise:
                    m_ReadyToSecondRotationStep = true;
                    break;
                case EInputCommand.RotateClockwise:
                    m_ReadyToFinishRotationTutorial = true;
                    break;
            }   
        }

        private void StartMovementTutorial()
        {
            if (m_MovementTutorialStarted || m_MovementTutorialFinished)
                return;
            TutorialStarted?.Invoke(ETutorialType.Movement);
            var cont = ContainersGetter.GetContainer(ContainerNames.Tutorial);
            var goMovePrompt = PrefabSetManager.InitPrefab(
                cont, "tutorials", "hand_swipe_movement");
            goMovePrompt.transform.localScale = Vector3.one * 6f;
            m_MovePrompt = goMovePrompt.GetCompItem<Animator>("animator");
            var uiCol = ColorProvider.GetColor(ColorIds.UI);
            var handRend = goMovePrompt.GetCompItem<SpriteRenderer>("hand");
            handRend.color = uiCol.SetA(handRend.color.a);
            var traceRend = goMovePrompt.GetCompItem<Triangle>("trace");
            traceRend.Color = uiCol.SetA(traceRend.Color.a);
            Coroutines.Run(MovementTutorialFirstStepCoroutine());
            m_MovementTutorialStarted = true;
        }

        private void StartRotationTutorial()
        {
            if (m_RotationTutorialStarted || m_RotationTutorialFinished)
                return;
            TutorialStarted?.Invoke(ETutorialType.Rotation);
            var cont = ContainersGetter.GetContainer(ContainerNames.Tutorial);
            var goRotPrompt = PrefabSetManager.InitPrefab(
                cont, "tutorials", "hand_swipe_rotation");
            goRotPrompt.transform.localScale = Vector3.one * 6f;
            m_RotatePrompt = goRotPrompt.GetCompItem<Animator>("animator");
            var uiCol = ColorProvider.GetColor(ColorIds.UI);
            var handRend = goRotPrompt.GetCompItem<SpriteRenderer>("hand");
            handRend.color = uiCol.SetA(handRend.color.a);
            var trace1Rend = goRotPrompt.GetCompItem<Triangle>("trace_1");
            trace1Rend.Color = uiCol.SetA(trace1Rend.Color.a);
            var trace2Rend = goRotPrompt.GetCompItem<Triangle>("trace_2");
            trace2Rend.Color = uiCol.SetA(trace2Rend.Color.a);
            CommandsProceeder.LockCommands(RazorMazeUtils.GetMoveCommands(), GetGroupName());
            Coroutines.Run(RotationTutorialFirstStepCoroutine());
            m_RotationTutorialStarted = true;
        }

        
        private IEnumerator MovementTutorialFirstStepCoroutine()
        {
            m_MovePrompt.SetTrigger(AkMoveRightPrompt);
            m_MovePrompt.transform.SetPosXY(
                CoordinateConverter.GetMazeCenter().x,
                GetScreenBounds().min.y + m_Offsets.z + 10f);
            CommandsProceeder.LockCommands(RazorMazeUtils.GetMoveCommands(), GetGroupName());
            CommandsProceeder.UnlockCommand(EInputCommand.MoveRight, GetGroupName());
            while (!m_ReadyToSecondMovementStep)
                yield return null;
            yield return MovementTutorialSecondStepCoroutine();
        }
        
        private IEnumerator MovementTutorialSecondStepCoroutine()
        {
            m_MovePrompt.SetTrigger(AkMoveUpPrompt);
            m_MovePrompt.transform.SetPosXY(
                GetScreenBounds().max.x - m_Offsets.y - 5f,
                CoordinateConverter.GetMazeCenter().y);
            CommandsProceeder.LockCommands(RazorMazeUtils.GetMoveCommands(), GetGroupName());
            CommandsProceeder.UnlockCommand(EInputCommand.MoveUp, GetGroupName());
            while (!m_ReadyToThirdMovementStep)
                yield return null;
            yield return MovementTutorialThirdStepCoroutine();
        }

        private IEnumerator MovementTutorialThirdStepCoroutine()
        {
            m_MovePrompt.SetTrigger(AkMoveLeftPrompt);
            m_MovePrompt.transform.SetPosXY(
                CoordinateConverter.GetMazeCenter().x,
                GetScreenBounds().min.y +  m_Offsets.z + 10f);
            CommandsProceeder.LockCommands(RazorMazeUtils.GetMoveCommands(), GetGroupName());
            CommandsProceeder.UnlockCommand(EInputCommand.MoveLeft, GetGroupName());
            while (!m_ReadyToFourthMovementStep)
                yield return null;
            yield return MovementTutorialFourthStepCoroutine();
        }

        private IEnumerator MovementTutorialFourthStepCoroutine()
        {
            m_MovePrompt.SetTrigger(AkMoveDownPrompt);
            m_MovePrompt.transform.SetPosXY(
                GetScreenBounds().max.x - m_Offsets.y - 5f,
                CoordinateConverter.GetMazeCenter().y);
            CommandsProceeder.LockCommands(RazorMazeUtils.GetMoveCommands(), GetGroupName());
            CommandsProceeder.UnlockCommand(EInputCommand.MoveDown, GetGroupName());
            while (!m_ReadyToFinishMovementTutorial)
                yield return null;
            FinishMovementTutorial();
        }

        private void FinishMovementTutorial()
        {
            m_MovePrompt.SetTrigger(AkIdlePrompt);            
            CommandsProceeder.UnlockCommands(RazorMazeUtils.GetMoveCommands(), GetGroupName());
            m_MovementTutorialFinished = true;
            SaveUtils.PutValue(SaveKeys.MovementTutorialFinished, true);
            TutorialFinished?.Invoke(ETutorialType.Movement);
        }
        
        private IEnumerator RotationTutorialFirstStepCoroutine()
        {
            m_RotatePrompt.SetTrigger(AkRotateCounterClockwise);
            m_RotatePrompt.transform.SetPosXY(
                CoordinateConverter.GetMazeCenter().x,
                GetScreenBounds().min.y +  m_Offsets.z + 10f);
            CommandsProceeder.LockCommands(RazorMazeUtils.GetRotateCommands(), GetGroupName());
            CommandsProceeder.UnlockCommand(EInputCommand.RotateCounterClockwise, GetGroupName());
            while (!m_ReadyToSecondRotationStep)
                yield return null;
            yield return RotationTutorialSecondStepCoroutine();
        }

        private IEnumerator RotationTutorialSecondStepCoroutine()
        {
            m_RotatePrompt.SetTrigger(AkRotateClockwise);
            m_RotatePrompt.transform.SetPosXY(
                CoordinateConverter.GetMazeCenter().x,
                GetScreenBounds().min.y +  m_Offsets.z + 10f);
            CommandsProceeder.LockCommands(RazorMazeUtils.GetRotateCommands(), GetGroupName());
            CommandsProceeder.UnlockCommand(EInputCommand.RotateClockwise, GetGroupName());
            while (!m_ReadyToFinishRotationTutorial)
                yield return null;
            FinishRotationTutorial();
        }

        private void FinishRotationTutorial()
        {
            CommandsProceeder.UnlockCommands(RazorMazeUtils.GetMoveCommands(), GetGroupName());
            CommandsProceeder.UnlockCommands(RazorMazeUtils.GetRotateCommands(), GetGroupName());
            m_RotatePrompt.SetTrigger(AkIdlePrompt);
            m_RotationTutorialFinished = true;
            SaveUtils.PutValue(SaveKeys.RotationTutorialFinished, true);
            TutorialFinished?.Invoke(ETutorialType.Rotation);
        }

        private Bounds GetScreenBounds()
        {
            return GraphicUtils.GetVisibleBounds(CameraProvider.MainCamera);
        }

        private string GetGroupName()
        {
            return nameof(IViewUITutorial);
        }

        #endregion
    }
}