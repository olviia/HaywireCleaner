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
                Evaluate(quest);
            }
        }

        private void Evaluate(QuestDefinitionSO quest)
        {
            var stageKey = FactKeys.QuestStage(quest.id);
            int stage = WorldState.GetCounter(stageKey);
            
            //0 = not started
            if (stage == 0)
            {
                //this can be rewritten later if not all objectives are met
                if ( quest.startConditions.Length>0 && AllConditionsMet(quest.startConditions))
                {
                    Debug.Log($"[Quest] '{quest.id}' started → stage 1");
                    WorldState.SetCounter(stageKey, 1);
                }

                return;
            }

            if (stage > quest.stages.Length)
            {
                var questCompleted = FactKeys.QuestCompleted(quest.id);
                if (!WorldState.GetFlag(questCompleted))
                {
                    Teardown(quest);
                    WorldState.SetFlag(questCompleted, true);
                    Debug.Log($"[Quest] '{quest.id}' completed");
                }

                return;
            }
            //when stage is between 0 and Length
            ReconcileSetup(quest, stage);
            //increase stage when all objectives are complete
            if (AllObjectivesMet(quest.stages[stage - 1]))
            {
                Debug.Log($"[Quest] '{quest.id}' stage {stage} done → {stage + 1}");
                WorldState.AddToCounter(stageKey, 1);
            }
            
        }

        private static bool AllConditionsMet(FactCondition[] conditions)
        {
            foreach (var c in conditions)
            {
                if (!c.IsMet()) return false;
            }
            return true;
        }

        private static bool AllObjectivesMet(Stage stage)
        {
            foreach (var objective in stage.objective)
                if(!objective.condition.IsMet()) return false;
            return true;
        }
        
        //this actually kinda instantiates the prefab for this quest
        private void ReconcileSetup(QuestDefinitionSO quest, int stage)
        {
            int build = setupStage.TryGetValue(quest, out var s) ? s : 0;
            if (build == stage) return; //prefab is already instantiated
            
            Debug.Log($"[Quest] '{quest.id}' building stage {stage}: " +
                      $"{quest.stages[stage - 1].setupPrefabs.Length} prefab(s)"); 

            Teardown(quest); //remove objects from previous stage
            
            var instances = new List<GameObject>();
            foreach (var prefab in quest.stages[stage -1].setupPrefabs)
                if(prefab != null)
                    instances.Add(Instantiate(prefab));
            setupInstances[quest] = instances;
            setupStage[quest] = stage;
        }

        private void Teardown(QuestDefinitionSO quest)
        {
            if (setupInstances.TryGetValue(quest, out var instances))
            {
                foreach(var go in instances)
                    if (go != null)
                        Destroy(go);
                instances.Clear();
            }
            setupStage.Remove(quest);
        }
    }
}