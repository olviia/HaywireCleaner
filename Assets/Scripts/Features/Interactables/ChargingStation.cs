using System;
using Core.Events;
using Core.Player;
using Core.UI;
using Features.Character;
using UnityEngine;

namespace Features.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class ChargingStation : MonoBehaviour
    {
        [SerializeField] private Transform dockAnchor; //spot to move to
        [SerializeField] private Camera dockCamera; //static camera
        
        [SerializeField] private UIElementDisplayRequestSO requestSO; //for events
        [SerializeField] private GameObject stopChargingButtonPrefab; //to show button
        [SerializeField] private VoidEventSO stopChargingRequested;
        
        [SerializeField] private int livePriority = 10;
        [SerializeField] private int idlePriority = -1;

        private bool charging;
        private Actor actor;

        void OnTriggerEnter(Collider other)
        {
            var host = other.GetComponentInParent<ActorHost>();
            if (host == null) return;
            
            actor =  host.Actor;
            actor.Tags.Added += OnTagAdded;
            actor.Tags.Removed += OnTagRemoved;
            stopChargingRequested.Raised += OnStop;
            
            host.Actor.Dispatch(Intent.Charge, dockAnchor);
        }

        void OnTagAdded(Tag t)
        {
            if (t == Tag.Charging)
            {
                requestSO.RaiseShow(stopChargingButtonPrefab);
                dockCamera.depth = livePriority;
            }
        }

        void OnTagRemoved(Tag t)
        {
            requestSO.RaiseHide(stopChargingButtonPrefab);
            dockCamera.depth = idlePriority;
        }

        void OnStop()
        {
            if(actor == null || !actor.Tags.HasAny(Tag.Charging)) return;
            actor.Dispatch(Intent.StopCharge, null);
        }

        void OnDisable()
        {
            stopChargingRequested.Raised -= OnStop;
            
            if (actor == null) return;
            actor.Tags.Added -= OnTagAdded;
            actor.Tags.Removed -= OnTagRemoved;
        }

        //repeated OnDisable
        void OnTriggerExit(Collider other)
        {
            stopChargingRequested.Raised -= OnStop;
            
            if (actor == null) return;
            actor.Tags.Added -= OnTagAdded;
            actor.Tags.Removed -= OnTagRemoved;
        }
    }
}