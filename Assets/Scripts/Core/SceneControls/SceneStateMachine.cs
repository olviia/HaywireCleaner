using System;

namespace Core.SceneControls
{

    public enum GameScene
    {
        Title,
        DockStation,
        Gameplay, 
        Prototype1,
        Prototype2
    }
    /// <summary>
    /// Core seam for app-level screens (Title, Gameplay, ...). Features call
    /// ShowScreen and subscribe to ScreenChanged - they never reference each  
    /// other directly.
    ///
    /// the method of this class is called from featurewhen we want to go from one scene
    /// to another
    ///
    /// this class shows what changes to what, not how.
    /// this is the vocabulary 
    /// </summary>
    public class SceneStateMachine
    {
        //currently active state of game in case anything needs it
        public static GameScene CurrentGameScene { get; private set; }
        
        /// <summary>
        /// raised when the screen is changed
        /// </summary>
        public static event Action<GameScene, GameScene> OnGameSceneChanged;

        
        /// <summary>
        /// Switches the game state to the other state. No two state can exist at the same time
        /// </summary>
        /// <param name="scene">The state the game is switched to</param>
        public static void ChangeSceneTo(GameScene nextScene)
        {
            OnGameSceneChanged?.Invoke(CurrentGameScene, nextScene);
            CurrentGameScene = nextScene;
        }
        
    }
}