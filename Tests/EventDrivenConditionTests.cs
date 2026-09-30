#nullable enable
using System;
using DynamicBox.Quest.Core;
using StarStoneStudio.Scriptables;
using UnityEngine;

namespace DynamicBox.Quest.Tests
{
    /// <summary>
    /// Tests for EventDrivenConditionBase abstract class behavior.
    /// Validates base class functionality using concrete implementations.
    /// </summary>
    public static class EventDrivenConditionTests
    {
        public static void RunAllEventDrivenTests()
        {
            Debug.Log("\n=== Running Event Driven Condition Tests ===");
            TestBindSubscribesToEvents();
            TestUnbindUnsubscribesFromEvents();
            TestHandleEventCalledOnEventRaised();
            TestNotifyChangedInvokesCallback();
            TestMultipleBindUnbindCycles();
            TestOnBindOnUnbindLifecycle();
            TestOnlyAssignedEventAssetIsHeard();
            TestNullEventAssetDoesNotThrow();
            Debug.Log("✓ All event driven condition tests passed!");
        }

        private static void TestBindSubscribesToEvents()
        {
            Debug.Log("\n[TEST] Bind Subscribes To Events");

            // Arrange
            var context = new QuestContext();
            var testEvent = ScriptableObject.CreateInstance<TestScriptableEvent>();
            var condition = new TestEventDrivenCondition(testEvent);
            bool callbackInvoked = false;

            // Act
            condition.Bind(context, () => callbackInvoked = true);
            testEvent.Raise(new TestGameEvent("test"));

            // Assert - Event should be received and processed
            if (!condition.EventReceived)
                throw new Exception("Event was not received after Bind");
            if (condition.ReceivedEventData != "test")
                throw new Exception($"Expected event data 'test', got '{condition.ReceivedEventData}'");
            
            // Note: Callback is only invoked when condition calls NotifyChanged()
            // This test verifies event subscription works, not callback invocation
            if (callbackInvoked) { } // Used to suppress warning

            Debug.Log("✓ Bind subscribes to events correctly");
        }

        private static void TestUnbindUnsubscribesFromEvents()
        {
            Debug.Log("\n[TEST] Unbind Unsubscribes From Events");

            // Arrange
            var context = new QuestContext();
            var testEvent = ScriptableObject.CreateInstance<TestScriptableEvent>();
            var condition = new TestEventDrivenCondition(testEvent);
            bool callbackInvoked = false;

            // Act
            condition.Bind(context, () => callbackInvoked = true);
            if (!callbackInvoked) { } // Suppress unused warning - callback is tested implicitly
            condition.Unbind(context);
            
            // Reset state and trigger event
            condition.Reset();
            testEvent.Raise(new TestGameEvent("after-unbind"));

            // Assert
            if (condition.EventReceived)
                throw new Exception("Event was received after Unbind");

            Debug.Log("✓ Unbind unsubscribes from events correctly");
        }

        private static void TestHandleEventCalledOnEventRaised()
        {
            Debug.Log("\n[TEST] HandleEvent Called On Event Raised");

            // Arrange
            var context = new QuestContext();
            var testEvent = ScriptableObject.CreateInstance<TestScriptableEvent>();
            var condition = new TestEventDrivenCondition(testEvent);

            // Act
            condition.Bind(context, () => { });
            testEvent.Raise(new TestGameEvent("data1"));
            testEvent.Raise(new TestGameEvent("data2"));

            // Assert
            if (!condition.EventReceived)
                throw new Exception("HandleEvent was not called");
            if (condition.HandleEventCallCount != 2)
                throw new Exception($"Expected HandleEvent called 2 times, got {condition.HandleEventCallCount}");
            if (condition.ReceivedEventData != "data2")
                throw new Exception($"Expected last event data 'data2', got '{condition.ReceivedEventData}'");

            Debug.Log("✓ HandleEvent called correctly on event raised");
        }

        private static void TestNotifyChangedInvokesCallback()
        {
            Debug.Log("\n[TEST] NotifyChanged Invokes Callback");

            // Arrange
            var context = new QuestContext();
            var testEvent = ScriptableObject.CreateInstance<TestScriptableEvent>();
            var condition = new TestEventDrivenCondition(testEvent);
            int callbackCount = 0;

            // Act
            condition.Bind(context, () => callbackCount++);
            condition.TriggerNotifyChanged(); // Direct call to NotifyChanged
            condition.TriggerNotifyChanged();

            // Assert
            if (callbackCount != 2)
                throw new Exception($"Expected callback invoked 2 times, got {callbackCount}");

            Debug.Log("✓ NotifyChanged invokes callback correctly");
        }

