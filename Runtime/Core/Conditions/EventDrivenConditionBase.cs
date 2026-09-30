#nullable enable
using System;
using StarStoneStudio.Scriptables;
using UnityEngine;

namespace DynamicBox.Quest.Core
{
    /// <summary>
    /// Base class for conditions driven by a ScriptableEvent asset. Registers with the event on Bind
    /// and unregisters on Unbind, so only the asset assigned to this condition is heard, not every
    /// event of the same payload type in the game.
    /// </summary>
    /// <typeparam name="TEvent">The payload type raised by the event asset.</typeparam>
    public abstract class EventDrivenConditionBase<TEvent> : IConditionInstance
    {
        private readonly ScriptableEvent<TEvent>? _scriptableEvent;
        private Action? _onChanged;
        private bool _isRegistered;

        public abstract bool IsMet { get; }

        /// <param name="scriptableEvent">
        /// The event asset to listen to. Null logs a warning on Bind, since it is almost always a
        /// missing Inspector assignment; the condition then only reacts to its own OnBind checks.
        /// </param>
        protected EventDrivenConditionBase(ScriptableEvent<TEvent>? scriptableEvent)
        {
            _scriptableEvent = scriptableEvent;
        }

        public void Bind(QuestContext context, Action onChanged)
        {
            _onChanged = onChanged;

            if (_scriptableEvent != null)
            {
                // Guard against double registration if Bind is called twice without Unbind
                if (!_isRegistered)
                {
                    _scriptableEvent.Register(OnEventReceived);
                    _isRegistered = true;
                }
            }
            else
            {
                Debug.LogWarning($"{GetType().Name} '{this}' has no event asset assigned and will not receive events.");
            }

            // Allow subclasses to perform additional initialization
            OnBind(context);
        }

        public void Unbind(QuestContext context)
        {
            if (_isRegistered && _scriptableEvent != null)
            {
                _scriptableEvent.Unregister(OnEventReceived);
                _isRegistered = false;
            }

            _onChanged = null;

            // Allow subclasses to perform cleanup
            OnUnbind(context);
        }

        /// <summary>
        /// Notifies listeners that the condition state has changed.
        /// Call this when IsMet changes value.
        /// </summary>
        protected void NotifyChanged()
        {
            _onChanged?.Invoke();
        }

        /// <summary>
        /// Called when the event asset is raised.
        /// Implement condition-specific logic here.
        /// </summary>
        protected abstract void HandleEvent(TEvent evt);

        /// <summary>
        /// Called after the condition is bound.
        /// Override to perform additional initialization.
        /// </summary>
        protected virtual void OnBind(QuestContext context) { }

        /// <summary>
        /// Called when the condition is unbound.
        /// Override to perform cleanup.
        /// </summary>
        protected virtual void OnUnbind(QuestContext context) { }

        private void OnEventReceived(TEvent evt)
        {
            HandleEvent(evt);
        }
    }
}
