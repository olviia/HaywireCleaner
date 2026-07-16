using System.Collections.Generic;
using Core.Events;
using UnityEngine;

namespace Features.UI
{
    /// <summary>
    /// put it on the placeholder where you want to show or hide a prefab on certain
    /// eventSO risen. attach the UIElementShowHideRequest that holds those events 
    /// </summary>
    public class UIMountPoint:MonoBehaviour
    {
        [SerializeField] private UIElementDisplayRequestSO request;
        [SerializeField] private Transform container; //where to put the prefab
        
        //to hold a reference to the instance, so we can destroy it later. prefab - instance
        private readonly Dictionary<GameObject, GameObject> active = new();

        void OnEnable()
        {
            request.Show += Mount;
            request.Hide += Unmount;
        }
        
        void OnDisable()
        {
            request.Show -= Mount;
            request.Hide -= Unmount;
        }

        void Mount(GameObject prefab)
        {
            if (active.ContainsKey(prefab)) return; //nothing to unmount
            active[prefab] = Instantiate(prefab, container);
        }

        void Unmount(GameObject key)
        {
            //addressed by prefab
            if (active.Remove(key, out var instance))
            {
                Destroy(instance);
                return;
            }
                
            
            //addressed by instance
            GameObject prefab = null;
            foreach(var pair in active)
                if (pair.Value == key)
                {
                    prefab = pair.Key;
                    break;
                }
            if(prefab != null && active.Remove(prefab, out var otherInstance))
            {
                Destroy(otherInstance);  
            }
        }
    }
}