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

        public static void Initialize(Dictionary<GameScene, string> map)
        {
            sceneMap = map;
        }

        public static void LoadScene(GameScene from, GameScene to)
        {
            string sceneToLoad = sceneMap[to];
            string sceneToUnload = sceneMap[from];
            
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneToLoad, 
                                                                        LoadSceneMode.Additive);
            
            loadOperation.completed += _ => SceneManager.UnloadSceneAsync(sceneToUnload);
        }


    }
}