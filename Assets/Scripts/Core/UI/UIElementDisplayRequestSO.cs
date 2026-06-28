using System;
using UnityEngine;

namespace Core.UI
{
    /// <summary>
    /// analog of unreal WidgetChannel
    /// </summary>
    [CreateAssetMenu(menuName = "Cleanbot/UI/UI Element ShowHide Request")]
    public class UIElementDisplayRequestSO : ScriptableObject
    {
        //passed prefab has to be the same
        public event Action<GameObject> Show;
        public event Action<GameObject> Hide;
        public void RaiseShow(GameObject prefab) => Show?.Invoke(prefab);
        public void RaiseHide(GameObject prefab) => Hide?.Invoke(prefab);
    }
}