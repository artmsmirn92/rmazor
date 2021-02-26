﻿using System;
using System.Collections.Generic;
using System.Linq;
using Constants;
using Entities;
using GameHelpers;
using Managers;
using Network;
using Network.Packets;
using PygmyMonkey.ColorPalette;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using Unity.Android.Logcat;
using Utils;
using Utils.Editor;

public class EditorHelper : EditorWindow
{
    private int m_DailyBonusIndex;
    private Dictionary<BankItemType, long> m_Money = new Dictionary<BankItemType, long>();
    private int m_TestUsersCount = 3;
    private bool m_IsGuest;
    private string m_TestUrl;
    private string m_TestUrlCheck;
    private int m_GameId = -1;
    private int m_GameIdCheck;
    private int m_Level = -1;
    private int m_Quality = -1;
    private int m_QualityCheck;

    [MenuItem("Tools/Helper", false, 0)]
    public static void ShowWindow()
    {
        GetWindow<EditorHelper>("Helper");
    }
    
    [MenuItem("Tools/Profiler",false, 3)]
    public static void ShowProfilerWindow()
    {
        Type tProfiler = typeof(Editor).Assembly.GetType("UnityEditor.ProfilerWindow");
        GetWindow(tProfiler, false);
    }
    
#if UNITY_ANDROID
    [MenuItem("Tools/Android Logcat",false, 4)]
    public static void ShowAndroidLogcatWindow()
    {
        Type tLogcat = typeof(ColumnData).Assembly.GetType("Unity.Android.Logcat.AndroidLogcatConsoleWindow");
        MethodInfo mInfoShow = tLogcat?.GetMethod("ShowWindow",
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        mInfoShow?.Invoke(null, null);
    }
#endif
    
    [MenuItem("Tools/Color Palette", false, 5)]
    private static void ShowColorPaletteWindow()
    {
        EditorWindow window = ColorPaletteWindow.createWindow<ColorPaletteWindow>("Color Palette");
        window.minSize = new Vector2(280, 500);
    }

    private void OnEnable()
    {
        UpdateTestUrl();
        UpdateGameId();
        UpdateQuality();
    }

    private void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUILayout.Label($"Account Id: {GameClientUtils.AccountId}");
            GUILayout.Label($"Device Id: {GameClientUtils.DeviceId}");
            GUILayout.Label($"Target FPS: {Application.targetFrameRate}");
        }
        
        GUI.enabled = Application.isPlaying;
        if (!GUI.enabled)
            GUILayout.Label("Available only in play mode:");
        if (GUI.enabled)
            GUILayout.Space(10);
        
        GUILayout.BeginHorizontal();
        EditorUtilsEx.GuiButtonAction("Enable Daily Bonus", EnableDailyBonus);
        GUILayout.Label("Day:");
        m_DailyBonusIndex = EditorGUILayout.Popup(
            m_DailyBonusIndex, new[] { "1", "2", "3", "4", "5", "6", "7" });
        GUILayout.EndHorizontal();
        
        if (Application.isPlaying)
        {
            GUILayout.BeginHorizontal();
            var money = m_Money.CloneAlt();
            foreach (var kvp in m_Money)
            {
                GUILayout.Label($"{kvp.Key}:");
                money[kvp.Key] = EditorGUILayout.LongField(money[kvp.Key]);
            }
            m_Money = money;
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            EditorUtilsEx.GuiButtonAction("Get From Bank", GetMoneyFromBank);
            EditorUtilsEx.GuiButtonAction("Set Money", SetMoney);
            GUILayout.EndHorizontal();
        }
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Game Id:"))
            SaveUtils.PutValue(SaveKey.GameId, m_GameId);
        m_GameId = EditorGUILayout.IntField(m_GameId);
        EditorUtilsEx.GuiButtonAction("Start Level:", LevelLoader.LoadLevel, m_Level);
        m_Level = EditorGUILayout.IntField(m_Level);
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        EditorUtilsEx.GuiButtonAction(PauseGame, true);
        EditorUtilsEx.GuiButtonAction("Continue game", PauseGame, false);
        GUILayout.EndHorizontal();
        
        EditorUtilsEx.DrawUiLine(Color.gray);
        GUI.enabled = true;

        EditorUtilsEx.GuiButtonAction(PrintCommonInfo);

        GUILayout.BeginHorizontal();
        EditorUtilsEx.GuiButtonAction(CreateTestUsers, m_TestUsersCount);
        GUILayout.Label("count:", GUILayout.Width(40));
        m_TestUsersCount = EditorGUILayout.IntField(m_TestUsersCount);
        GUILayout.Label("guest:", GUILayout.Width(40));
        m_IsGuest = EditorGUILayout.Toggle(m_IsGuest, GUILayout.Width(30));
        GUILayout.EndHorizontal();
        
