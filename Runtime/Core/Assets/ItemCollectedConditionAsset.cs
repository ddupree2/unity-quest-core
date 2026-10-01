#nullable enable
using DynamicBox.Quest.Core.Events;
using UnityEngine;

namespace DynamicBox.Quest.Core.Conditions
{
    /// <summary>
    /// Condition asset that completes when a specific item is collected.
    /// Listens to the assigned ItemCollectedScriptableEvent asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Star Stone Studio/Quests/Legacy/Item Collected", fileName = "NewItemCollectedCondition")]
    public class ItemCollectedConditionAsset : ConditionAsset
    {
        [Tooltip("Event asset the game raises when an item is collected.")]
        [SerializeField] private ItemCollectedScriptableEvent? itemCollectedEvent;
        [SerializeField] private int requiredCount = 1;
        
        /// <summary>
        /// Gets the number of items required to complete this condition.
        /// </summary>
        public int RequiredCount => requiredCount;

        public override IConditionInstance CreateInstance()
        {
            return new ItemCollectedConditionInstance(itemCollectedEvent, ConditionId, requiredCount);
        }
    }
}
