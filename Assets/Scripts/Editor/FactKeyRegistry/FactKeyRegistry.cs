using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.FactKeyRegistry
{
    /// <summary>
    /// this is from where picker asks the fact, and this gathers all
    /// keys that are from interfaces IFactKeySource
    /// </summary>
    public static class FactKeyRegistry
    {
        public static List<string> Collect()
        {
            var seen = new  HashSet<string>();
            var keys = new List<string>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<IFactKeySource>())
            {
                if (type.IsAbstract || type.IsInterface) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Debug.LogWarning($"[FactKeys] {type.Name} needs a parameterless constructor to be discovered");
                    continue;

                }
                var source = (IFactKeySource)Activator.CreateInstance(type);
                foreach (var key in source.GetFactKeys())
                {
                    if(seen.Add(key))
                        keys.Add(key);
                }
            }
            return keys;
        }
    }
}