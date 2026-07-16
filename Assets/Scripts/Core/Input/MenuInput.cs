using System;
using UnityEngine;

namespace Core.Input
{
    public static class MenuInput
    {
        public static event Action ToggleMenu;
        public static event Action ConfirmDown;
        public static event Action ConfirmUp;
        
        public static void RaiseToggleMenu() => ToggleMenu?.Invoke();
        
        public static void RaiseConfirmDown() => ConfirmDown?.Invoke();
        public static void RaiseConfirmUp() => ConfirmUp?.Invoke();
    }
}