using System.Collections;
using System.Collections.Generic;
using Core.Player;
using UnityEngine;

namespace Features.Modules
{
    public class ChargingModule:MonoBehaviour, IModule
    {
        private ActorHost host;
        private Rigidbody rb;
        private Coroutine goToCharge;

        [SerializeField] private float moveToDockDuration = 0.6f;

        private static readonly Intent[] reactsTo = { Intent.Charge , Intent.StopCharge };
        public IEnumerable<Intent> ReactsTo => reactsTo;
        
        void Awake()
        {
            host = GetComponentInParent<ActorHost>();
            rb = host?.GetComponent<Rigidbody>();
        }
        void OnEnable() => host.Actor.RegisterModule(this);
        void OnDisable() => host.Actor.RemoveModule(this);
        public void Handle(Actor owner, Command cmd)
        {
            switch (cmd.WhatToDo)
            {
                case Intent.Charge      :Dock(owner, cmd.Position); break;
                case Intent.StopCharge  :Undock(owner); break;
            }
        }

        private void Dock(Actor owner, Transform anchor)
        {
            if (owner.Tags.HasAny(Tag.Charging) || anchor == null) return;
            owner.Tags.Add(Tag.Charging);
            rb.isKinematic = true; // to move smoothly and not fighting with physics
            if(goToCharge != null)StopCoroutine(goToCharge); // some stopper for failstate
            goToCharge = StartCoroutine(GoToCharge(anchor));
        }

        private void Undock(Actor owner)
        {
            if (!owner.Tags.HasAny(Tag.Charging)) return;
            if (goToCharge != null)
            {
                StopCoroutine(goToCharge);
                goToCharge = null;
            }
            rb.isKinematic = false;
            owner.Tags.Remove(Tag.Charging);
        }

        private IEnumerator GoToCharge(Transform anchor)
        {
            Vector3 startPos = rb.position;
            Quaternion startRot = rb.rotation;
            float elapsed = 0f;

            while (elapsed < moveToDockDuration)
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.SmoothStep(0.0f, 1.0f, elapsed / moveToDockDuration);
                
                rb.MovePosition(Vector3.Lerp(startPos, anchor.position, t));
                rb.MoveRotation(Quaternion.Slerp(startRot, anchor.rotation, t));
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(anchor.position); //exact snap
            rb.MoveRotation(anchor.rotation);
            goToCharge = null;
        }
    }
}