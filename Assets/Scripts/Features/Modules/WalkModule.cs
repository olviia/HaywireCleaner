using System;
using System.Collections.Generic;
using Core.Player;
using Features.Character;
using UnityEngine;

namespace Features.Modules
{
    /// <summary>
    /// as the module can be dropped only inside of the playable character,
    /// we can absolutely find our actor as a component in parent
    /// </summary>
    public class WalkModule : MonoBehaviour, IModule
    {
        private ActorHost host;
        private Rigidbody rb;

        [SerializeField] private float speed;
        [SerializeField] private float reverseSpeed;
        [SerializeField] private float rotationSpeed;

        private float moveInput;
        private float rotateInput;
        
        private static readonly Tag BlockedBy = Tag.Interacting;
        
        private static readonly Intent[] reactsTo = { Intent.Move };
        public IEnumerable<Intent> ReactsTo=>reactsTo;
        
        void Awake()
        {
            host = GetComponentInParent<ActorHost>();
            rb = host?.GetComponent<Rigidbody>();
        }

        void OnEnable() => host.Actor.RegisterModule(this);
        void OnDisable() => host.Actor.RemoveModule(this);
        public void Handle(Actor owner, Command cmd)
        {
             if (owner.Tags.HasAny(BlockedBy))
             {

                 moveInput = rotateInput = 0f;
                 return;
             }

             moveInput = cmd.ExtraInfo.y;
             rotateInput = cmd.ExtraInfo.x;
        }

        private void FixedUpdate()
        {
            rb.angularVelocity = Vector3.up * (rotateInput * rotationSpeed * Mathf.Deg2Rad);

            float movement = moveInput >= 0 ? speed : reverseSpeed;
            Vector3 forward = transform.forward * (moveInput * movement);
            rb.linearVelocity = new Vector3(forward.x, rb.linearVelocity.y, forward.z);

            Debug.Log($"inputs: {moveInput} / {rotateInput}  vel:{rb.linearVelocity}");
        }
    }
}