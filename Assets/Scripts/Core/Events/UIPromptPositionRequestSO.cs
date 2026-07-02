using System;
using UnityEngine;

namespace Core.Events
{
    [CreateAssetMenu(menuName = "Cleanbot/UI/Prompt Position Request")]
    public class UIPromptPositionRequestSO:ScriptableObject
    {
        public event Action<Vector3> SetPosition;
        
        public void RaiseSetPosition(Vector3 position) => SetPosition?.Invoke(position);
    }
}