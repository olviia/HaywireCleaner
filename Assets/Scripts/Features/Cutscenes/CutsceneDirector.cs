using System;
using System.Collections.Generic;
using Core;
using Core.Events;
using Core.Input;
using Core.SaveSystem;
using Core.SceneControls;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace Features.Cutscenes
{
    /// <summary>
    /// This class knows about all the cutscenes and when to play them
    /// </summary>
    public class CutsceneDirector:MonoBehaviour
    {
        [SerializeField]private CutsceneCatalogSO catalog;

        private int activeCutscenes;
        
        //list for events from cutscene definitions
        private readonly List<(VoidEventSO eventSo, Action action)> bindings = new();

        private void OnEnable()
        {
            foreach (var cutscene in catalog.cutscenes)
            {
                if(cutscene.trigger == null ) continue;
                Action action = () => Play(cutscene);
                cutscene.trigger.Raised += action;
                bindings.Add((cutscene.trigger, action));
            }
        }

        private void OnDisable()
        {
            foreach (var (eventSo, action) in bindings)
            {
                eventSo.Raised -= action;
            }
            bindings.Clear();
        }

        private void Play(CutsceneDefinitionSO def)
        {
            if (activeCutscenes++ == 0) // signal to core to change input to cutscene. 
            {
                InputRouter.Enter(InputContext.Cutscene);
            }
            
            var instance = Instantiate(def.cutscenePrefab);
            var playable = instance.GetComponent<PlayableDirector>();
            playable.stopped += _ =>
            {
                Destroy(instance);
                if(--activeCutscenes == 0) 
                    InputRouter.Exit(InputContext.Cutscene);
            };
            playable.Play();
        }
    }
}
