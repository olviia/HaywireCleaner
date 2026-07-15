using System;
using System.Collections.Generic;

namespace Core.Player
{
    /// <summary>
    /// this class keeps track of available playable characters and who is
    /// an active one
    /// </summary>
    public static class Posession
    {
        private static IPosessable current;
        private static List<IPosessable> available = new();
        
        //can be called by actor.onEable or whenever we want
        public static void Register(IPosessable actor)
        {
            available.Add(actor);
        }

        public static void Unregister(IPosessable actor)
        {
            available.Remove(actor);
        }

        public static void Posess(IPosessable next)
        {
            current?.OnUnposessed();
            current = next;
            next.OnPosessed();
            
        }

        //public static event Action<IPosessable, IPosessable> OnPosessionChanged;
        
        public static IReadOnlyList<IPosessable> Available => available;
    }
}