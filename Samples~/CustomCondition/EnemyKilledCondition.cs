using DynamicBox.Quest.Core;
using StarStoneStudio.Scriptables;
using UnityEngine;

namespace DynamicBox.Quest.Samples
{
    /// <summary>
    /// Example of creating a custom condition for enemy kills.
    /// Shows the minimal code needed to extend the quest system.
    /// </summary>
    ///
    // 1. Create the asset (designer-facing)
    [CreateAssetMenu(menuName = "Quest Samples/Conditions/Enemy Killed")]
    public class EnemyKilledCondition : ConditionAsset
    {
        [Tooltip("Event asset your game raises when an enemy dies.")]
        [SerializeField] private EnemyKilledScriptableEvent enemyKilledEvent;
        [SerializeField] private string enemyType = "Goblin";
        [SerializeField] private int requiredKills = 3;

        public override IConditionInstance CreateInstance()
        {
            return new EnemyKilledConditionInstance(enemyKilledEvent, enemyType, requiredKills);
        }
    }

    // 2. Create the instance (runtime logic). EventDrivenConditionBase registers with the
    // event asset on Bind and unregisters on Unbind.
    public class EnemyKilledConditionInstance : EventDrivenConditionBase<EnemyKilledEvent>, IProgressReportingCondition
    {
        private readonly string _enemyType;
        private readonly int _requiredKills;
        private int _currentKills;

        public EnemyKilledConditionInstance(ScriptableEvent<EnemyKilledEvent> enemyKilledEvent, string enemyType, int requiredKills)
            : base(enemyKilledEvent)
        {
            _enemyType = enemyType;
            _requiredKills = requiredKills;
        }

        public override bool IsMet => _currentKills >= _requiredKills;

        public float Progress => _requiredKills > 0 ? Mathf.Clamp01((float)_currentKills / _requiredKills) : 1f;
        public string ProgressDescription => $"{_currentKills}/{_requiredKills} {_enemyType}s defeated";

        protected override void HandleEvent(EnemyKilledEvent evt)
        {
            if (evt.EnemyType == _enemyType && _currentKills < _requiredKills)
            {
                _currentKills++;
                NotifyChanged();

                Debug.Log($"Killed {_enemyType}: {_currentKills}/{_requiredKills}");
            }
        }
    }

    // 3. Define your event payload and its event asset type
    public sealed class EnemyKilledEvent
    {
        public string EnemyType { get; }

        public EnemyKilledEvent(string enemyType)
        {
            EnemyType = enemyType;
        }
    }

    [CreateAssetMenu(menuName = "Quest Samples/Events/Enemy Killed Event")]
    public sealed class EnemyKilledScriptableEvent : ScriptableEvent<EnemyKilledEvent>
    {
    }
}
