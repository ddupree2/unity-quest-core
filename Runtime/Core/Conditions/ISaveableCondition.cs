#nullable enable

namespace DynamicBox.Quest.Core
{
    /// <summary>
    /// Optional interface for conditions whose progress must survive a save/load (e.g. counters: 5/8
    /// collected). Conditions that can rebuild their state on Bind (such as flag conditions, which
    /// re-read the flag service) don't need it.
    /// The state is a string so it fits any serializer; its format is private to the condition.
    /// </summary>
    public interface ISaveableCondition
    {
        /// <summary>
        /// Captures the condition's progress. Called when the quest is snapshotted.
        /// </summary>
        string CaptureState();

        /// <summary>
        /// Restores progress captured by <see cref="CaptureState"/>. Called on a fresh instance
        /// before it is bound, so Bind must not reset restored progress.
        /// </summary>
        void RestoreState(string state);
    }
}
