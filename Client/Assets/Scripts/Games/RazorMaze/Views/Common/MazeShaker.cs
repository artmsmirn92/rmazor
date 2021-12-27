﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DI.Extensions;
using Games.RazorMaze.Models;
using Games.RazorMaze.Views.ContainerGetters;
using Games.RazorMaze.Views.MazeItems;
using Ticker;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace Games.RazorMaze.Views.Common
{
    public interface IMazeShaker : IInit, IOnLevelStageChanged
    {
        bool ShakeMaze { get; set; }
        IEnumerator HitMazeCoroutine(CharacterMovingEventArgs _Args);
        IEnumerator ShakeMazeCoroutine();
        void OnCharacterDeathAnimation(
            Vector2             _DeathPosition,
            List<IViewMazeItem> _MazeItems,
            UnityAction         _OnFinish);
    }
    
    public class MazeShaker : IMazeShaker, IUpdateTick
    {
        #region nonpublic members

        private Transform m_MazeContainer;
        private Vector3 m_StartPosition;
        private bool m_ShakeMaze;

        #endregion
        
        #region inject

        private IContainersGetter ContainersGetter { get; }
        private IMazeCoordinateConverter CoordinateConverter { get; }
        private IViewGameTicker GameTicker { get; }

        public MazeShaker(
            IContainersGetter _ContainersGetter, 
            IMazeCoordinateConverter _CoordinateConverter,
            IViewGameTicker _GameTicker)
        {
            ContainersGetter = _ContainersGetter;
            CoordinateConverter = _CoordinateConverter;
            GameTicker = _GameTicker;
            _GameTicker.Register(this);
        }

        #endregion

        #region api

        public bool ShakeMaze
        {
            get => m_ShakeMaze;
            set
            {
                m_ShakeMaze = value;
                if (!value)
                    m_MazeContainer.position = m_StartPosition;
            }
        }

        public bool              Initialized { get; private set; }
        public event UnityAction Initialize;
        
        public void Init()
        {
            m_MazeContainer = ContainersGetter.GetContainer(ContainerNames.Maze);
            Initialize?.Invoke();
            Initialized = true;
        }
        
        public void OnLevelStageChanged(LevelStageArgs _Args)
        {
            if (_Args.Stage == ELevelStage.Loaded)
                m_StartPosition = CoordinateConverter.GetMazeCenter();
        }

        public IEnumerator HitMazeCoroutine(CharacterMovingEventArgs _Args)
        {
            const float amplitude = 0.5f;
            var dir = RazorMazeUtils.GetDirectionVector(_Args.Direction, MazeOrientation.North);
            const float duration = 0.1f;
            yield return Coroutines.Lerp(
                0f,
                1f,
                duration,
                _Progress =>
                {
                    float distance = _Progress < 0.5f ? _Progress * amplitude : (1f - _Progress) * amplitude;
                    var res = (Vector2)m_StartPosition + distance * dir;
                    m_MazeContainer.position = res;
                },
                GameTicker,
                (_Finished, _Progress) =>
                {
                    m_MazeContainer.position = m_StartPosition;
                });
        }

        public IEnumerator ShakeMazeCoroutine()
        {
            const float duration = 1f;
            yield return Coroutines.Lerp(
                1f,
                0f,
                duration,
                _Progress =>
                {
                    float amplitude = 0.5f * _Progress;
                    Vector2 res;
                    res.x = m_StartPosition.x + amplitude * Mathf.Sin(GameTicker.Time * 200f);
                    res.y = m_StartPosition.y + amplitude * Mathf.Cos(GameTicker.Time * 100f);
                    m_MazeContainer.position = res;
                },
                GameTicker,
                (_Finished, _Progress) =>
                {
                    m_MazeContainer.position = m_StartPosition;
                });
        }
        
        public void UpdateTick()
        {
            if (!Initialized)
                return;
            if (!m_ShakeMaze)
                return;
            float amplitude = 0.1f;
            Vector2 res;
            res.x = m_StartPosition.x + amplitude * Mathf.Sin(GameTicker.Time * 200f);
            res.y = m_StartPosition.y + amplitude * Mathf.Cos(GameTicker.Time * 100f);
            m_MazeContainer.position = res;
        }
        
         public void OnCharacterDeathAnimation(
            Vector2             _DeathPosition,
            List<IViewMazeItem> _MazeItems,
            UnityAction         _OnFinish)
        {
            const float scaleCoeff = 0.2f;
            const float maxDistance = 10f;
            const float transitionTime = 0.5f;
            var shapes = _MazeItems
                .SelectMany(_Item => _Item.Shapes.Where(_Shape => _Shape.IsNotNull()))
                .ToList();
            var finished = shapes
                .ToDictionary(_Shape => _Shape, _Shape => false);
            foreach (var shape in shapes)
            {
                float dist = Vector2.Distance(_DeathPosition, shape.transform.position);
                if (dist > maxDistance * CoordinateConverter.Scale)
                {
                    finished[shape] = true;
                    continue;
                }

                var startLocalScale = shape.transform.localScale;
                Coroutines.Run(Coroutines.Delay(
                    Mathf.Max(0f, (dist - 1) * 0.005f),
                    () =>
                    {
                        Coroutines.Run(Coroutines.Lerp(
                            0f,
                            1f,
                            transitionTime,
                            _Progress =>
                            {
                                var scale = startLocalScale * (1f + _Progress * scaleCoeff);
                                shape.transform.localScale = scale;
                            },
                            GameTicker,
                            (_, __) =>
                            {
                                shape.transform.localScale = startLocalScale;
                                finished[shape] = true;
                            },
                            _ProgressFormula: _P => _P < 0.5f ? 2f * _P : 2f * (1f - _P)));
                    }));
            }
            Coroutines.Run(Coroutines.WaitWhile(
                () => finished.Values.Any(_F => !_F),
                () => _OnFinish?.Invoke()));
        }

        #endregion
    }
}