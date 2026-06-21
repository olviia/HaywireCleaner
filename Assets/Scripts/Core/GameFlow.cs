using System;
using Core.SaveSystem;

namespace Core
{
    /// <summary>
    /// start new game or load old game
    /// </summary>
    public static class GameFlow
    {
        public static event Action OnNewGameRequested;
        public static event Action OnLoadGameRequested;

        //for the new game button honestly
        public static void StartNewGame()
        {
            WorldState.NewSave();
            OnNewGameRequested?.Invoke();
            SceneStateMachine.ChangeSceneTo(GameScene.Gameplay);
        }
        
        //maybe load game will carry something different
        public static void LoadGame()
        {
            OnLoadGameRequested?.Invoke();
            SceneStateMachine.ChangeSceneTo(GameScene.Gameplay);
        }
    }
}