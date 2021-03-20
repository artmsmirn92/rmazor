﻿using System;
using System.Collections.Generic;
using Entities;
using Exceptions;
using Extensions;
using Games.RazorMaze.Models;
using Shapes;
using UnityEngine;

namespace Games.RazorMaze.Prot
{
    [ExecuteInEditMode, Serializable]
    public class MazeProtItem : MonoBehaviour
    {
        public Rectangle rectangle;
        [SerializeField] private MazeItemType type;
        [SerializeField] public V2Int start;
        [SerializeField] public List<V2Int> path;
        [SerializeField, HideInInspector] private int m_Size; 
        

        public MazeItemType Type
        {
            get => type;
            set
            {
                type = value;
                rectangle.Color = ColorByType(type);
            }
        }

        public void Init(PrototypingItemProps _Props)
        {
            type = _Props.Type;
            start = _Props.Position;
            path = _Props.Path;
            m_Size = _Props.Size;
            rectangle = gameObject.AddComponent<Rectangle>();
            var converter = new CoordinateConverter();
            converter.Init(m_Size);
            transform.localPosition = converter.ToLocalMazeItemPosition(start);
            rectangle.Width = 0.97f * converter.GetScale();
            rectangle.Height = 0.97f * converter.GetScale();
            rectangle.Type = Rectangle.RectangleType.RoundedSolid;
            rectangle.CornerRadius = 0.1f;
            rectangle.Color = ColorByType(type);
            switch (_Props.Type)
            {
                case MazeItemType.NodeStart:
                case MazeItemType.Node: 
                    rectangle.SortingOrder = 0; 
                    break;
                case MazeItemType.ObstacleTrap:
                case MazeItemType.ObstacleTrapMoving:
                    rectangle.SortingOrder = 1;
                    break;
                case MazeItemType.Obstacle: 
                    rectangle.SortingOrder = 2; 
                    break;
                case MazeItemType.ObstacleMoving:
                case MazeItemType.ObstacleTrapMovingFree:
                    rectangle.SortingOrder = 10;
                    break;
                default: throw new SwitchCaseNotImplementedException(_Props.Type);
            }
        }

        public void SetLocalPosition(Vector2 _Position)
        {
            transform.localPosition = _Position;
        }

        public bool Equal(Obstacle _Obstacle)
        {
            return _Obstacle.Path == path && _Obstacle.Type == RazorMazePrototypingUtils.GetObstacleType(type);
        }

        private static Color ColorByType(MazeItemType _Type)
        {
            switch (_Type)
            {
                case MazeItemType.Node:                  return Color.white;
                case MazeItemType.NodeStart:             return Color.yellow;
                case MazeItemType.Obstacle:              return new Color(0.53f, 0.53f, 0.53f, 0.8f);
                case MazeItemType.ObstacleMoving:        return Color.blue;
                case MazeItemType.ObstacleTrap:          return Color.red;
                case MazeItemType.ObstacleTrapMoving:    return Color.magenta;
                default: throw new SwitchCaseNotImplementedException(_Type);
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;
            var converter = new CoordinateConverter();
            converter.Init(m_Size);
            Func<V2Int, Vector2> addConv = _V => converter.ToLocalMazeItemPosition(_V).PlusY(converter.GetCenter().y); 
            switch (Type)
            {
                case MazeItemType.ObstacleMoving:
                case MazeItemType.ObstacleTrap:
                    if (path == null || path.Count <= 1)
                        return;
                    Gizmos.color = Type == MazeItemType.ObstacleMoving ? Color.blue : Color.red;
                    for (int i = 0; i < path.Count; i++)
                    {
                        var pos = path[i];
                        Gizmos.DrawSphere(addConv(pos), 1);
                        if (i == path.Count - 1)
                            return;
                        Gizmos.DrawLine(addConv(pos), addConv(path[i + 1]));
                    }
                    break;
                case MazeItemType.Node:
                case MazeItemType.NodeStart:
                case MazeItemType.Obstacle:
                case MazeItemType.ObstacleTrapMoving:
                    //do nothing
                    break;
                default: throw new SwitchCaseNotImplementedException(Type);
            }
        }
    }

    public enum MazeItemType
    {
        Node,
        NodeStart,
        Obstacle,
        ObstacleMoving,
        ObstacleTrap,
        ObstacleTrapMoving,
        ObstacleTrapMovingFree
    }
    
    public class PrototypingItemProps
    {
        public MazeItemType Type { get; set; }
        public V2Int Position { get; set; }
        public List<V2Int> Path { get; set; } = new List<V2Int>();
        public int Size { get; set; }
    }
}