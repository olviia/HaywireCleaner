using System;
using Core.Events;
using Core.Interaction;
using Core.Player;
using UnityEngine;
using UnityEngine.Localization;

namespace Features.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class ChargingStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform dockAnchor; //spot to move to
        [SerializeField] private Camera dockCamera; //static camera
        
        [SerializeField] private UIElementDisplayRequestSO requestSO; //for events
        [SerializeField] private GameObject stopChargingButtonPrefab; //to show button
        
        [SerializeField] private UIInteractPromptDisplayRequestSO promptDisplayRequestSo;
        [SerializeField] private LocalizedString promptText;
        
        [SerializeField] private int livePriority = 10;
        [SerializeField] private int idlePriority = -1;
        
        public bool CanInteract(Actor actor) => true;
        

        public void OnFocus()
        {
            promptDisplayRequestSo.RaiseShow(promptText.GetLocalizedString(), Intent.Interact, this.transform);
        }

        public void OnUnfocus()
        {
            promptDisplayRequestSo.RaiseHide();
        }

        public void Interact(Actor actor)
        {
            if (actor.Tags.HasAny(Tag.Charging))//stop charging
            {
                actor.Dispatch(Intent.StopCharge, null);
                requestSO.RaiseHide(stopChargingButtonPrefab);
                dockCamera.depth = idlePriority;
                
                OnFocus(); //show ui prompt again
            }
            else //start charging
            {
                actor.Dispatch(Intent.Charge, dockAnchor);
                requestSO.RaiseShow(stopChargingButtonPrefab);
                dockCamera.depth = livePriority;

                OnUnfocus(); //hide ui prompt
            }
        }

    }
}