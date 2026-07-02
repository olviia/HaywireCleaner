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
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private float castRadius = 0.3f;
        [SerializeField] private LayerMask interactionLayer;
        
        private static Tag BlockedBy => Tag.Interacting | Tag.Charging;
        
        private ActorHost host;
        private IInteractable currentInteractable;
        private Camera cam;

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

        void Awake()
        {
            host = GetComponentInParent<ActorHost>();
            cam = Camera.main;
        }

        void OnEnable() => host.Actor.RegisterModule(this);
        void OnDisable() => host.Actor.RemoveModule(this);

        void Update()
        {
            if (host.Actor.Tags.HasAny(BlockedBy)) return;
            IInteractable candidate = null;

            if (Physics.SphereCast(cam.transform.position,
                    castRadius, cam.transform.forward,
                    out var hit, interactRange, interactionLayer,
                    QueryTriggerInteraction.Collide)
                && hit.collider.TryGetComponent(out IInteractable found)
                && found.CanInteract(host.Actor))
            {
                candidate = found;
            }

            if (candidate == currentInteractable) return;
            if (currentInteractable != null)
            {
                host.Actor.Focus.Clear(currentInteractable);
            }
            currentInteractable = candidate;
            if (currentInteractable != null)
            {
                host.Actor.Focus.Set(currentInteractable);
            }
        }
        
    
    }
}