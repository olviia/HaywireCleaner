using Core.Interaction;

namespace Core.Player
{
    /// <summary>
    /// this class defines what interactable we currently can interact with
    /// </summary>
    public class InteractionFocus
    {
        public IInteractable Current { get; private set; }

        public void Set(IInteractable next)
        {
            if (Current == next) return; 
            Current?.OnUnfocus();
            Current = next;
            Current?.OnFocus();
        }

        public void Clear(IInteractable leaving)
        {
            if(Current != leaving) return;
            Current?.OnUnfocus();
            Current = null;
        }
    }
}