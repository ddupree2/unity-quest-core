#nullable enable
using System.Collections.Generic;

namespace DynamicBox.Quest.Core
{
    /// <summary>
    /// Registry for quests. Tracks quests in progress plus the history of quests that ended.
    /// A quest is in at most one place: active, completed or failed. Restarting an ended quest
    /// takes it out of history, so the history always holds the latest outcome per quest.
    /// </summary>
    public sealed class QuestLog
    {
        private readonly List<QuestState> _active = new();
        private readonly List<QuestState> _completed = new();
        private readonly List<QuestState> _failed = new();

        /// <summary>
        /// Gets the read-only list of all quests currently in progress.
        /// </summary>
        public IReadOnlyList<QuestState> Active => _active;

        /// <summary>
        /// Gets the quests that ended in completion, with their final objective states.
        /// </summary>
        public IReadOnlyList<QuestState> Completed => _completed;

        /// <summary>
        /// Gets the quests that ended in failure, with their final objective states.
        /// </summary>
        public IReadOnlyList<QuestState> Failed => _failed;

        /// <summary>
        /// Starts tracking a new quest and sets its status to InProgress.
        /// Any previous outcome for the same quest is dropped from history.
        /// </summary>
        /// <param name="quest">The quest definition to start tracking.</param>
        /// <returns>The newly created quest state.</returns>
        public QuestState StartQuest(QuestAsset quest)
        {
            RemoveFromHistory(quest);

            var state = new QuestState(quest);
            state.SetStatus(QuestStatus.InProgress);
            _active.Add(state);
            return state;
        }

        /// <summary>
        /// Removes a quest from the active list without recording an outcome (used when a quest is stopped/abandoned).
        /// </summary>
        /// <param name="state">The quest state to remove from tracking.</param>
        public void RemoveQuest(QuestState state)
        {
            _active.Remove(state);
        }

        /// <summary>
        /// Moves an ended quest from the active list into history, based on its status.
        /// Quests that are not Completed or Failed are only removed from the active list.
        /// </summary>
        /// <param name="state">The quest state that just completed or failed.</param>
        public void ArchiveQuest(QuestState state)
        {
            _active.Remove(state);
            RemoveFromHistory(state.Definition);

            switch (state.Status)
            {
                case QuestStatus.Completed:
                    _completed.Add(state);
                    break;
                case QuestStatus.Failed:
                    _failed.Add(state);
                    break;
            }
        }

        /// <summary>
        /// Adds a quest restored from a save: InProgress quests become active, Completed/Failed go to history.
        /// </summary>
        /// <param name="state">The restored quest state.</param>
        public void AddRestored(QuestState state)
        {
            if (state.Status.IsTerminal())
            {
                ArchiveQuest(state);
            }
            else
            {
                RemoveFromHistory(state.Definition);
                _active.Add(state);
            }
        }

        /// <summary>
        /// Forgets all active quests and history. Callers must unbind active quests first.
        /// </summary>
        public void Clear()
        {
            _active.Clear();
            _completed.Clear();
            _failed.Clear();
        }

        public bool IsActive(QuestAsset quest) => Find(_active, quest) != null;
        public bool IsCompleted(QuestAsset quest) => Find(_completed, quest) != null;
        public bool IsFailed(QuestAsset quest) => Find(_failed, quest) != null;

        private void RemoveFromHistory(QuestAsset quest)
        {
            _completed.RemoveAll(q => q.Definition == quest);
            _failed.RemoveAll(q => q.Definition == quest);
        }

        private static QuestState? Find(List<QuestState> list, QuestAsset quest)
        {
            foreach (var state in list)
            {
                if (state.Definition == quest)
                    return state;
            }
            return null;
        }
    }
}
