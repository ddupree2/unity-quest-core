#nullable enable
using DynamicBox.Quest.Core.Events;
using StarStoneStudio.Scriptables;
using UnityEngine;

namespace DynamicBox.Quest.Core.Conditions
{
    /// <summary>
    /// Condition instance that tracks area entry events.
    /// Uses EventDrivenConditionBase to reduce boilerplate.
    /// </summary>
    public sealed class AreaEnteredConditionInstance : EventDrivenConditionBase<AreaEnteredEvent>, ISaveableCondition
    {
        private readonly string _areaId;
        private readonly string? _areaDescription;
        private bool _isCompleted;

        public override bool IsMet => _isCompleted;

        public AreaEnteredConditionInstance(ScriptableEvent<AreaEnteredEvent>? areaEnteredEvent, string areaId, string? areaDescription = null)
            : base(areaEnteredEvent)
        {
            _areaId = areaId;
            _areaDescription = areaDescription;
        }

        protected override void HandleEvent(AreaEnteredEvent evt)
        {
            if (evt.AreaId == _areaId && !_isCompleted)
            {
                _isCompleted = true;
                NotifyChanged();
            }
        }

        public string CaptureState() => _isCompleted ? "1" : "0";

        public void RestoreState(string state)
        {
            _isCompleted = state == "1";
        }

        public override string ToString()
        {
            return $"Enter area: {_areaDescription ?? _areaId}";
        }
    }
}
