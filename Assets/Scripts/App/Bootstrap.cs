using System.Collections.Generic;
using UnityEngine;
using Core;
using Core.SceneControls;

namespace Bootstrap
{
    /// <summary>
    /// This is the class to rule them all. It knows both about Core and Features.
    /// It defines what loads and in what order
    /// This is a named exception in Mark Seemann's Depencency Injection Principles,
    /// Patterns and Practices
    /// this is something they call a composition root
    ///
    /// In the build, Title scene should sit on the 0 index, this class will allow it to proceed
    ///
    /// If the class becomes too big, split by seam:
    /// App/
    /// Bootstrap.cs          — the [RuntimeInitializeOnLoadMethod], calls  
    /// each below in order
    /// Bootstrap.Scenes.cs    — ScreenId↔scene map, SceneFlowLoader wiring 
    /// Bootstrap.Modules.cs   — ModuleSystem wiring, default-owned modules 
    /// Bootstrap.Save.cs      — GameState load-on-boot wiring

    /// </summary>
    public static class Bootstrap
    {

        /// <summary>
        /// Runs before any scene is loaded
        /// </summary>
        /// <returns></returns>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneLoader.Initialize(new Dictionary<GameScene, string>
                {
                    { GameScene.Title , "Title" },
                    { GameScene.Gameplay , "Gameplay" }
                });
            SceneStateMachine.OnGameSceneChanged += SceneLoader.LoadScene;
        }
    }
}