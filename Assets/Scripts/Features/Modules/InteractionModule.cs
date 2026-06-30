using System.Collections.Generic;
using Core.Interaction;
using Core.Player;
using UnityEngine;

namespace Features.Modules
{
    /// <summary>
    /// put it under the actor, and add InteractionSensor to it.
    /// maybe in the future is shoudl be refactored so that interaction sensor
    /// sits in this module
    /// </summary>
    public class InteractionModule:MonoBehaviour, IModule
    {
        private ActorHost host;
        private IInteractable currentInteractable;

        private static readonly Intent[] reactsTo = { Intent.Interact };
        public IEnumerable<Intent> ReactsTo => reactsTo;
        public void Handle(Actor owner, Command cmd)
        {
            var target = owner.Focus.Current;
            if (target != null && target.CanInteract(owner))
            {
                target.Interact(owner); 
            }
        }

        void Awake() => host = GetComponentInParent<ActorHost>();
        void OnEnable() => host.Actor.RegisterModule(this);
        void OnDisable() => host.Actor.RemoveModule(this);
        
    
    }
}