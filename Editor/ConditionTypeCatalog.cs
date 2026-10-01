using System;
using System.Collections.Generic;
using System.Linq;
using DynamicBox.Quest.Core;
using DynamicBox.Quest.Core.Conditions;
using UnityEditor;

namespace DynamicBox.Quest.Editor
{
    /// <summary>
    /// Every condition type designers can create, found by reflection so a new ConditionAsset
    /// subclass shows up in the graph's Add Condition menu and the objective inspector without
    /// editor code. Shared by the inspectors and the graph editor; lives outside GraphEditor/ so
    /// the inspectors don't depend on the graph.
    /// </summary>
    public static class ConditionTypeCatalog
    {
        // Quest Core's original conditions that identify things by typed strings. Existing assets
        // still load and display; they just aren't offered for new conditions.
        private static readonly HashSet<Type> HiddenTypes = new HashSet<Type>
        {
            typeof(ItemCollectedConditionAsset),
            typeof(AreaEnteredConditionAsset),
            typeof(CustomFlagConditionAsset),
        };

        private static List<Type> _creatableTypes;

        /// <summary>Concrete condition types offered for new conditions, sorted by display name.</summary>
        public static IReadOnlyList<Type> CreatableTypes
        {
            get
            {
                // TypeCache is rebuilt on domain reload, which also resets this cache
                if (_creatableTypes == null)
                {
                    _creatableTypes = TypeCache.GetTypesDerivedFrom<ConditionAsset>()
                        .Where(IsCreatable)
                        .OrderBy(GetDisplayName)
                        .ToList();
                }

                return _creatableTypes;
            }
        }

        /// <summary>"WorldFlagConditionAsset" → "World Flag".</summary>
        public static string GetDisplayName(Type type)
        {
            if (type == null)
            {
                return "Condition";
            }

            string name = type.Name;
            if (name.EndsWith("ConditionAsset") && name.Length > "ConditionAsset".Length)
            {
                name = name.Substring(0, name.Length - "ConditionAsset".Length);
            }
            else if (name.EndsWith("Asset") && name.Length > "Asset".Length)
            {
                name = name.Substring(0, name.Length - "Asset".Length);
            }

            return ObjectNames.NicifyVariableName(name);
        }

        private static bool IsCreatable(Type type)
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition || HiddenTypes.Contains(type))
            {
                return false;
            }

            // Test mocks are loaded in the editor too; keep them out of authoring menus
            string assembly = type.Assembly.GetName().Name;
            return !assembly.EndsWith(".Tests") && !assembly.EndsWith(".Tests.Editor");
        }
    }
}
