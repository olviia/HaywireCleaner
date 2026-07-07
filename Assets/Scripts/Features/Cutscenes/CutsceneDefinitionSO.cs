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
        public bool replayable; //if true, played is never written to save data
        public VoidEventSO trigger;
    }
}