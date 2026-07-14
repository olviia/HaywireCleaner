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

        private List <UIQuestListEntry> instantiatedPrefabs= new();
        
        
        private string _selectedId;
        

        private void OnEnable()
        {
            QuestInfo.Changed += Rebuild;
        }

        private void OnDisable()
        {
            QuestInfo.Changed -= Rebuild;
        }

        private void Rebuild()
        {
            instantiatedPrefabs.Clear();
            var snapshots = QuestInfo.Snapshots;
            foreach (var snapshot in snapshots)
            {
                if (snapshot == null) return;
                //instantiatedPrefabs = Instantiate(prefab);
                if (snapshot.Status == QuestStatus.Active)
                {
                    
                }
            }
        }
        
    }
}