using System;
using Core.Player;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(menuName = "Cleanbot/UI/Prompt Display Request")]
    public class UIPromptDisplayRequestSO:ScriptableObject
    {
        public event Action<string, Intent> Show;
        public event Action Hide;
        
        //fix show
        public void RaiseShow(string text, Intent intent) 
            => Show?.Invoke(text, intent);
        
        public void RaiseHide() => Hide?.Invoke();
        
        
    }
}