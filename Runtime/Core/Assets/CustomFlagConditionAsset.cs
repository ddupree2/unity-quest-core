#nullable enable
using DynamicBox.Quest.Core.Events;
using UnityEngine;
using DynamicBox.Quest.Core.Conditions;

namespace DynamicBox.Quest.Core.Conditions
{
    /// <summary>
    /// Condition asset that completes when a custom flag matches an expected value.
    /// Listens to the assigned FlagChangedScriptableEvent asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Star Stone Studio/Quests/Legacy/Custom Flag Condition", fileName = "NewCustomFlagCondition")]
    public class CustomFlagConditionAsset : ConditionAsset
    {
        [Tooltip("Event asset the game raises when a flag changes.")]
        [SerializeField] private FlagChangedScriptableEvent? _flagChangedEvent;
        [SerializeField] private bool _expectedValue = true;
        [SerializeField, TextArea(2, 3)] private string _description = string.Empty;
        
        /// <summary>
        /// Gets the expected boolean value for this flag to complete the condition.
        /// </summary>
        public bool ExpectedValue => _expectedValue;
        
        /// <summary>
        /// Gets the descriptive text for this flag condition (optional).
        /// </summary>
        public string Description => _description;

        public override IConditionInstance CreateInstance()
        {
            return new CustomFlagConditionInstance(_flagChangedEvent, ConditionId, _expectedValue, _description);
        }
    }
}