        EditorUtilsEx.GuiButtonAction("Delete test users", DeleteTestUsers);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Debug Server Url:");
        m_TestUrl = EditorGUILayout.TextField(m_TestUrl);
        GUILayout.EndHorizontal();

        EditorUtilsEx.GuiButtonAction("Set default api url", SetDefaultApiUrl);
        EditorUtilsEx.GuiButtonAction("Delete all settings", DeleteAllSettings);
        EditorUtilsEx.GuiButtonAction("Get ready to commit", GetReadyToCommit);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Quality:");
        m_Quality = EditorGUILayout.Popup(
            m_Quality, new[] { "Normal", "Good" });
        GUILayout.EndHorizontal();
        
        EditorUtilsEx.DrawUiLine(Color.gray);

        GUILayout.BeginHorizontal();
        EditorUtilsEx.GuiButtonAction(SceneNames.Preload, LoadScene, $"Assets/Scenes/{SceneNames.Preload}.unity");
        EditorUtilsEx.GuiButtonAction(SceneNames.Main, LoadScene, $"Assets/Scenes/{SceneNames.Main}.unity");
        EditorUtilsEx.GuiButtonAction(SceneNames.Level, LoadScene, $"Assets/Scenes/{SceneNames.Level}.unity");
        GUILayout.EndHorizontal();
        
        EditorUtilsEx.DrawUiLine(Color.gray);
        
