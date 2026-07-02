using Core.Interaction;
using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// this class defines what interactable we currently can interact with
    /// </summary>
    public class InteractionFocus
    {
        public IInteractable Current { get; private set; }

        public void Set(IInteractable next, Vector3 hitPoint)
        {
            if (Current == next) return; 
            Current?.OnUnfocus();
            Current = next;
            Current?.OnFocus(hitPoint);
        }

        public void Clear(IInteractable leaving)
        {
            if(Current != leaving) return;
            Current?.OnUnfocus();
            Current = null;
        }
    }
}