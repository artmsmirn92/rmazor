﻿using Ticker;
using UnityEngine.Events;

namespace Games.RazorMaze.Views.UI
{
    public class ViewUI : ViewUIBase
    {
        public ViewUI(ITicker _Ticker) : base(_Ticker)
        { }

        public override void OnBeforeLevelStarted(LevelStateChangedArgs _Args, UnityAction _StartLevel)
        {
            throw new System.NotImplementedException();
        }

        public override void OnLevelStarted(LevelStateChangedArgs _Args)
        {
            throw new System.NotImplementedException();
        }

        public override void OnLevelFinished(LevelFinishedEventArgs _Args, UnityAction _Finish)
        {
            throw new System.NotImplementedException();
        }
    }
}