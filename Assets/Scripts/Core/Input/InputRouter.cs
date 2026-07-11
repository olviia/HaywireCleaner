using System;
using System.Collections.Generic;

namespace Core.Input
{
    public static class InputRouter
    {
        private static readonly List<InputContext> stack = new();
        public static event Action<InputContext> ContextChangedTo;
        public static InputContext ActiveContext => stack.Count > 0 ? stack[stack.Count - 1] : InputContext.Gameplay;
        public static void Enter(InputContext c)
        {
            stack.Add(c);
            ContextChangedTo?.Invoke(ActiveContext);
        }

        public static void Exit(InputContext c)
        {
            stack.Remove(c);
            ContextChangedTo?.Invoke(ActiveContext);
        }
    }
}