using Core.Events;
using Core.Interaction;
using Core.Player;
using UnityEngine;
using UnityEngine.Localization;

namespace Features.Interactables
{
    public class SlidingDoors:MonoBehaviour, IInteractable
    {
        [SerializeField] private UIInteractPromptDisplayRequestSO promptDisplayRequestSo;
        [SerializeField] private LocalizedString promptOpen;
        [SerializeField] private LocalizedString promptClose;
        
        [SerializeField] private Animator animator;
        private bool isOpen;
        private bool isBusy;
        private string Prompt => isOpen ? promptClose.GetLocalizedString() : promptOpen.GetLocalizedString();

        public bool CanInteract(Actor actor) => !isBusy;

        public void OnFocus()
        {
            promptDisplayRequestSo.RaiseShow(Prompt, Intent.Interact, this.transform);

        }

        public void OnUnfocus()
        {
            promptDisplayRequestSo.RaiseHide();

        }

        public void Interact(Actor actor)
        {
            isOpen = !isOpen;
            isBusy = true;
            animator.SetBool("isOpen", isOpen);
        }

        public void OnMotionFinished() => isBusy = false;
    }
}