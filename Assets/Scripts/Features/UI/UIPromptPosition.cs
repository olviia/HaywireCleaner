using System;
using Core.Events;
using Core.Input;
using Core.Player;
using TMPro;
using UnityEngine;

namespace Features.UI
{
    /// <summary>
    /// this script only positions the prompt where told to
    /// </summary>
    public class UIPromptPosition:MonoBehaviour
    {
        [SerializeField] private UIPromptPositionRequestSO positionRequest;
        //distance between prompt and interactable object
        [SerializeField]private Vector3 worldOffset = Vector3.up; 

        private Camera cam;
        private Vector3 worldPoint;
        private bool hasTarget;

        

        void OnEnable()
        {
            positionRequest.SetPosition += OnSetPosition;
        }

        void OnDisable()
        {
            positionRequest.SetPosition -= OnSetPosition;
        }

        void OnSetPosition(Vector3 hitPoint)
        {
            cam = Camera.main;
            worldPoint = hitPoint;
            hasTarget = true;

            PositionToAnchor();
        }


        void LateUpdate()
        {
            if (!hasTarget) return;
            PositionToAnchor();
        }

        void PositionToAnchor()
        {
            var screen = cam.WorldToScreenPoint(worldPoint + worldOffset);
            if (screen.z <= 0f) return; // dont move behind the screen
            this.transform.position = screen;
        }

    }
}