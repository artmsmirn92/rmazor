﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Extensions;
using Common.Managers;
using Common.Utils;
using Newtonsoft.Json;
using RMAZOR.Models;
using RMAZOR.Models.MazeInfos;
using UnityEngine;

namespace RMAZOR.Helpers
{
    public class LevelsLoaderRmazor : LevelsLoader
    {
        #region nonpublic members

        private string[]
            m_SerializedBonusLevelsFromCache  = new string[0],
            m_SerializedBonusLevelsFromRemote = new string[0];

        private bool m_CachedBonusLevelsLoaded, m_RemoteBonusLevelsLoaded;

        #endregion

        #region inject

        protected LevelsLoaderRmazor(
            IPrefabSetManager  _PrefabSetManager,
            IMazeInfoValidator _MazeInfoValidator) 
            : base(
                _PrefabSetManager,
                _MazeInfoValidator) { }

        #endregion

        #region api

        public override void Init()
        {
            PreloadLevels(CommonData.GameId, true);
            PreloadLevels(CommonData.GameId, false);
            bool InitializationPredicate()
            {
                bool mainLevelsLoaded  = CachedLevelsLoaded        || RemoteLevelsLoaded;
                bool bonusLevelsLoaded = m_CachedBonusLevelsLoaded || m_RemoteBonusLevelsLoaded;
                return mainLevelsLoaded && bonusLevelsLoaded;
            }
            Cor.Run(Cor.WaitWhile(
                InitializationPredicate,
                RaiseInitialization));
        }

        public override MazeInfo GetLevelInfo(int _GameId, long _Index, Dictionary<string, object> _Args)
        {
            PreloadLevelsIfWereNotLoaded(_GameId);
            MazeInfo Deserialize(string[] _Levels)
            {
                return _Index < _Levels.Length ? 
                    JsonConvert.DeserializeObject<MazeInfo>(_Levels[_Index]) : null;
            }
            bool isBonusLevel = IsNextLevelBonus(_Args);
            var dicRemote = isBonusLevel ? 
                m_SerializedBonusLevelsFromRemote
                : SerializedLevelsFromRemote;
            var mazeInfo = Deserialize(dicRemote);
            bool valid = MazeInfoValidator.Validate(mazeInfo, out string error);
            if (!valid)
            {
                Dbg.LogError("Remote maze info is not valid: " + error);
                var dictCached = isBonusLevel
                    ? m_SerializedBonusLevelsFromCache
                    : SerializedLevelsFromCache;
                mazeInfo = Deserialize(dictCached);
            }
            valid = MazeInfoValidator.Validate(mazeInfo, out error);
            if (valid)
                return mazeInfo;
            throw new Exception("Local maze info is not valid: " + error);
        }
        
        public override int GetLevelsCount(int _GameId, Dictionary<string, object> _Args = null)
        {
            PreloadLevelsIfWereNotLoaded(_GameId);
            if (IsNextLevelBonus(_Args))
            {
                return m_SerializedBonusLevelsFromRemote.Length > 0
                    ? m_SerializedBonusLevelsFromRemote.Length
                    : m_SerializedBonusLevelsFromCache.Length;
            }
            return SerializedLevelsFromRemote.Length > 0
                ? SerializedLevelsFromRemote.Length
                : SerializedLevelsFromCache.Length;
        }

        #endregion

        #region nonpublic methods

        protected override void PreloadLevels(int _GameId, bool _Bundle)
        {
            PreloadLevels(_GameId, _Bundle, true);
            PreloadLevels(_GameId, _Bundle, false);
        }
        
        private void PreloadLevels(int _GameId, bool _Bundle, bool _Bonus)
        {
            if (!_Bonus)
            {
                base.PreloadLevels(_GameId, _Bundle);
                return;
            }
            int heapIndex = Application.isEditor ? SaveUtilsInEditor.GetValue(SaveKeysInEditor.StartHeapIndex) : 1;
            var args = new Dictionary<string, object>
            {
                {CommonInputCommandArg.KeyNextLevelType, CommonInputCommandArg.ParameterLevelTypeBonus}
            };
            var asset = PrefabSetManager.GetObject<TextAsset>(PrefabSetName(_GameId),
                LevelsAssetName(heapIndex, args),
                _Bundle ? EPrefabSource.Bundle : EPrefabSource.Asset);
            string[] serializedLevels;
            var t = typeof(MazeInfo);
            var firstProp = t.GetProperties()[0];
            string levelsText = asset.text;
            Task.Run(() =>
            {
                levelsText = levelsText.Remove(levelsText.Length - 2, 2);
                string splitter = "{" + "\"" + firstProp.Name + "\"";
                serializedLevels = levelsText.Split(new[] {splitter, "," + splitter}, StringSplitOptions.None);
                serializedLevels = serializedLevels
                    .RemoveRange(new[] {serializedLevels[0]})
                    .Select(_MazeSerialized => splitter + _MazeSerialized).ToArray();
                if (_Bundle)
                {
                    m_SerializedBonusLevelsFromRemote = serializedLevels;
                    m_RemoteBonusLevelsLoaded = true;
                }
                else
                {
                    m_SerializedBonusLevelsFromCache = serializedLevels;
                    m_CachedBonusLevelsLoaded = true;
                }
            });
        }
        
        protected override string LevelsAssetName(int _HeapIndex, Dictionary<string, object> _Args = null)
        {
            if (!IsNextLevelBonus(_Args)) 
                return base.LevelsAssetName(_HeapIndex, _Args);
            return base.LevelsAssetName(_HeapIndex, _Args) + "_bonus";
        }

        private static bool IsNextLevelBonus(Dictionary<string, object> _Args)
        {
            string nextLevelType = (string)_Args.GetSafe(CommonInputCommandArg.KeyNextLevelType, out _);
            return nextLevelType == CommonInputCommandArg.ParameterLevelTypeBonus;
        }

        #endregion

    }
}