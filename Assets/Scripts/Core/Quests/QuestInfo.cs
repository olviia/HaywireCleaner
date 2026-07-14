using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Core.Quests
{
    
    /// <summary>
    /// this place is where UI reaches for quest data
    /// </summary>
    public static class QuestInfo
    {
        private static IQuestInfoSource _source;
        
        public static event Action Changed;
        public static bool IsReady => _source != null;

        public static void Register(IQuestInfoSource source)
        {
            if (_source == source) return;
            if (IsReady) _source.Changed -= Raise; //detach from old 
            _source = source;
            if (IsReady) _source.Changed += Raise;
            Raise();
        }

        public static void Unregister(IQuestInfoSource source)
        {
            if (_source != source) return;
            _source.Changed -= Raise;
            _source = null;
            Raise(); //to blank UI
        }
        public static IReadOnlyList<QuestSnapshot>Snapshots => _source?.Snapshots() ?? Array.Empty<QuestSnapshot>();
        public static QuestSnapshot Get(string id) => _source?.Get(id);
        public static string TrackedId => _source?.TrackedId;
        public static void SetTracked(string id) => _source?.SetTracked(id);
        
        private static void Raise() => Changed?.Invoke();
    }
}