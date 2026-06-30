using System;
using System.Collections.Generic;
using Core.Interaction;
using Features.Character;
using UnityEngine;

namespace Features.Interaction
{
    /// <summary>
    /// this class checks the surroundings if there is something we can interact with.
    /// Should sit as a child the actor component
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractionSensor:MonoBehaviour
    {
        private ActorHost host;

        private readonly List<IInteractable> inRange = new();
        private IInteractable focused;

        private void Awake()
        {
            host = GetComponentInParent<ActorHost>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out IInteractable it) && !inRange.Contains(it))
            {
                inRange.Add(it);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out IInteractable it))
            {
                inRange.Remove(it);
            }
        }

        void Update()
        {
            //here select the nearest interactable
            IInteractable best = Nearest();
            if (best == focused) return;

            if (focused != null)
            {
                host.Actor.Focus.Clear(focused);
            }
            focused = best;
            if (focused != null)
            {
                host.Actor.Focus.Set(focused);
            }
        }

        private IInteractable Nearest()
        {
            IInteractable best = null;
            float min = float.MaxValue;
            Vector3 me = transform.position;

            for (int i = 0; i < inRange.Count; i++)
            {
                var it = inRange[i];
                if (!it.CanInteract(host.Actor)) continue;
                float d = (((Component)it).transform.position - me).sqrMagnitude;
                if (d < min)
                {
                    min = d;
                    best = it;
                }
            }
            return best;
        }
    }
}