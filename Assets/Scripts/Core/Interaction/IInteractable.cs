using Core.Player;
using UnityEngine;

namespace Core.Interaction
{
    public interface IInteractable
    {
        bool CanInteract(Actor actor);
        void OnFocus(Vector3 hitPoint); //show highlight or button
        void OnUnfocus(); // hide highlight or button
        void Interact(Actor actor); //toggle the interaction with actor
    }
}