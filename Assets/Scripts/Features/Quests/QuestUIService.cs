using System;
using System.Collections.Generic;
using Core.Quests;
using Core.SaveSystem;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Features.Quests
{
    /// <summary>
    /// this class gives the snapshot of quests to ui through core, through IQuestUISource
    /// </summary>
    public class QuestUIService: MonoBehaviour, IQuestUISource
    {
        [SerializeField] private QuestCatalogSO catalog;
        
        private string _tracked;

        public string TrackedId
        {
            get
            {
                if(!string.IsNullOrEmpty(_tracked) && IsActive(_tracked)) return _tracked;
                foreach (var quest in catalog.quests)
                    if(IsActive(quest.id)) return quest.id;
                return null;
            }
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            WorldState.FactChanged += OnFactChaged;
            QuestUI.Register(this);
        }

        private void OnDisable()
        {
            QuestUI.Unregister(this);
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            WorldState.FactChanged -= OnFactChaged;
        }

        public IReadOnlyList<QuestSnapshot> Snapshots()
        {
            var list = new List<QuestSnapshot>();
            foreach (var quest in catalog.quests)
            {
                var snap = Build(quest);
                if(snap != null) list.Add(snap);
            }

            return list;
        }

        public QuestSnapshot Get(string id)
        {
            foreach (var quest in catalog.quests)
                if(quest.id ==id)
                    return Build(quest);
            return null;
        }

        
        public void SetTracked(string id)
        {
            _tracked = id;
            Changed?.Invoke();
        }

        public event Action Changed;

        private QuestSnapshot Build(QuestDefinitionSO quest)
        {
            int stage = WorldState.GetCounter(FactKeys.QuestStage(quest.id));
            if(stage == 0) return null;
            
            int length = quest.stages.Length;
            int reached = Mathf.Min(stage,length);
            bool completed = stage > length;
            
            var story = new string[reached];
            for(int i = 0; i < reached; i++)
                story[i] = GetLocalizedString(quest.stages[i].journalEntry);

            ObjectiveLine[] objectives;
            if (completed)
            {
                objectives = Array.Empty<ObjectiveLine>();
            }
            else
            {
                var current = quest.stages[stage - 1].objective;
                objectives = new ObjectiveLine[current.Length];
                for (int i = 0; i < current.Length; i++)
                    objectives[i] = new ObjectiveLine(
                        GetLocalizedString(current[i].description),
                        current[i].condition.IsMet());
            }

            return new QuestSnapshot(
                quest.id,
                GetLocalizedString(quest.title),
                completed ? QuestStatus.Completed : QuestStatus.Active,
                objectives,
                story);
        }
        
        //get the localised string
        private string GetLocalizedString(LocalizedString text) => text.GetLocalizedString();

        private bool IsActive(string id)
        {
            foreach(var quest in catalog.quests)
                if (quest.id == id)
                {
                    int stage = WorldState.GetCounter(FactKeys.QuestStage(quest.id));
                    return stage >= 1 && stage <= quest.stages.Length;
                }
            return false;
        }
        
        private void OnFactChaged(string key) => Changed?.Invoke();
        private void OnLocaleChanged(Locale locale) => Changed?.Invoke();
    }
}