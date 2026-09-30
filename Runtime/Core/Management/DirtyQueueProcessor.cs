#nullable enable
using System;
using System.Collections.Generic;

namespace DynamicBox.Quest.Core
{
    /// <summary>
    /// Manages the dirty queue for objectives that need evaluation.
    /// Processes objectives in batch and fires appropriate events based on evaluation results.
    /// Extracted from QuestManager to follow Single Responsibility Principle.
    /// </summary>
    internal sealed class DirtyQueueProcessor
    {
        /// <summary>Upper bound on drain passes per ProcessAll, guarding against conditions that re-dirty each other.</summary>
        internal const int MaxPassesPerProcess = 64;

        private readonly HashSet<(QuestState quest, ObjectiveState obj)> _dirtySet = new();
        private readonly List<(QuestState quest, ObjectiveState obj)> _processing = new();
        private readonly ObjectiveEvaluator _evaluator;

        public event Action<QuestState>? OnQuestCompleted;
        public event Action<QuestState>? OnQuestFailed;
        public event Action<ObjectiveState>? OnObjectiveStatusChanged;

        public DirtyQueueProcessor(ObjectiveEvaluator evaluator)
        {
            _evaluator = evaluator;
        }

        /// <summary>
        /// Marks an objective as dirty for evaluation in the next processing cycle.
        /// </summary>
        public void MarkDirty(QuestState quest, ObjectiveState obj)
        {
            _dirtySet.Add((quest, obj));
        }

        /// <summary>
        /// Processes all dirty objectives and fires appropriate events.
        /// Should be called once per frame.
        /// Evaluating an objective can mark more objectives dirty (completing one activates the next,
        /// whose condition may already be met on bind), so work is drained in passes until nothing new
        /// is dirty. A chain of already-met objectives therefore resolves in a single call.
        /// </summary>
        public void ProcessAll()
        {
            int pass = 0;
            while (_dirtySet.Count > 0)
            {
                // Two conditions that keep re-dirtying each other would otherwise loop forever.
                // Leave the remainder queued for the next frame instead of hanging.
                if (pass++ >= MaxPassesPerProcess)
                {
                    UnityEngine.Debug.LogError(
                        $"Quest dirty queue still had {_dirtySet.Count} objective(s) after {MaxPassesPerProcess} passes. " +
                        "Conditions may be re-triggering each other; the rest will be evaluated next frame.");
                    return;
                }

                // Snapshot and clear before evaluating so MarkDirty calls made during evaluation
                // land in the (now empty) set instead of modifying the collection being iterated.
                _processing.Clear();
                _processing.AddRange(_dirtySet);
                _dirtySet.Clear();

                foreach (var (quest, obj) in _processing)
                {
                    var result = _evaluator.Evaluate(quest, obj);

                    // Fire appropriate events based on evaluation result
                    switch (result)
                    {
                        case QuestEvaluationResult.ObjectiveCompleted:
                            SafeInvoke(OnObjectiveStatusChanged, obj);
                            _evaluator.ActivateReadyObjectives(quest);
                            break;

                        case QuestEvaluationResult.QuestCompleted:
                            SafeInvoke(OnObjectiveStatusChanged, obj);
                            SafeInvoke(OnQuestCompleted, quest);
                            break;

                        case QuestEvaluationResult.QuestFailed:
                            SafeInvoke(OnObjectiveStatusChanged, obj);
                            SafeInvoke(OnQuestFailed, quest);
                            break;
                    }
                }
            }

            _processing.Clear();
        }

        /// <summary>
        /// Gets the current number of dirty objectives pending evaluation.
        /// </summary>
        public int DirtyCount => _dirtySet.Count;

        /// <summary>
        /// Safely invokes an event, catching exceptions from individual subscribers to prevent breaking the event chain.
        /// </summary>
        private void SafeInvoke<T>(Action<T>? eventDelegate, T arg)
        {
            if (eventDelegate == null) return;

            foreach (Action<T> handler in eventDelegate.GetInvocationList())
            {
                try
                {
                    handler(arg);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"Exception in quest event subscriber: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }
}
