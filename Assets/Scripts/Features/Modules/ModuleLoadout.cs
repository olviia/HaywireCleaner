using System;
using System.Collections.Generic;
using Core.SaveSystem;
using UnityEngine;

namespace Features.Modules
{
    /// <summary>
    /// instals the module to the player
    /// </summary>
    public class ModuleLoadout:MonoBehaviour
    {
        [SerializeField] private ModuleCatalogSO catalog;
        [SerializeField] private Transform moduleRoot;
        private readonly Dictionary<ModuleDefinitionSO, GameObject> installed = new();

        //raised only for modules gained during play
        public event Action<ModuleDefinitionSO> ModuleInstalled;
        private bool primed;

        private void OnEnable()
        {
            WorldState.FactChanged += OnFactChanged;
            OnFactChanged(null);
        }

        private void OnDisable() => WorldState.FactChanged -= OnFactChanged;

        private void OnFactChanged(string key)
        {
            //if key == null it means save was replaced = load or a new game
            bool restoring = !primed || key == null;

            foreach (var def in catalog.modules)
            {
                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                
                bool owned = WorldState.GetFlag(FactKeys.ModuleOwned(def.id));
                bool present = installed.ContainsKey(def);
                
                if(owned && !present) Install(def, restoring);
                else if (!owned && present) Uninstall(def);
            }
            primed = true;
        }

        private void Install(ModuleDefinitionSO def, bool restoring)
        {
            if (def.prefab == null)
            {
                Debug.LogError($"[Modules] '{def.id}' is owned but has no prefab assigned");
                return;
            }

            installed[def] = Instantiate(def.prefab, moduleRoot, false);
            Debug.Log($"[Modules] installed '{def.id}'{(restoring ? "(restored)" : "")}");

            if(!restoring) ModuleInstalled?.Invoke(def);
        }

        private void Uninstall(ModuleDefinitionSO def)
        {
            if (installed.TryGetValue(def, out var instance) && instance != null)
                Destroy(instance);
            installed.Remove(def);
            Debug.Log($"[Modules] removed '{def.id}'");
        }
    }
}