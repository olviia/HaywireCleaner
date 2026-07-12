using System.Collections.Generic;
using Core.SaveSystem;
using Features.Cutscenes;
using Features.Quests;
using UnityEditor;

namespace Tools.FactKeyRegistry
{
    public sealed class QuestKeySource:IFactKeySource
    {
        public IEnumerable<string> GetFactKeys()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:QuestDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<QuestDefinitionSO>(path);

                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                
                yield return FactKeys.QuestCompleted(def.id);
                yield return FactKeys.QuestStage(def.id);
            }
        }
    }
}