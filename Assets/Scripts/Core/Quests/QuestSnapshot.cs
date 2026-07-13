using System.Collections.Generic;

namespace Core.Quests
{
    public enum QuestStatus{Active, Completed}

    public readonly struct ObjectiveLine
    {
        public readonly string Text;
        public readonly bool Completed;

        public ObjectiveLine(string text, bool completed)
        {
            Text = text;
            Completed = completed;
        }
    }
    
    public sealed class QuestSnapshot
    {
        public string Id { get; }
        public string Title { get; }
        public QuestStatus Status { get; }
        
        public IReadOnlyList<ObjectiveLine> Objectives { get; }
        public IReadOnlyList<string> StageStory { get; }

        public QuestSnapshot(string id, string title, QuestStatus status, IReadOnlyList<ObjectiveLine> objectives,
            IReadOnlyList<string> stageStory)
        {
            Id = id;
            Title = title;
            Status = status;
            Objectives = objectives;
            StageStory = stageStory;
        }
    }
}