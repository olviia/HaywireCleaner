using Core.Events;
using UnityEngine;
using UnityEngine.Timeline;

namespace Features.Cutscenes
{
    [CreateAssetMenu(fileName = "CutsceneDefinition", menuName = "Cleanbot/Cutscenes/Definition")]
    public class CutsceneDefinitionSO : ScriptableObject
    {
        public string id;
        public GameObject cutscenePrefab;
        public VoidEventSO eventTrigger;

        public VoidEventSO eventRaiseOnFinish;
        
        public bool replayable; 
        public bool isTriggerForQuest; //if yes, writes the fact that this cutscene
                                            //was played into facts
                                            
        //derived definition
        public bool WritesFinishedFact => !replayable || isTriggerForQuest;
    }
}