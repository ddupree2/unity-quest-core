#nullable enable
using System.Reflection;
using DynamicBox.Quest.Core;
using DynamicBox.Quest.Core.Conditions;
using DynamicBox.Quest.Core.Events;
using UnityEngine;

namespace DynamicBox.Quest.Tests
{
    /// <summary>
    /// Shared event assets for tests, standing in for the ones a game would author.
    /// Every test condition listens to the same asset per payload type, so raising one reaches
    /// every bound condition of that type (tests filter by id, as the conditions do).
    /// </summary>
    public static class TestEvents
    {
        private static ItemCollectedScriptableEvent? _items;
        private static AreaEnteredScriptableEvent? _areas;
        private static FlagChangedScriptableEvent? _flags;

        public static ItemCollectedScriptableEvent Items => _items != null ? _items : _items = Create<ItemCollectedScriptableEvent>();
        public static AreaEnteredScriptableEvent Areas => _areas != null ? _areas : _areas = Create<AreaEnteredScriptableEvent>();
        public static FlagChangedScriptableEvent Flags => _flags != null ? _flags : _flags = Create<FlagChangedScriptableEvent>();

        /// <summary>
        /// Assigns the shared event asset to a built-in condition asset created in a test,
        /// in place of the Inspector assignment a designer would make. Returns the asset for chaining.
        /// </summary>
        public static T Wire<T>(T conditionAsset) where T : ConditionAsset
        {
            switch (conditionAsset)
            {
                case ItemCollectedConditionAsset:
                    SetField(conditionAsset, "itemCollectedEvent", Items);
                    break;
                case AreaEnteredConditionAsset:
                    SetField(conditionAsset, "_areaEnteredEvent", Areas);
                    break;
                case CustomFlagConditionAsset:
                    SetField(conditionAsset, "_flagChangedEvent", Flags);
                    break;
            }

            return conditionAsset;
        }

        private static T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            // Survive scene cleanup between tests
            asset.hideFlags = HideFlags.HideAndDontSave;
            return asset;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new System.MissingFieldException(target.GetType().Name, fieldName);
            field.SetValue(target, value);
        }
    }
}
