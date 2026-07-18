using Core.SaveSystem;
using Core.SceneControls;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Title
{
    public class LoadGameButton:MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private Button loadButton;

        private void OnEnable()
        {
            if (loadButton != null)
                loadButton.interactable = WorldState.SaveExists;
        }
        
        public void LoadGame() => GameFlow.Begin(gameSession, EntryMode.Continue);
    }
}