using System;
using Core.SaveSystem;
using UnityEngine;

namespace Core.SceneControls
{
    /// <summary>
    /// moves player from the menu into the gameplay, prepares world state first
    /// then reconstructs the intent, then transitions
    /// </summary>
    public static class GameFlow
    {
        public static void Begin(GameSession session, EntryMode mode)
        {
            if (mode == EntryMode.Continue && !WorldState.Load())
            {
                Debug.LogWarning("[GameFlow] no readable save, starting a newgame instead");
                mode = EntryMode.NewGame;

            }
            if(mode == EntryMode.NewGame)
                WorldState.NewSave();
            session.Request(mode);
            SceneStateMachine.ChangeSceneTo(GameScene.Gameplay);
        }
    }
}