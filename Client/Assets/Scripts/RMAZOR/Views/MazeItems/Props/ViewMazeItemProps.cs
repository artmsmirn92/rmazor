﻿using System;
using System.Collections.Generic;
using Common.Entities;
using mazing.common.Runtime.Entities;
using RMAZOR.Models.MazeInfos;
using RMAZOR.Models.ProceedInfos;
// ReSharper disable InconsistentNaming

namespace RMAZOR.Views.MazeItems.Props
{
    [Serializable]
    public class ViewMazeItemProps
    {
        public bool          IsNode;
        public bool          IsStartNode;
        public EMazeItemType Type;
        public V2Int         Position;
        public List<V2Int>   Path       = new List<V2Int>();
        public List<V2Int>   Directions = new List<V2Int>{V2Int.Zero};
        public V2Int         Pair;
        public bool          IsMoneyItem;
        public bool          Blank;
        public List<string>  Args = new List<string>();

        public bool Equals(IMazeItemProceedInfo _Info)
        {
            if (_Info.Type != Type)
                return false;
            if (_Info.StartPosition != Position)
                return false;
            return Directions[0] == _Info.Direction;
        }
    }
}