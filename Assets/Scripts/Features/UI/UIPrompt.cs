using System;
using Core.Events;
using Core.Input;
using Core.Player;
using TMPro;
using UnityEngine;

namespace Features.UI
{
    public class UIPrompt:MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        //might be changed to images later
        [SerializeField] private TMP_Text glyphLabel; 
        [SerializeField]private CanvasGroup canvasGroup;
        [SerializeField] private UIPromptDisplayRequestSO displayRequest;
        
        private Intent currentIntent;

        private void Awake()
        {
            canvasGroup.alpha = 0; //jsut in case
        }
        
        void OnEnable()
        {
            displayRequest.Show += Show;
            displayRequest.Hide += Hide;
        }

        void OnDisable()
        {
            displayRequest.Show -= Show;
            displayRequest.Hide -= Hide;
        }

        void Show(string labelText, Intent intent)
        {
            label.text = labelText;
            
            currentIntent = intent;
            RefreshGlyph();

            if (GlyphInput.Glyphs != null)
            {
                GlyphInput.Glyphs.DeviceChanged += RefreshGlyph;
            }
            
            canvasGroup.alpha = 1;
        }
        
        void Hide()
        {
            if (GlyphInput.Glyphs != null)
            {
                GlyphInput.Glyphs.DeviceChanged -= RefreshGlyph;
            }
            canvasGroup.alpha = 0;
        }
        
        void RefreshGlyph()
        {
            var glyph = GlyphInput.Glyphs?.GetGlyph(currentIntent);
            glyphLabel.text = glyph?.label ?? "?";
        }
    }
}