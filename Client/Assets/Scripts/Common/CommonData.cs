﻿using UnityEngine;

namespace Common
{
    public class CommonData
    {
        public static bool   DevelopmentBuild    = false;
        public static bool   PausedByAdvertising = false;
        public static bool   Release             = false;
        public static bool   Testing             = false;
        public const  string SavedGameFileName   = "main_save";
        
        public static readonly Color CompanyLogoBackgroundColor = new Color(0.37f, 0.07f, 0.25f);
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void ResetState()
        {
            DevelopmentBuild = false;
#if !UNITY_EDITOR && DEVELOPMENT_BUILD
            DevelopmentBuild = true;
#endif
        }
    }
}