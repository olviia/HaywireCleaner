using System;
using UnityEngine;

namespace Core.Input
{
    public static class MenuInput
    {
        public static event Action ToggleMenu;
        
        public static void RaiseToggleMenu()
        {
            ToggleMenu?.Invoke();
            Debug.Log("Toggle Menu");
        }
    }
}