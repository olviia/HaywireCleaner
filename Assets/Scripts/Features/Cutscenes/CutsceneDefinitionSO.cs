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
        public VoidEventSO trigger;
        //todo: implement not replayable cutscenes with check for their key in 
        //world facts dictionary
        public bool replayable; 
        public bool isTriggerForSomething; //if yes, writes the fact that this cutscene
                                            //was played into facts
    }
}