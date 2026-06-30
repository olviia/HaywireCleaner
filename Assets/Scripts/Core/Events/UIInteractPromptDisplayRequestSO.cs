using System;
using Core.Player;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(menuName = "Cleanbot/UI/Interact Prompt Request")]
    public class UIInteractPromptDisplayRequestSO:ScriptableObject
    {
        public event Action<string, Intent, Transform> Show;
        public event Action Hide;
        
        //fix show
        public void RaiseShow(string text, Intent intent, Transform interactionObject) 
            => Show?.Invoke(text, intent, interactionObject);
        public void RaiseHide() => Hide?.Invoke();
    }
}