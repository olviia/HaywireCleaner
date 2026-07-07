using UnityEngine;

namespace Core.SceneControls
{

    public enum EntryMode
    {
        None,
        NewGame,
        Continue,
    }
    /// <summary>
    /// Session intent carried from the menu into gameplay
    /// </summary>

    [CreateAssetMenu(fileName = "GameSession", menuName = "Cleanbot/App/GameSession")]
    public class GameSession:ScriptableObject
    {
        [SerializeField] private EntryMode pendingEntry;
        //exposed read-only so the inspector shows it live. writes go through the api
        public EntryMode PendingEntry => pendingEntry;

        private void OnEnable()
        {
            //for the hygiene. as so field values survive between play sessions, so start every session on a clean state
            pendingEntry = EntryMode.None;
        }
        public void Request(EntryMode mode) => pendingEntry = mode;

        public EntryMode Consume()
        {
            EntryMode mode = pendingEntry;
            pendingEntry = EntryMode.None;
            return mode;
        }
    }
}