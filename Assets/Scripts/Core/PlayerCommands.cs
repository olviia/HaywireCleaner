using System;
using UnityEngine;

namespace Core
{
    public static class PlayerCommands
    {
        public static Vector2 CurrentMove { get; private set; }
        public static event Action OnInteract;
        
        public static void SetMove(Vector2 direction)
        {
            Debug.Log(direction);
            CurrentMove = direction;
        }

        public static void RaiseInteract() => OnInteract?.Invoke();
        //all the other commands here
    }
}