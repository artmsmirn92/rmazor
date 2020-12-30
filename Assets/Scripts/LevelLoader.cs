﻿using Constants;
using Exceptions;
using Games.LinesDefender;
using Games.PointsTapper;
using Network;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelLoader
{
    private const string WasNotMadeMessage = "Game was not made";
    private static int _level;

    static LevelLoader()
    {
        SceneManager.sceneLoaded += OnLoadLevel;
    }

    private static void OnLoadLevel(Scene _Scene, LoadSceneMode _LoadSceneMode)
    {
        if (_Scene.name != SceneNames.Level)
            return;
        LoadGame(GameClient.Instance.GameId);
    }

    public static void LoadLevel(int _Level)
    {
        _level = _Level;
        SceneManager.LoadScene(SceneNames.Level);
    }
    
    private static void LoadGame(int _GameId)
    {
        Debug.Log(_GameId);
        switch (_GameId)
        {
            case 1:
                PointsTapperManager.Instance.Init(_level);
                break;
            case 2:
                //LinesDefenderManager.Instance.Init(_level);
                break;
            case 3:
                Debug.Log(WasNotMadeMessage);
                break;
            case 4:
                Debug.Log("PathFinder WIP");
                PathFinder.PathFinderManager.Instance.Init(_level);
                break;
            case 5:
                Debug.Log(WasNotMadeMessage);
                break;
            default:
                throw new SwitchCaseNotImplementedException(_GameId);
        }
    }
}
