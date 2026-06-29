using System.Collections.Generic;
using Core.Interaction;
using Core.Player;
using Features.Character;
using UnityEngine;

namespace Features.Modules
{
    public class InteractionModule:MonoBehaviour, IModule
    {
        private ActorHost host;
        private IInteractable currentInteractable;

        private static readonly Intent[] reactsTo = { Intent.Interact };
        public IEnumerable<Intent> ReactsTo => reactsTo;
        public void Handle(Actor owner, Command cmd)
        {
            if (currentInteractable != null && currentInteractable.CanInteract(owner))
            {
                currentInteractable.Interact(owner);
            }
        }

        void Awake() => host = GetComponentInParent<ActorHost>();
        void OnEnable() => host.Actor.RegisterModule(this);
        void OnDisable() => host.Actor.RemoveModule(this);
        
        //call when we can interact with something, for example trigger collider
        public void SetFocus(IInteractable nextInteractable)
        {
            if (currentInteractable == nextInteractable) return;
            currentInteractable?.OnUnfocus();
            currentInteractable = nextInteractable;
            currentInteractable?.OnFocus();
        } 
        
        //leaving interacting zone 
        public void ClearFocus(IInteractable leavingInteractable)
        {
            if(currentInteractable != leavingInteractable) return;
            currentInteractable?.OnUnfocus();
            currentInteractable = null;
        }
        
        
    
    }
}