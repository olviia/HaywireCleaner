using System;
using System.Collections.Generic;

namespace Core.Quests
{
    public interface IQuestUISource
    {
        IReadOnlyList<QuestSnapshot> Snapshots(); //all the quests that are active
        QuestSnapshot Get(string id); //for all texts from quests
        string TrackedId { get; }
        void SetTracked(string id);
        event Action Changed; //repull shapshot or tracked quest
    }
}