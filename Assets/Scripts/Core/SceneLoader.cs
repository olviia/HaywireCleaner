using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    /// <summary>
    /// this is an interpreter for SceneStateMachine
    /// wired in bootstrap
    /// do not call this class directly
    /// </summary>
    public static class SceneLoader
    {
        private static Dictionary<GameScene, string> sceneMap;
        public static event Action<GameScene> OnSceneLoaded;

        public static void Initialize(Dictionary<GameScene, string> map)
        {
            sceneMap = map;
        }

        public static void LoadScene(GameScene from, GameScene to)
        {
            Scene leaving = SceneManager.GetActiveScene();
            string sceneToLoad = sceneMap[to];
            
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneToLoad, 
                                                                        LoadSceneMode.Additive);
            
            loadOperation.completed += _ =>
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToLoad)); 
                SceneManager.UnloadSceneAsync(leaving);
                OnSceneLoaded?.Invoke(to);
            };
            
        }


    }
}