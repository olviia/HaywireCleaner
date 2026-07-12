using System.Collections.Generic;
using System.Reflection;
using Core.SaveSystem;
using UnityEditor;

namespace Tools.FactKeyRegistry
{
    /// <summary>
    /// For keys that are contstants in the code, hand written
    /// </summary>
    public sealed class ConstKeySource: IFactKeySource
    {
        public IEnumerable<string> GetFactKeys()
        {
            foreach (var type in TypeCache.GetTypesWithAttribute<FactKeySourceAttribute>())
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (!field.IsLiteral || field.FieldType != typeof(string)) continue;
                    yield return (string)field.GetRawConstantValue();
                }
            }
        }
    }
}