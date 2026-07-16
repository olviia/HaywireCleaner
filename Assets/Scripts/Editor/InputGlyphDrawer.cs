using System;
using System.Collections.Generic;
using Features.UI.TextDisplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tools
{
    [CustomPropertyDrawer(typeof(InputActionKeyAttribute))]
    public class InputGlyphDrawer:PropertyDrawer
    {
        private const string None = "(none)";
        private string[] _options;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            _options ??= BuildOptions();
            int current = Array.IndexOf(_options, property.stringValue);
            int index = current<0?0:current;
            
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(position,label.text, index,_options);
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = picked == 0?string.Empty:_options[picked];
            }
            EditorGUI.EndProperty();
            
        }

        private string[] BuildOptions()
        {
            var options = new List<string>{None};
            var guids = AssetDatabase.FindAssets($"t:InputActionAsset");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (asset != null)
                    foreach(var map in asset.actionMaps)
                        foreach(var action in map.actions)
                            options.Add($"{map.name}/{action.name}");
            }
            return options.ToArray();
        }
    }
}