using System;
using Core.Player;
using UnityEngine;

namespace Core.UI
{
    [CreateAssetMenu(menuName = "Cleanbot/UI/Interact Prompt Request")]
    public class InteractPromptRequestSO:ScriptableObject
    {
        public event Action<string, Intent> Show;
        public event Action Hide;
        
        public void RaiseShow(string text, Intent intent) => Show?.Invoke(text, intent);
        public void RaiseHide() => Hide?.Invoke();
    }
}