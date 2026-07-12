using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Tools.FactKeyRegistry
{
    public class FactKeyDropdownItem:AdvancedDropdownItem
    {
        public readonly string Key;
        public FactKeyDropdownItem(string display, string key) : base(display) => Key = key;
    }

    public sealed class FactKeyDropdown :AdvancedDropdown
    {
        private readonly Action<string> _onSelected;
        
        public FactKeyDropdown(AdvancedDropdownState state, Action<string> onSelected) : base(state)
        {
            _onSelected = onSelected;
            minimumSize = new Vector2(250, 300); //change size of dropdown if needed here
        }
        
        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Fact Keys");
            var keys = FactKeyRegistry.Collect();
            keys.Sort(StringComparer.Ordinal);

            var nodes = new Dictionary<string, AdvancedDropdownItem>();

            foreach (var key in keys)
            {
                var segments = key.Split('.');
                var parent = root;
                var pathSoFar = "";

                for (int s = 0; s < segments.Length; s++)
                {
                    pathSoFar = s == 0?segments[s] : pathSoFar + "." + segments[s];
                    bool isLeaf = s == segments.Length - 1;

                    if (!nodes.TryGetValue(pathSoFar, out var node))
                    {
                        node = isLeaf 
                            ? new FactKeyDropdownItem(segments[s], key)
                            : new AdvancedDropdownItem(segments[s]);
                        parent.AddChild(node);
                        nodes[pathSoFar] = node;
                    }

                    parent = node;
                }
            }
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is FactKeyDropdownItem leaf)
                _onSelected?.Invoke(leaf.Key);
        }
    }
}