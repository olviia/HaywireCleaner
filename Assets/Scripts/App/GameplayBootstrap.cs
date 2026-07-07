using Core.Events;
using Core.SceneControls;
using UnityEngine;

namespace Bootstrap
{
    /// <summary>
    /// here wire things in the gameplay scene
    /// </summary>
    public class GameplayBootstrap:MonoBehaviour
    {
        [Header("Session")] [SerializeField] private GameSession session;
        [Header("Events")] [SerializeField] private VoidEventSO newGameStarted;

        private void Start()
        {
            switch (session.Consume())
            {
                case EntryMode.NewGame:
                    InitializeNewGame();
                    newGameStarted.RaiseAction();
                    break;
                case EntryMode.Continue:
                    LoadSavedGame();
                    break;
                case EntryMode.None:
                    //do nothing for now
                    break;
            }
        }

        private void InitializeNewGame()
        {
            //code for the new game initialization
        }

        private void LoadSavedGame()
        {
            //code for loading existing game
        }
    }
}