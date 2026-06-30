using System;
using System.Collections.Generic;
using Core.Input;
using Core.Player;
using UnityEngine.InputSystem;

namespace Features.Input
{
    /// <summary>
    /// takes the map and asset, gives the glyph. this class gives the core
    /// the glyph based on the intent 
    /// </summary>
    public class InputGlyphProvider:IInputGlyphProvider, IDisposable
    {
        private readonly IReadOnlyDictionary<Intent, InputAction> intentInputMap;
        private readonly InputActionAsset actions; //to get buttons pressed

        private readonly Dictionary<Intent, Glyph> glyphMap = new(); //this is what we have to give back to core

        private InputControlScheme? scheme;

        public InputGlyphProvider(IReadOnlyDictionary<Intent, InputAction> intentInputMap, InputActionAsset actions)
        {
            this.intentInputMap = intentInputMap;
            this.actions = actions;
            scheme = SchemeForConnectedDevices();
            InputSystem.onActionChange += OnActionChange;
        }
        
        //pick active device
        private InputControlScheme? SchemeForConnectedDevices()
        {
            foreach (var device in InputSystem.devices)
            {
                var s = InputControlScheme.FindControlSchemeForDevice(device, actions.controlSchemes);
                if (s!=null) return s;
            }
            return actions.controlSchemes.Count>0? actions.controlSchemes[0] : (InputControlScheme?)null;
        }

        public Glyph GetGlyph(Intent intent)
        {
            if (glyphMap.TryGetValue(intent, out var g)) return g;
            g = Resolve(intent);
            glyphMap[intent] = g;
            return g;
        }
        
        private Glyph Resolve(Intent intent)
        {
            if (!intentInputMap.TryGetValue(intent, out var action))
                return new Glyph { label = "?" };
            
            return new Glyph //somehow get the string for glyph
            {
                label = action.GetBindingDisplayString(
                    InputBinding.DisplayStringOptions.DontIncludeInteractions,
                    scheme?.bindingGroup)
            };
        }

        public event Action DeviceChanged;

        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed || obj is not InputAction a) return;
            var device = a.activeControl?.device;
            if(device == null) return;
            var next = InputControlScheme.FindControlSchemeForDevice(device, actions.controlSchemes);
            if(next == null || next.Value.bindingGroup == scheme?.bindingGroup) return; 
            
            scheme = next;
            glyphMap.Clear();
            DeviceChanged?.Invoke();
        }

        public void Dispose() => InputSystem.onActionChange -= OnActionChange;
    }
}