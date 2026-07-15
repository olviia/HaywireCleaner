using Core.Input;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Features.UI.TextDisplay
{
    public class InputGlyphText:MonoBehaviour
    {
        [SerializeField] private LocalizeStringEvent localizedString;
        [SerializeField, InputActionKey] private string[] actionKeys;

        private void OnEnable()
        {
            RefreshGlyphs();                                       
            if (GlyphInput.Glyphs != null)
                GlyphInput.Glyphs.DeviceChanged += RefreshGlyphs;
        }

        private void OnDisable()
        {
            if (GlyphInput.Glyphs != null)
                GlyphInput.Glyphs.DeviceChanged -= RefreshGlyphs;
        }

        private void RefreshGlyphs()
        {
            var glyphs = GlyphInput.Glyphs;
            var args = new object[actionKeys.Length];
            for (int i = 0; i < actionKeys.Length; i++)
            {   
                args[i] = glyphs?.GetGlyph(actionKeys[i]).label;
            }

            localizedString.StringReference.Arguments = args;
            localizedString.RefreshString();

        }
    }
}