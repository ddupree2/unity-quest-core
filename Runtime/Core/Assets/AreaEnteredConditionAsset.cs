#nullable enable
using DynamicBox.Quest.Core.Events;
using UnityEngine;
using DynamicBox.Quest.Core.Conditions;

namespace DynamicBox.Quest.Core.Conditions
{
    /// <summary>
    /// Condition asset that completes when a specific area is entered.
    /// Listens to the assigned AreaEnteredScriptableEvent asset.
    /// </summary>
    [CreateAssetMenu(menuName = "DynamicBox/Quest/Conditions/Area Entered Condition", fileName = "NewAreaEnteredCondition")]
    public class AreaEnteredConditionAsset : ConditionAsset
    {
        [Tooltip("Event asset the game raises when the player enters an area.")]
        [SerializeField] private AreaEnteredScriptableEvent? _areaEnteredEvent;
        [SerializeField, TextArea(2, 3)] private string _areaDescription = string.Empty;
        
        /// <summary>
        /// Gets the descriptive text for this area (optional).
        /// </summary>
        public string AreaDescription => _areaDescription;

        public override IConditionInstance CreateInstance()
        {
            return new AreaEnteredConditionInstance(_areaEnteredEvent, ConditionId, _areaDescription);
        }
    }
}
