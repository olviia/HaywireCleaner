using System;
using System.Collections;
using Core.Events;
using Core.Interaction;
using Core.Player;
using UnityEngine;
using UnityEngine.Localization;

namespace Features.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class ChargingStation : MonoBehaviour, IInteractable, IDock
    {
        [SerializeField] private Transform dockAnchor; //spot to move to
        [SerializeField] private Camera dockCamera; //static camera
        
        [SerializeField] private UIPromptDisplayRequestSO interactDisplay; //for events
        [SerializeField] private UIPromptPositionRequestSO interactPosition; //for events
        
        [SerializeField] private UIPromptDisplayRequestSO stopChargingDisplay;
        [SerializeField] private LocalizedString startCharging;
        [SerializeField] private LocalizedString stopCharging;
        
        [SerializeField] private int livePriority = 10;
        [SerializeField] private int idlePriority = -1;
        
        [SerializeField] private float moveToDockDuration = 0.6f;
        
        
        private Coroutine dockingCoroutine;

        public bool CanInteract(Actor actor) => actor.GetModule<IChargeable>() != null;

        public void OnFocus(Vector3 hitPoint)
        {
            interactDisplay.RaiseShow(startCharging.GetLocalizedString(), Intent.Interact);
            interactPosition.RaiseSetPosition(hitPoint);
        }

        public void OnUnfocus()
        {
            interactDisplay.RaiseHide();
        }

        public void Interact(Actor actor)
        {
            actor.GetModule<IChargeable>()?.StartDocking(this);
            actor.Tags.Add(Tag.Interacting);
        }

        public void Dock(Rigidbody body)
        {
            stopChargingDisplay.RaiseShow(stopCharging.GetLocalizedString(), Intent.Interact);
            dockCamera.depth = livePriority;
            
            body.isKinematic = true;
            if(dockingCoroutine != null)StopCoroutine(dockingCoroutine); // some stopper for failstate
            dockingCoroutine = StartCoroutine(StartDocking(body));
        }

        public void UnDock(Rigidbody body)
        {
            stopChargingDisplay.RaiseHide();
            dockCamera.depth = idlePriority;
            
            if (dockingCoroutine != null)
            {
                StopCoroutine(dockingCoroutine);
                dockingCoroutine = null;
            }
            
            body.isKinematic = false;
        }

        public event Action Docked;

        private IEnumerator StartDocking(Rigidbody rb)
        {
            Vector3 startPos = rb.position;
            Quaternion startRot = rb.rotation;
            float elapsed = 0f;

            while (elapsed < moveToDockDuration)
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.SmoothStep(0.0f, 1.0f, elapsed / moveToDockDuration);
                
                rb.MovePosition(Vector3.Lerp(startPos, dockAnchor.position, t));
                rb.MoveRotation(Quaternion.Slerp(startRot, dockAnchor.rotation, t));
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(dockAnchor.position); //exact snap
            rb.MoveRotation(dockAnchor.rotation);
            dockingCoroutine = null;
            
            Docked?.Invoke();
        }

    }
}