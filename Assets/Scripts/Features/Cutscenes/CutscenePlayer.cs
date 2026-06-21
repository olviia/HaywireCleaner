using Core;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Features.Cutscenes
{
    /// <summary>
    /// put this script on the scene and put PlayableDirector on the same gameobject
    /// this class will play all the cutscenes when requested
    /// </summary>
    public class CutscenePlayer:MonoBehaviour
    {

        void OnEnable() => CutsceneDirector.OnPlayRequested += Play;
        void OnDisable() => CutsceneDirector.OnPlayRequested -= Play;

        void Play(GameObject cutscenePrefab)
        {
            var instance = Instantiate(cutscenePrefab);
            var director = instance.GetComponent<PlayableDirector>();
            director.stopped += _ =>
            {
                Destroy(instance);
                
                //TODO:
                //remove this line when discarding prototypes
                SceneStateMachine.ChangeSceneTo(GameScene.Prototype1);
            };
            director.Play();
        }
    }
}