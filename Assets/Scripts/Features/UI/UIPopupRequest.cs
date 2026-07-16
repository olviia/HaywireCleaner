using System;
using Core.Events;
using Core.Input;
using UnityEngine;

namespace Features.UI
{
    public class UIPopupRequest:MonoBehaviour
    {
        [SerializeField] private GameObject prefab;

        [SerializeField] private UIElementDisplayRequestSO request;

        private void OnEnable()
        {
            Show();
        }

        private void OnDisable()
        {
            Hide();
        }

        public void Show()
        {
            request.RaiseShow(prefab);
        }

        public void Hide()
        {
            request.RaiseHide(prefab);
        }
    }
}