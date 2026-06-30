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

        void Unmount(GameObject prefab)
        {
            if(active.Remove(prefab, out var instance)) 
                Destroy(instance);
        }
    }
}