using System;
using Core.Events;
using Core.Input;
using Core.Player;
using UnityEngine;

namespace Tests
{
    public class GlyphLogTest:MonoBehaviour
    {
        [SerializeField] private UIInteractPromptDisplayRequestSO displayRequestSO;
        private void OnEnable() => displayRequestSO.Show += OnShow;

        private void OnDisable() => displayRequestSO.Show -= OnShow;

        private void OnShow(string label, Intent intent, Transform _)
        {
            var glyph = GlyphInput.Glyphs?.GetGlyph(intent);
            Debug.Log($"[prompt] {label} button = {glyph?.label}");
        }
    }
}