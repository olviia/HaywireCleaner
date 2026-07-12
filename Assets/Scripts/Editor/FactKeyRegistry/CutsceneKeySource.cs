using System.Collections.Generic;
using Core.SaveSystem;
using Features.Cutscenes;
using UnityEditor;

namespace Tools.FactKeyRegistry
{
    public sealed class CutsceneKeySource:IFactKeySource
    {
        public IEnumerable<string> GetFactKeys()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CutsceneDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<CutsceneDefinitionSO>(path);
                if(def == null || !def.WritesFinishedFact) continue;
                yield return FactKeys.CutsceneFinished(def.id);
            }
        }
    }
}