        private static void TestMultipleBindUnbindCycles()
        {
            Debug.Log("\n[TEST] Multiple Bind/Unbind Cycles");

            // Arrange
            var context = new QuestContext();
            var testEvent = ScriptableObject.CreateInstance<TestScriptableEvent>();
            var condition = new TestEventDrivenCondition(testEvent);

            // Act & Assert - Cycle 1
            condition.Bind(context, () => { });
            testEvent.Raise(new TestGameEvent("cycle1"));
            if (!condition.EventReceived || condition.ReceivedEventData != "cycle1")
                throw new Exception("Cycle 1 failed");

            condition.Unbind(context);
            condition.Reset();

            // Act & Assert - Cycle 2
            condition.Bind(context, () => { });
            testEvent.Raise(new TestGameEvent("cycle2"));
            if (!condition.EventReceived || condition.ReceivedEventData != "cycle2")
                throw new Exception("Cycle 2 failed");

            condition.Unbind(context);
            condition.Reset();

            // Act & Assert - Cycle 3
            condition.Bind(context, () => { });
            testEvent.Raise(new TestGameEvent("cycle3"));
            if (!condition.EventReceived || condition.ReceivedEventData != "cycle3")
                throw new Exception("Cycle 3 failed");

            Debug.Log("✓ Multiple bind/unbind cycles work correctly");
        }

        private static void TestOnBindOnUnbindLifecycle()
        {
            Debug.Log("\n[TEST] OnBind/OnUnbind Lifecycle Hooks");

            // Arrange
            var context = new QuestContext();
            var testEvent = ScriptableObject.CreateInstance<TestScriptableEvent>();
            var condition = new TestEventDrivenCondition(testEvent);

            // Act
            condition.Bind(context, () => { });

            // Assert
            if (!condition.OnBindCalled)
                throw new Exception("OnBind was not called during Bind");
            if (condition.OnUnbindCalled)
                throw new Exception("OnUnbind should not be called before Unbind");

            // Act
            condition.Unbind(context);

            // Assert
            if (!condition.OnUnbindCalled)
                throw new Exception("OnUnbind was not called during Unbind");

            Debug.Log("✓ OnBind/OnUnbind lifecycle hooks work correctly");
        }

        private static void TestOnlyAssignedEventAssetIsHeard()
        {
            Debug.Log("\n[TEST] Only Assigned Event Asset Is Heard");

            // Arrange - two assets of the same payload type
            var context = new QuestContext();
            var assignedEvent = ScriptableObject.CreateInstance<TestScriptableEvent>();
            var otherEvent = ScriptableObject.CreateInstance<TestScriptableEvent>();
            var condition = new TestEventDrivenCondition(assignedEvent);

            // Act
            condition.Bind(context, () => { });
            otherEvent.Raise(new TestGameEvent("other"));

            // Assert
            if (condition.EventReceived)
                throw new Exception("Condition heard an event asset it was not assigned");

            assignedEvent.Raise(new TestGameEvent("assigned"));
            if (condition.ReceivedEventData != "assigned")
                throw new Exception("Condition did not hear its assigned event asset");

            condition.Unbind(context);
            Debug.Log("✓ Only the assigned event asset is heard");
        }

        private static void TestNullEventAssetDoesNotThrow()
        {
            Debug.Log("\n[TEST] Null Event Asset Does Not Throw");

            // A missing Inspector assignment should warn, not break quest binding
            var context = new QuestContext();
            var condition = new TestEventDrivenCondition(null);

            condition.Bind(context, () => { });
            if (!condition.OnBindCalled)
                throw new Exception("OnBind should still run without an event asset");
            condition.Unbind(context);

            Debug.Log("✓ Null event asset binds and unbinds without throwing");
        }

        // Test implementation of EventDrivenConditionBase
        private class TestEventDrivenCondition : EventDrivenConditionBase<TestGameEvent>
        {
            public bool EventReceived { get; private set; }
            public string? ReceivedEventData { get; private set; }
            public int HandleEventCallCount { get; private set; }
            public bool OnBindCalled { get; private set; }
            public bool OnUnbindCalled { get; private set; }
            private bool _isMet;

            public TestEventDrivenCondition(ScriptableEvent<TestGameEvent>? testEvent) : base(testEvent) { }

            public override bool IsMet => _isMet;

            protected override void HandleEvent(TestGameEvent evt)
            {
                EventReceived = true;
                ReceivedEventData = evt.Data;
                HandleEventCallCount++;
            }

            protected override void OnBind(QuestContext context)
            {
                OnBindCalled = true;
            }

            protected override void OnUnbind(QuestContext context)
            {
                OnUnbindCalled = true;
            }

            public void TriggerNotifyChanged()
            {
                _isMet = true;
                NotifyChanged();
            }

            public void Reset()
            {
                EventReceived = false;
                ReceivedEventData = null;
                HandleEventCallCount = 0;
            }
        }
    }

    // Test event payload and its event asset type
    internal sealed class TestGameEvent
    {
        public string Data { get; }

        public TestGameEvent(string data)
        {
            Data = data;
        }
    }

    internal sealed class TestScriptableEvent : ScriptableEvent<TestGameEvent>
    {
    }
}
