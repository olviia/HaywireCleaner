using System;
using Core.Input;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Features.UI
{
    public class UIHoldToConfirm:MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private float holdSeconds = 0.5f;
        [SerializeField] private UnityEvent onCompleted;

        private bool held;
        private bool fired;
        private float elapsed;

        private void OnEnable()
        {
            MenuInput.ConfirmDown += OnDown;
            MenuInput.ConfirmUp += OnUp;
            ResetHold();
        }

        private void OnDisable()
        {
            MenuInput.ConfirmDown -= OnDown;
            MenuInput.ConfirmUp -= OnUp;
        }

        private void Update()
        {
            if (!held) return;
            elapsed +=Time.unscaledDeltaTime;
            fill.fillAmount = elapsed / holdSeconds;

            if (!fired && elapsed >= holdSeconds)
            {
                fired = true;
                onCompleted?.Invoke();
            }
        }
        
        private void OnDown() => held = true;

        private void OnUp() => ResetHold();

        private void ResetHold()
        {
            held = false;
            fired = false;
            elapsed = 0f;
            fill.fillAmount = 0f;
        }
    }
}