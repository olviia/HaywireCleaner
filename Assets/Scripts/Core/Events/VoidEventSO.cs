using System;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(menuName = "Cleanbot/Events/VoidEventSO")]
    public class VoidEventSO:ScriptableObject
    {
        public event Action Raised;
        public void RaiseAction() => Raised?.Invoke();
    }
}