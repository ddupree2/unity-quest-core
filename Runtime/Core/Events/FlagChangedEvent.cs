#nullable enable

namespace DynamicBox.Quest.Core.Events
{
    /// <summary>
    /// Event published when a gameplay flag changes value.
    /// Immutable payload, raised through a ScriptableEvent asset.
    /// </summary>
    public sealed class FlagChangedEvent
    {
        /// <summary>
        /// Gets the unique identifier of the flag that changed.
        /// </summary>
        public string FlagId { get; }
        
        /// <summary>
        /// Gets the new value of the flag after the change.
        /// </summary>
        public bool NewValue { get; }
        
        /// <summary>
        /// Gets the previous value of the flag before the change.
        /// </summary>
        public bool OldValue { get; }

        /// <summary>
        /// Creates a new flag changed event.
        /// </summary>
        /// <param name="flagId">The unique identifier of the flag.</param>
        /// <param name="newValue">The new value of the flag.</param>
        /// <param name="oldValue">The previous value of the flag (default false).</param>
        public FlagChangedEvent(string flagId, bool newValue, bool oldValue = false)
        {
            FlagId = flagId;
            NewValue = newValue;
            OldValue = oldValue;
        }
    }
}
