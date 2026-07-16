using Core.Events;
using Core.Interaction;
using Core.Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace Features.Interactables
{
    public class SlidingDoors:MonoBehaviour, IInteractable
    {
        [SerializeField] private UIPromptDisplayRequestSO promptDisplayRequestSo;
        [SerializeField] private UIPromptPositionRequestSO promptPositionRequestSo;
        [SerializeField] private LocalizedString promptOpen;
        [SerializeField] private LocalizedString promptClose;
        
        [SerializeField] private Animator animator;
        [SerializeField] private UnityEvent onOpened;
        
        private bool isOpen;
        private bool isBusy;
        private string Prompt => isOpen ? promptClose.GetLocalizedString() : promptOpen.GetLocalizedString();

        public bool CanInteract(Actor actor) => !isBusy;

        public void OnFocus(Vector3 hitPoint)
        {
            promptDisplayRequestSo.RaiseShow(Prompt, Intent.Interact);
            promptPositionRequestSo.RaiseSetPosition(hitPoint);

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

        public void OnMotionFinished()
        {
            isBusy = false;
            if(isOpen) onOpened?.Invoke();
        }
    }
}