using System;
using System.Collections.Generic;
using Core.Quests;
using UnityEngine;
using UnityEngine.UI;

namespace Features.UI
{
    public class UIQuestTab:MonoBehaviour
    {
        [SerializeField] private UIQuestListEntry prefab;
        [SerializeField] private VerticalLayoutGroup activeGroup;
        [SerializeField] private VerticalLayoutGroup completedGroup;
        [SerializeField] private UIQuestDetailPanel detailPanel;

        private string currentId;
        private List <UIQuestListEntry> instantiatedPrefabs= new();

        private void OnEnable()
        {
            QuestInfo.Changed += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            QuestInfo.Changed -= Rebuild;
        }

        private void Rebuild()
        {
            Clear();
            foreach (var snapshot in QuestInfo.Snapshots)
            {
                var group = snapshot.Status == QuestStatus.Completed ? completedGroup : activeGroup;
                var instance = Instantiate(prefab, group.transform);
                instance.Bind(snapshot);
                instantiatedPrefabs.Add(instance);
                instance.Clicked += id => Select(id);
            }
            Select(currentId);
        }

        private void Clear()
        {
            foreach (var prefab in instantiatedPrefabs)
            {
                if (prefab != null)
                {
                    Destroy(prefab.gameObject);
                }
            }
            instantiatedPrefabs.Clear();
        }

        private void Select(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            currentId = id;
            detailPanel.Show(QuestInfo.Get(id));
        }
        

    }
}