using System;
using UnityEngine;

namespace Core.Interaction
{
    /// <summary>
    /// this is for anything interactable, that can dock things on some place
    /// </summary>
    public interface IDock
    {
        void Dock(Rigidbody body);
        void UnDock(Rigidbody body);
        
        public event Action Docked;
    }
}