        GUILayout.Label("Cached data:");
        foreach (var skVal in GetAllSaveKeyValues())
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(skVal.Key, GUILayout.Width(200));
            Color defCol = GUI.contentColor;
            if (skVal.Value == "empty")
                GUI.contentColor = Color.yellow;
            if (skVal.Value == "not exist")
                GUI.contentColor = Color.red;
            GUILayout.Label(skVal.Value);
            GUI.contentColor = defCol;
            GUILayout.EndHorizontal();
        }

        UpdateTestUrl();
        UpdateGameId();
        UpdateQuality();
    }

    private void UpdateTestUrl(bool _Forced = false)
    {
        if (string.IsNullOrEmpty(m_TestUrl))
            m_TestUrl = SaveUtils.GetValue<string>(SaveKeyDebug.ServerUrl);
        if (m_TestUrl != m_TestUrlCheck || _Forced)
            SaveUtils.PutValue(SaveKeyDebug.ServerUrl, m_TestUrl);
        m_TestUrlCheck = m_TestUrl;
    }

    private void UpdateGameId()
    {
        if (m_GameId == -1)
            m_GameId = SaveUtils.GetValue<int>(SaveKey.GameId);
        if (m_GameId != m_GameIdCheck)
            SaveUtils.PutValue(SaveKey.GameId, m_GameId);
        m_GameIdCheck = m_GameId;
    }

    private void UpdateQuality()
    {
        if (m_Quality == -1)
            m_Quality = SaveUtils.GetValue<bool>(SaveKeyDebug.GoodQuality) ? 1 : 0;
        if (m_Quality != m_QualityCheck)
            SaveUtils.PutValue(SaveKeyDebug.GoodQuality, m_Quality != 0);
        m_QualityCheck = m_Quality;
    }

    private void EnableDailyBonus()
    {
        SaveUtils.PutValue(SaveKey.DailyBonusLastDate, DateTime.Now.Date.AddDays(-1));
        SaveUtils.PutValue(SaveKey.DailyBonusLastItemClickedDay, m_DailyBonusIndex);
    }

    private void GetMoneyFromBank()
    {
        var bank = BankManager.Instance.GetBank();
        Coroutines.Run(Coroutines.WaitWhile(
            () => !bank.Loaded,
            () =>  m_Money = bank.BankItems));
    }
    
    private void SetMoney()
    {
        BankManager.Instance.SetBank(m_Money);
    }

    private static void PrintCommonInfo()
    {
        Debug.Log($"Account Id: {GameClientUtils.AccountId}");
        Debug.Log($"Device Id: {GameClientUtils.DeviceId}");
    }

    private void CreateTestUsers(int _Count)
    {
        GameClient.Instance.Init(true);
        int gameId = 1; 
        var randGen = new System.Random();
        for (int i = 0; i < _Count; i++)
        {
            var packet = new RegisterUserPacket(
                new RegisterUserPacketRequestArgs
                {
                    Name = m_IsGuest ? string.Empty : $"test_{CommonUtils.GetUniqueId()}",
                    PasswordHash = CommonUtils.GetMd5Hash("1"),
                    DeviceId = m_IsGuest ? $"test_{CommonUtils.GetUniqueId()}" : string.Empty,
                    GameId = gameId
                });
            int ii = i;
            packet.OnSuccess(() =>
                {
                    var adf = new AccountDataFieldFilter(packet.Response.Id, 
                        DataFieldIds.FirstCurrency, DataFieldIds.SecondCurrency);
                    adf.Filter(_DfValues =>
                    {
                        _DfValues.First(_DfValue => _DfValue.FieldId == DataFieldIds.FirstCurrency)
                            .SetValue(randGen.Next(0, 10000)).Save();
                        _DfValues.First(_DfValue => _DfValue.FieldId == DataFieldIds.SecondCurrency)
                            .SetValue(randGen.Next(0, 100)).Save();
                    });
                    Debug.Log("All test users were created successfully");
                })
                .OnFail(() =>
                {
                    Debug.LogError($"Creating test user #{ii + 1} of {_Count} failed");
                    Debug.LogError(packet.Response);
                });
            GameClient.Instance.Send(packet);
        }
    }

    private static void DeleteTestUsers()
    {
        GameClient.Instance.Init(true);
        IPacket packet = new DeleteTestUsersPacket();
        packet.OnSuccess(() =>
            {
                Debug.Log("All test users deleted");
            })
            .OnFail(() => Debug.Log($"Failed to delete test users: {packet.ErrorMessage}"));
        GameClient.Instance.Send(packet);
    }

    private static void GetReadyToCommit()
    {
        string[] files =
        {
            @"Assets\Materials\CircleTransparentTransition.mat",
            @"Assets\Materials\MainMenuBackground.mat"
        };
        foreach (var file in files)
        {
            GitUtils.RunGitCommand($"reset -- {file}");
            GitUtils.RunGitCommand($"checkout -- {file}");
        }
        AssetDatabase.SaveAssets();
        
        BuildSettingsUtils.AddDefaultScenesToBuild();
    }

    private static void LoadScene(string _Name)
    {
        if (Application.isPlaying)
            SceneManager.LoadScene(_Name);
        else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(_Name);
    }
    
    private static void PauseGame(bool _Pause)
    {
        UiTimeProvider.Instance.Pause = _Pause;
        GameTimeProvider.Instance.Pause = _Pause;
    }

    private void SetDefaultApiUrl()
    {
        m_TestUrl = @"http://77.37.152.15:7000";
        UpdateTestUrl(true);
    }

    private void DeleteAllSettings()
    {
        PlayerPrefs.DeleteAll();
        UpdateTestUrl(true);
    }

    private static Dictionary<string, string> GetAllSaveKeyValues() =>
        new Dictionary<string, string>
        {
            {"Last connection succeeded", SaveUtils.GetValue<bool>(SaveKey.LastConnectionSucceeded).ToString()},
            {"Login", SaveUtils.GetValue<string>(SaveKey.Login) ?? "not exist"},
            {"Password hash", SaveUtils.GetValue<string>(SaveKey.PasswordHash) ?? "not exist"},
            {"Account id", SaveUtils.GetValue<int?>(SaveKey.AccountId).ToString()},
            {"Game id", SaveUtils.GetValue<int?>(SaveKey.GameId).ToString()},
            {"Show ads", GetAccountFieldCached(DataFieldIds.ShowAds)},
            {"Gold", GetAccountFieldCached(DataFieldIds.FirstCurrency)},
            {"Diamonds", GetAccountFieldCached(DataFieldIds.SecondCurrency)},
            {"Main score", GetGameFieldCached(DataFieldIds.InfiniteLevelScore)}
        };
    
    private static string GetAccountFieldCached(ushort _FieldId)
    {
        int? accountId = SaveUtils.GetValue<int?>(SaveKey.AccountId);
        var field = SaveUtils.GetValue<AccountDataField>(
            SaveKey.AccountDataFieldValue(accountId ?? GameClientUtils.DefaultAccountId, _FieldId));
        return DataFieldValueString(field);
    }
    
    private static string GetGameFieldCached(ushort _FieldId)
    {
        int? accountId = SaveUtils.GetValue<int?>(SaveKey.AccountId);
        int gameId = SaveUtils.GetValue<int>(SaveKey.GameId);
        var field = SaveUtils.GetValue<GameDataField>(
            SaveKey.GameDataFieldValue(accountId ?? GameClientUtils.DefaultAccountId, gameId, _FieldId));
        return DataFieldValueString(field);
    }

    private static string DataFieldValueString(DataFieldBase _DataField)
    {
        if (_DataField == null)
            return "not exist";
        string value = _DataField.ToString();
        if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
            return "empty";
        return value;
    }
}