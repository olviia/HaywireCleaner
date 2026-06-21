using System;
using System.Collections.Generic;
using Core;
using Core.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace Features.Cutscenes
{
    /// <summary>
    /// This class knows about all the cutscenes and when to play them
    /// </summary>
    public static class CutsceneDirector
    {
        static CutsceneCatalogSO catalog;
        private static Dictionary<string, Func<bool>> conditions;
        //to notify monobehavoiur on the scene
        public static event Action<GameObject> OnPlayRequested;
        
        //standard unity lookup among the resources, loads CutsceneCatalogSO
        [RuntimeInitializeOnLoadMethod]

        static void Init()
        {
            //put cutscenecatalog into folder Asets/Resources/Cutscenes
            catalog = Resources.Load<CutsceneCatalogSO>("CutsceneCatalog");

            SceneLoader.OnSceneLoaded += CheckIntro;
            
            
            //specifically for intro
            static void CheckIntro(GameScene scene)
            {
                if (scene == GameScene.Gameplay ) CheckAllCutscenes();
            }
            
            //one method to check what has to be played
            static void CheckAllCutscenes()
            {
                //here the name of the cutscene HAS to be the same as in the cutsceneDefinition
                //scriptable object ID
                TryPlay("NewGameIntro" ,true);
            }

            static void TryPlay(string cutsceneId, bool conditionMet)
            {
                //check if the condition is correct for the cutscene
                if (!conditionMet) return;
                //check if this cutscene was already played, or if it is replayable, 
                //then it is always false 
                if (WorldState.GetFlag(CutsceneSaveKeys.Played(cutsceneId))) return;
                //get scriptable object from the list with cutscenes
                var def = FindDefinition(cutsceneId);
                Play(def);
            }

            static CutsceneDefinitionSO FindDefinition(string cutsceneId)
            {
                foreach (var def in catalog.all)
                {
                    if(def.id == cutsceneId) return def;
                }

                return null;
            }

            static void Play(CutsceneDefinitionSO def)
            {
                //play def.timeline
                OnPlayRequested?.Invoke(def.cutscenePrefab);
                
                //set the fact that the cutscene was played if it is one time played
                if(!def.replayable) WorldState.SetFlag(CutsceneSaveKeys.Played(def.id), true);
            }

        }
    }
}
