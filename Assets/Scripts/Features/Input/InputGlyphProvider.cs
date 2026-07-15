using System;
using System.Collections.Generic;
using System.Linq;
using Core.Input;
using Core.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

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

        private readonly Dictionary<string, Glyph> glyphMap = new(); //this is what we have to give back to core

        private InputControlScheme? scheme;
        private InputDevice activeDevice;
        public string KeyFor(Intent intent)
        {
            if (intentInputMap.TryGetValue(intent, out var action))
                return action.actionMap.name + "/" + action.name;
            return "";
        }
        
        public Glyph GetGlyph(string actionName)
        {
            if (glyphMap.TryGetValue(actionName, out var cached)) return cached;
            
            var action = actions.FindAction(actionName);
            var display = action == null ? "?" : DisplayFor(action, scheme?.bindingGroup);
            

            var g = new Glyph() { label = string.IsNullOrEmpty(display) ? "?" : display };
            
            glyphMap[actionName] = g;
            return g;
        }

        private static string DisplayFor(InputAction action, string group)
        {
            var mask = InputBinding.MaskByGroup(group);
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (!binding.isComposite && !binding.isPartOfComposite && mask.Matches(binding))
                    return action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
                if(binding.isComposite && i+ 1 <  bindings.Count && mask.Matches(bindings[i+1]))
                    return PrimaryKeys(action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions));
                
            }
            return string.Empty;
        }
        private static string PrimaryKeys(string display) =>
            string.Join("/", display.Split('/').Select(p => p.Split('|')[0].Trim()));

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
        

        public event Action DeviceChanged;

        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed || obj is not InputAction action) return;

            var device = action.activeControl?.device;   // the device that actually caused the input
            if (device == null || device == activeDevice) return;   

            activeDevice = device;  

            var next = SchemeForDevice(device);     
            if (next == null || next.Value.bindingGroup == scheme?.bindingGroup) return; 
            
            scheme = next;
            glyphMap.Clear();
            DeviceChanged?.Invoke();
        }
        
        private InputControlScheme? SchemeForDevice(InputDevice device)
        {
            foreach (var s in actions.controlSchemes)
            {
                foreach (var req in s.deviceRequirements)
                {
                    if (InputControlPath.Matches(req.controlPath, device))
                    {
                        return s;
                    }
                }
            }
            return null;
        }

        public void Dispose()=> InputSystem.onActionChange -= OnActionChange;
    }
}