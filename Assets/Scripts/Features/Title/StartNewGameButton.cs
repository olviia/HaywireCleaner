using Core;
using Core.SceneControls;
using UnityEngine;

namespace Features.Title
{
    public class StartNewGameButton:MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        public void StartNewGame() => GameFlow.Begin(gameSession, EntryMode.NewGame);

    }
}