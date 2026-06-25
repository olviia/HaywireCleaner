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

        [SerializeField] private int speed;
        
        
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
            //TODO: imiplement tags
            
            // if (owner.Tags.HasAny(BlockedBy) || !FacingClimbable()) return;
            // owner.Tags.Add("Climbing");

            var direction = cmd.ExtraInfo;
            rb.linearVelocity = new Vector3(direction.x, 0, direction.y) * speed;
            
            Debug.Log(direction);

        }
    }
}