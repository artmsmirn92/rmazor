﻿using System.Collections.Generic;
using System.Linq;
using mazing.common.Runtime.CameraProviders;
using mazing.common.Runtime.Constants;
using mazing.common.Runtime.Entities;
using mazing.common.Runtime.Enums;
using mazing.common.Runtime.Extensions;
using mazing.common.Runtime.Helpers;
using mazing.common.Runtime.Managers;
using mazing.common.Runtime.Providers;
using mazing.common.Runtime.SpawnPools;
using mazing.common.Runtime.Ticker;
using RMAZOR.Models;
using Shapes;
using UnityEngine;
using static RMAZOR.Models.ComInComArg;

namespace RMAZOR.Views.Common.CongratulationItems
{
    public class ViewMazeBackgroundCongratItems2
        : ViewMazeBackgroundItemsBase,
          IViewMazeBackgroundCongratItems
    {
        #region nonpublic members
        
        private readonly SpawnPool<Firework>       m_BackCongratsItemsPool = new SpawnPool<Firework>();
        private readonly Dictionary<Firework, int> m_AudioClipIndices      = new Dictionary<Firework, int>();
        
        private float m_LastCongratsItemAnimTime;
        private float m_NextRandomCongratsItemAnimInterval;
        private bool  m_DoAnimateCongrats;

        #endregion

        #region inject
        
        private IPrefabSetManager PrefabSetManager { get; }
        private IAudioManager     AudioManager     { get; }

        private ViewMazeBackgroundCongratItems2(
            IColorProvider              _ColorProvider,
            IContainersGetter           _ContainersGetter,
            IViewGameTicker             _GameTicker,
            ICameraProvider             _CameraProvider,
            IPrefabSetManager           _PrefabSetManager,
            IAudioManager               _AudioManager)
            : base(
                _ColorProvider,
                _ContainersGetter,
                _GameTicker,
                _CameraProvider)
        {
            PrefabSetManager = _PrefabSetManager;
            AudioManager     = _AudioManager;
        }

        #endregion

        #region api
        
        public void OnLevelStageChanged(LevelStageArgs _Args)
        {
            m_DoAnimateCongrats = false;
            switch (_Args.LevelStage)
            {
                case ELevelStage.Finished:
                    m_DoAnimateCongrats = AreCongratItemsAvailableOnThisLevel(_Args);  
                    break;
                case ELevelStage.Unloaded: 
                    m_BackCongratsItemsPool.DeactivateAll();
                    break;
            }
        }
        
        public override void UpdateTick()
        {
            if (!Initialized)
                return;
            if (!m_DoAnimateCongrats)
                return;
            ProceedItems();
        }

        #endregion

        #region nonpublic methods
        
        protected override void InitItems()
        {
            var sourceGo = PrefabSetManager.GetPrefab(
                "background", "background_item_congrats_alt_1");
            for (int i = 0; i < PoolSize; i++)
            {
                var newGo = Object.Instantiate(sourceGo);
                newGo.SetParent(ContainersGetter.GetContainer(ContainerNamesCommon.Background));
                var firework = newGo.GetCompItem<Firework>("firework");
                var content = newGo.GetCompItem<Transform>("content");
                var discs = content.GetComponentsInChildren<Disc>().ToArray();
                var bodies = content.GetComponentsInChildren<Rigidbody>().ToArray();
                firework.InitFirework(discs, bodies, GameTicker);
                m_BackCongratsItemsPool.Add(firework);
                int randAudioIdx = Mathf.FloorToInt(1 + Random.value * 4.9f);
                m_AudioClipIndices.Add(firework, randAudioIdx);
                AudioManager.InitClip(GetAudioClipArgs(firework));
            }
            m_BackCongratsItemsPool.DeactivateAll(true);
            sourceGo.DestroySafe();
        }

        protected override void ProceedItems()
        {
            if (!(GameTicker.Time > m_NextRandomCongratsItemAnimInterval + m_LastCongratsItemAnimTime)) 
                return;
            m_LastCongratsItemAnimTime = GameTicker.Time;
            m_NextRandomCongratsItemAnimInterval = 0.05f + Random.value * 0.15f;
            var firework = m_BackCongratsItemsPool.FirstInactive;
            var tr = firework.transform;
            tr.position = RandomPositionOnScreen();
            tr.localScale = Vector3.one * (0.5f + 1.5f * Random.value);
            m_BackCongratsItemsPool.Activate(firework);
            firework.LaunchFirework();
            if (Random.value < 0.3f)
                AudioManager.PlayClip(GetAudioClipArgs(firework));
        }

        private AudioClipArgs GetAudioClipArgs(Firework _Firework)
        {
            int idx = m_AudioClipIndices[_Firework];
            return new AudioClipArgs($"firework_{idx}", EAudioClipType.GameSound, _Id: _Firework.GetInstanceID().ToString());
        }

        private bool AreCongratItemsAvailableOnThisLevel(LevelStageArgs _Args)
        {
            string gameMode = (string) _Args.Arguments.GetSafe(KeyGameMode, out _);
            if (gameMode != ParameterGameModeDailyChallenge)
                return true;
            bool isChallengeSuccessfullyFinished = (bool) _Args.Arguments.GetSafe(KeyIsDailyChallengeSuccess, out _);
            return isChallengeSuccessfullyFinished;
        }

        #endregion
    }
}