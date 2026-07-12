using Features.Quests;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Tools.FactKeyRegistry
{
    [CustomPropertyDrawer(typeof(FactCondition))]
    public class FactConditionDrawer:PropertyDrawer
    {
        private AdvancedDropdownState _state;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var keyProp = property.FindPropertyRelative("factKey");
            var testProp = property.FindPropertyRelative("test");
            var valueProp = property.FindPropertyRelative("value");

            const float spacing = 4f, valueW = 44f, testW = 110f;
            float keyW = position.width - testW - valueW - spacing * 2;
            float h = EditorGUIUtility.singleLineHeight;
            
            var keyRect = new Rect(position.x, position.y, keyW, h);
            var testRect = new Rect(keyRect.xMax + spacing, position.y, testW, h);
            var valueRect = new Rect(testRect.xMax + spacing, position.y, valueW, h);
            
            var display = string.IsNullOrEmpty(keyProp.stringValue) ? "(pick fact key)" : keyProp.stringValue;

            if (EditorGUI.DropdownButton(keyRect, new GUIContent(display, display), FocusType.Keyboard,
                    EditorStyles.popup))
            {
                _state ??=new AdvancedDropdownState();
                var so = keyProp.serializedObject;
                var path = keyProp.propertyPath;
                
                new FactKeyDropdown(_state, chosen =>
                {
                    var p = so.FindProperty(path);
                    if (p != null)
                    {
                        p.stringValue = chosen;
                        so.ApplyModifiedProperties();
                    }
                } ).Show(keyRect);
            }
            EditorGUI.PropertyField(testRect, testProp, GUIContent.none);

            if ((FactTest)testProp.enumValueIndex == FactTest.CounterAtLeast)
            {
                EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
            }
            EditorGUI.EndProperty();
        }
    }
}