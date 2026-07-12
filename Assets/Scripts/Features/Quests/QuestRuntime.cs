using System.Collections.Generic;
using System.Data;
using Core.SaveSystem;
using UnityEngine;

namespace Features.Quests
{
    public class QuestRuntime:MonoBehaviour
    {
        [SerializeField] private QuestCatalogSO catalog;

        private readonly Dictionary<QuestDefinitionSO, int> setupStage = new();
        
        //for prefabs that have to be instantiated for this quest/stage
        private readonly Dictionary<QuestDefinitionSO, List<GameObject>> setupInstances = new();

        void OnEnable()
        {
            WorldState.FactChanged += OnFactChanged;
            OnFactChanged(null);
        }

        void OnDisable() => WorldState.FactChanged -= OnFactChanged;

        private void OnFactChanged(string key)
        {
            foreach (var quest in catalog.quests)
            {
                //do something with quests
            }
        }

    }
}