using System;
using Core.Player;
using UnityEngine;

namespace Core.Input
{
    /// <summary>
    /// this class wires Command pattern to Actor
    /// intent event transport
    /// execution projection
    /// </summary>
    public static class ModuleInput
    {
        public static event Action<Intent, Vector2> OnIntent;
        
        public static void RaiseMove(Vector2 direction) => OnIntent?.Invoke(Intent.Move, direction);

        public static void RaiseInteract() => OnIntent?.Invoke(Intent.Interact, default);
        //all the other commands here
    }
}