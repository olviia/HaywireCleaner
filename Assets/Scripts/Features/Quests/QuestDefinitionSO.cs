using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Features.Quests
{
    [Serializable]
    public struct Objective
    {
        public LocalizedString description; //the journal line for this objective
        public FactCondition condition; 
    }

    [Serializable]
    public class Stage
    {
        public LocalizedString  journalEntry; 
        public Objective[] objective; //now all objectives have to be complete
        public GameObject[] setupPrefabs; //what needs to be instantiated for this stage
    }

    [CreateAssetMenu(fileName = "QuestDefinition", menuName = "Cleanbot/Quests/Definition")]
    public class QuestDefinitionSO : ScriptableObject
    {
        public string id;
        public LocalizedString title;

        public Stage[] stages;
        public FactCondition[] startConditions;
    }
}