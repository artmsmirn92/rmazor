﻿using Entities;
using UnityEngine;

namespace Extensions
{
    public static class VectorExtensions
    {
        public static Vector2 XY    (this Vector3 _V) => new Vector2(_V.x, _V.y);
        public static Vector3 SetX  (this Vector3 _V, float _X) => new Vector3(_X, _V.y, _V.z);
        public static Vector3 SetY  (this Vector3 _V, float _Y) => new Vector3(_V.x, _Y, _V.z);
        public static Vector3 SetZ  (this Vector3 _V, float _Z) => new Vector3(_V.x, _V.y, _Z);
        public static Vector3 PlusX (this Vector3 _V, float _X) => _V.SetX(_V.x + _X);
        public static Vector3 PlusY (this Vector3 _V, float _Y) => _V.SetY(_V.y + _Y);
        public static Vector3 PlusZ (this Vector3 _V, float _Z) => _V.SetY(_V.z + _Z);
        public static Vector3 MinusX(this Vector3 _V, float _X) => _V.SetX(_V.x - _X);
        public static Vector3 MinusY(this Vector3 _V, float _Y) => _V.SetY(_V.y - _Y);
        public static Vector3 MinusZ(this Vector3 _V, float _Z) => _V.SetY(_V.z - _Z);
        public static Vector3 SetXY (this Vector3 _V, Vector2 _XY) => new Vector3(_XY.x, _XY.y, _V.z);
        
        public static Vector2 SetX  (this Vector2 _V, float _X) => new Vector2(_X, _V.y);
        public static Vector2 SetY  (this Vector2 _V, float _Y) => new Vector2(_V.x, _Y);
        public static Vector2 PlusX (this Vector2 _V, float _X) => _V.SetX(_V.x + _X);
        public static Vector2 PlusY (this Vector2 _V, float _Y) => _V.SetY(_V.y + _Y);
        public static Vector2 MinusX(this Vector2 _V, float _X) => _V.SetX(_V.x - _X);
        public static Vector2 MinusY(this Vector2 _V, float _Y) => _V.SetY(_V.y - _Y);
        
        public static float Angle2D(this Vector2 _V) => Vector2.Angle(_V, Vector2.right) * _V.y > 0 ? 1 : -1;
        public static Vector2Int ToVector2Int(this Vector2 _V) => new Vector2Int(Mathf.RoundToInt(_V.x), Mathf.RoundToInt(_V.y));
        public static Vector2 ToVector3(this Vector2 _V) => Vector3.zero.SetXY(_V);
        
        public static Vector2 Rotate(this Vector2 _V, float _Angle)
        {
            float sin = Mathf.Sin(_Angle);
            float cos = Mathf.Cos(_Angle);
            float tx = _V.x;
            float ty = _V.y;
            _V.x = cos * tx - sin * ty;
            _V.y = sin * tx + cos * ty;
            return _V;
        }
        
        public static V2Int ToV2Int(this Vector2Int _V) => new V2Int(_V.x, _V.y);
        public static V2Int ToV2IntFloor(this Vector2 _V) => new V2Int(Mathf.FloorToInt(_V.x), Mathf.FloorToInt(_V.y));
        public static V2Int ToV2IntCeil(this Vector2 _V) => new V2Int(Mathf.CeilToInt(_V.x), Mathf.CeilToInt(_V.y));
        public static V2Int ToV2IntRound(this Vector2 _V) => new V2Int(Mathf.RoundToInt(_V.x), Mathf.RoundToInt(_V.y));

        public static V2Int ToV2IntFloor(this Vector3 _V) => _V.XY().ToV2IntFloor();
        public static V2Int ToV2IntCeil(this Vector3 _V) => _V.XY().ToV2IntCeil();
        public static V2Int ToV2IntRound(this Vector3 _V) => _V.XY().ToV2IntRound();
    }
}