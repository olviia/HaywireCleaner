using System;
using Core.Events;
using Core.Input;
using Core.Player;
using TMPro;
using UnityEngine;

namespace Features.UI
{
    /// <summary>
    /// this prompt does not instantiate the prefab, it turns the
    /// prefab that is already there, on and off
    /// </summary>
    public class UIInteractPrompt:MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        //might be changed to images later
        [SerializeField] private TMP_Text button;
        [SerializeField] private UIInteractPromptDisplayRequestSO displayRequest;
        
        //distance between prompt and interactable object
        [SerializeField]private Vector3 worldOffset = Vector3.up; 
        
        private CanvasGroup canvasGroup;
        private Camera cam;

        private Transform anchor;
        private Intent currentIntent;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
        }

        void OnEnable()
        {
            displayRequest.Show += OnShow;
            displayRequest.Hide += OnHide;
        }

        void OnDisable()
        {
            displayRequest.Show -= OnShow;
            displayRequest.Hide -= OnHide;
        }

        void OnShow(string labelText, Intent intent, Transform interactionObject)
        {
            cam = Camera.main;
            currentIntent = intent;
            anchor = interactionObject;
            label.text = labelText;
            RefreshGlyph();

            if (GlyphInput.Glyphs != null)
            {
                GlyphInput.Glyphs.DeviceChanged += RefreshGlyph;
            }
            
            PositionToAnchor();//
        }

        void OnHide()
        {
            if (GlyphInput.Glyphs != null)
            {
                GlyphInput.Glyphs.DeviceChanged -= RefreshGlyph;
            }

            anchor = null;
            canvasGroup.alpha = 0;
        }

        void LateUpdate()
        {
            if (anchor == null) return;
            PositionToAnchor();
        }

        void PositionToAnchor()
        {
            var screen = cam.WorldToScreenPoint(anchor.position + worldOffset);
            if (screen.z <= 0f)
            {
                canvasGroup.alpha = 0;
                return;
            }
            canvasGroup.alpha = 1;
            canvasGroup.transform.position = screen;
        }

        void RefreshGlyph()
        {
            var glyph = GlyphInput.Glyphs?.GetGlyph(currentIntent);
            button.text = glyph?.label ?? "?";
        }


    }
}