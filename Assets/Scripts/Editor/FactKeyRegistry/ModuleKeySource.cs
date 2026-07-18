using System.Collections.Generic;
using Core.SaveSystem;
using Features.Modules;
using Mono.Cecil;
using UnityEditor;

namespace Tools.FactKeyRegistry
{
    public class ModuleKeySource:IFactKeySource
    {
        public IEnumerable<string> GetFactKeys()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ModuleDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<ModuleDefinitionSO>(path);
                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                yield return FactKeys.ModuleOwned(def.id);
            }
        }
    }
}