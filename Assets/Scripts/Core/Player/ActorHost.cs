using System;
using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// even if it is a monobehaviour, this script belongs to core because it wires the
    /// actor script to the actor game object on the scene
    /// </summary>
    public class ActorHost : MonoBehaviour
    {
        public Actor Actor { get; } = new Actor();
        void OnEnable() => Posession.Register(Actor);
        void OnDisable() => Posession.Unregister(Actor);
        
        [ContextMenu("Possess This")] 
        void TestPossess()
        {
            if (!Application.isPlaying) return;
            Posession.Posess(Actor);
        }

        //TODO:
        //remove when not testing
        private void Awake()
        {
            TestPossess();
        }
    }
}