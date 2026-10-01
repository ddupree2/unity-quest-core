using System;
using DynamicBox.Quest.Core;
using UnityEditor;

namespace DynamicBox.Quest.Editor.GraphEditor
{
    /// <summary>
    /// Node for any ConditionAsset type. Shows a short summary of the asset's serialized fields;
    /// editing happens in the side panel, which draws the asset's own inspector. New condition
    /// types need no node code.
    /// </summary>
    public class ConditionNode : BaseConditionNode
    {
        private const int MaxSummaryLines = 4;
        private const int MaxValueLength = 28;

        public ConditionNode(ConditionAsset asset) : this(asset, asset != null ? asset.GetType() : null)
        {
        }

        public ConditionNode(ConditionAsset asset, Type conditionType) : base(asset, conditionType)
        {
            title = ConditionTypeCatalog.GetDisplayName(ConditionType).ToUpperInvariant();

            BuildContent();
            RefreshExpandedState();
            RefreshPorts();
        }

        public override void RefreshNode()
        {
            BuildContent();
            RefreshExpandedState();
        }

        private void BuildContent()
        {
            Body.Clear();

            if (Asset == null)
            {
                Body.Add(CreateLabel("New Condition (Not Saved)", "node-placeholder"));
                return;
            }

            Body.Add(CreateLabel(Asset.name, "node-name-label"));

            using (var serializedObject = new SerializedObject(Asset))
            {
                SerializedProperty property = serializedObject.GetIterator();
                int shown = 0;
                bool enterChildren = true;

                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    // The script reference and Quest Core's auto-generated id aren't settings
                    if (property.propertyPath == "m_Script" || property.propertyPath == "conditionId")
                    {
                        continue;
                    }

                    if (shown == MaxSummaryLines)
                    {
                        Body.Add(CreateLabel("…", "node-description-label"));
                        break;
                    }

                    Body.Add(CreatePropertyDisplay(property.displayName, Describe(property)));
                    shown++;
                }
            }
        }

        private static string Describe(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null ? property.objectReferenceValue.name : "None";
                case SerializedPropertyType.Boolean:
                    return property.boolValue ? "Yes" : "No";
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString();
                case SerializedPropertyType.Float:
                    return property.floatValue.ToString("0.##");
                case SerializedPropertyType.String:
                    return Truncate(string.IsNullOrEmpty(property.stringValue) ? "(empty)" : property.stringValue);
                case SerializedPropertyType.Enum:
                    int index = property.enumValueIndex;
                    return index >= 0 && index < property.enumDisplayNames.Length ? property.enumDisplayNames[index] : "?";
            }

            if (property.isArray)
            {
                return $"{property.arraySize} item(s)";
            }

            return "…";
        }

        private static string Truncate(string value)
        {
            return value.Length > MaxValueLength ? value.Substring(0, MaxValueLength) + "…" : value;
        }
    }
}
