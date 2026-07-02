using System.Collections;
using System.Collections.Generic;
using Core.Interaction;
using Core.Player;
using UnityEngine;

namespace Features.Modules
{
    public class ChargingModule:MonoBehaviour, IModule, IChargeable
    {
        private ActorHost host;
        private Rigidbody rb;

        private static readonly Intent[] reactsTo = { Intent.Interact };
        public IEnumerable<Intent> ReactsTo => reactsTo;
        private Tag BlockedBy => Tag.Interacting;
        
        private IDock currentDocking;
        void Awake()
        {
            host = GetComponentInParent<ActorHost>();
            rb = host?.GetComponent<Rigidbody>();
        }
        void OnEnable() => host.Actor.RegisterModule(this);
        void OnDisable() => host.Actor.RemoveModule(this);
        public void Handle(Actor owner, Command cmd)
        {
            if (host.Actor.Tags.HasAny(Tag.Charging))
            {
                StopCharge();
            }
        }

        public void StartDocking(IDock dock)
        {
            dock.Dock(rb);
            currentDocking = dock;
            dock.Docked += SetChargingTag;
        }

        private void SetChargingTag()
        {
            host.Actor.Tags.Remove(Tag.Interacting);
            host.Actor.Tags.Add(Tag.Charging);
        }

        public void StopCharge()
        {
            currentDocking.Docked -= SetChargingTag;
            currentDocking.UnDock(rb);
            host.Actor.Tags.Remove(Tag.Charging);
            currentDocking = null;
        }
    }
}