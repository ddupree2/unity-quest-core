using System;
using System.Collections.Generic;
using System.Reflection;
using DynamicBox.Quest.Core;
using DynamicBox.Quest.Core.Conditions;
using DynamicBox.Quest.Core.Events;
using UnityEngine;

namespace DynamicBox.Quest.Tests
{
    /// <summary>
    /// Tests for QuestManager.CaptureSaveData / RestoreSaveData / ClearAll and saveable condition progress.
    /// </summary>
    public static class QuestSaveRestoreTests
    {
        public static void RunAllSaveRestoreTests()
        {
            Debug.Log("\n=== Running Quest Save/Restore Tests ===");
            TestCounterProgressSurvivesRoundTrip();
            TestHistorySurvivesRoundTrip();
            TestRestoredConditionAlreadyMetCompletes();
            TestRestoreReplacesExistingQuests();
            TestUnknownQuestIsSkipped();
            TestPrerequisitesStayLockedAfterRestore();
            TestTimeAndGroupConditionStateRoundTrip();
            TestClearAllForgetsEverything();
            Debug.Log("✓ All quest save/restore tests passed!");
        }

        private static void TestCounterProgressSurvivesRoundTrip()
        {
            Debug.Log("\n[TEST] Counter Progress Survives Round Trip");

            var source = CreateQuestManager();
            var target = CreateQuestManager();
            try
            {
                var quest = CreateItemQuest("sr_counter", "sr_gem", 8);
                source.StartQuest(quest);
                for (int i = 0; i < 3; i++)
                    TestEvents.Items.Raise(new ItemCollectedEvent("sr_gem", 1));
                source.ProcessPendingEvaluations();

                var saveData = source.CaptureSaveData();
                source.ClearAll(); // stop the source listening to the shared test event

                target.RestoreSaveData(saveData, Resolver(quest));

                if (target.ActiveQuests.Count != 1)
                    throw new Exception($"Expected 1 restored active quest, got {target.ActiveQuests.Count}");

                var restored = target.ActiveQuests[0];
                var counter = GetItemCondition(restored, "obj1");
                if (counter.CurrentCount != 3)
                    throw new Exception($"Expected restored count 3/8, got {counter.CurrentCount}/8");
                if (restored.Objectives["obj1"].Status != ObjectiveStatus.InProgress)
                    throw new Exception("Restored objective should still be in progress");

                // The restored quest keeps listening and completes
                for (int i = 0; i < 5; i++)
                    TestEvents.Items.Raise(new ItemCollectedEvent("sr_gem", 1));
                target.ProcessPendingEvaluations();

                if (!target.IsCompleted(quest))
                    throw new Exception("Restored quest should complete once the remaining items are collected");

                Debug.Log("✓ Counter progress (3/8) survives a save/load and keeps progressing");
            }
            finally
            {
                Cleanup(source);
                Cleanup(target);
            }
        }

        private static void TestHistorySurvivesRoundTrip()
        {
            Debug.Log("\n[TEST] History Survives Round Trip");

            var source = CreateQuestManager();
            var target = CreateQuestManager();
            try
            {
                var completed = CreateMockQuest("sr_history_completed");
                var failed = CreateMockQuest("sr_history_failed");
                source.CompleteQuest(source.StartQuest(completed));
                source.FailQuest(source.StartQuest(failed));

                target.RestoreSaveData(source.CaptureSaveData(), Resolver(completed, failed));

                if (!target.IsCompleted(completed))
                    throw new Exception("Completed quest should be restored into history");
                if (!target.IsFailed(failed))
                    throw new Exception("Failed quest should be restored into history");
                if (target.ActiveQuests.Count != 0)
                    throw new Exception("History quests should not become active");

                Debug.Log("✓ Completed and failed history survives a save/load");
            }
            finally
            {
                Cleanup(source);
                Cleanup(target);
            }
        }

        private static void TestRestoredConditionAlreadyMetCompletes()
        {
            Debug.Log("\n[TEST] Restored Condition Already Met Completes");

            var source = CreateQuestManager();
            var target = CreateQuestManager();
            try
            {
                var quest = CreateItemQuest("sr_already_met", "sr_orb", 2);
                source.StartQuest(quest);
                TestEvents.Items.Raise(new ItemCollectedEvent("sr_orb", 2));

                // Saved before the pending evaluation ran: the objective is still InProgress at 2/2
                var saveData = source.CaptureSaveData();
                source.ClearAll();

                int completedEvents = 0;
                target.OnQuestCompleted += q => completedEvents++;
                target.RestoreSaveData(saveData, Resolver(quest));
                target.ProcessPendingEvaluations();

                if (!target.IsCompleted(quest) || completedEvents != 1)
                    throw new Exception("A restored condition that is already met should complete on the next evaluation");

                Debug.Log("✓ Restored conditions that are already met complete");
            }
            finally
            {
                Cleanup(source);
                Cleanup(target);
            }
        }

        private static void TestRestoreReplacesExistingQuests()
        {
            Debug.Log("\n[TEST] Restore Replaces Existing Quests");

            var source = CreateQuestManager();
            var target = CreateQuestManager();
            try
            {
                var savedQuest = CreateMockQuest("sr_replace_saved");
                source.StartQuest(savedQuest);
                var saveData = source.CaptureSaveData();

                // The target already has other progress that the load must discard
                var staleActive = CreateItemQuest("sr_replace_stale", "sr_stale_item", 1);
                var staleHistory = CreateMockQuest("sr_replace_stale_history");
                target.StartQuest(staleActive);
                target.CompleteQuest(target.StartQuest(staleHistory));

                target.RestoreSaveData(saveData, Resolver(savedQuest, staleActive, staleHistory));

                if (!target.IsActive(savedQuest))
                    throw new Exception("The saved quest should be active after restoring");
                if (target.IsActive(staleActive) || target.IsCompleted(staleHistory))
                    throw new Exception("Quests that weren't in the save should be gone after restoring");

                // The discarded quest must no longer listen to its event
                int conditionChanges = 0;
                target.OnConditionStatusChanged += (o, c, met) => conditionChanges++;
                TestEvents.Items.Raise(new ItemCollectedEvent("sr_stale_item", 1));
                if (conditionChanges != 0)
                    throw new Exception("Quests discarded by a restore should be unbound");

                Debug.Log("✓ Restore replaces existing quests and unbinds them");
            }
            finally
            {
                Cleanup(source);
                Cleanup(target);
            }
        }

        private static void TestUnknownQuestIsSkipped()
        {
            Debug.Log("\n[TEST] Unknown Quest Is Skipped");

            var source = CreateQuestManager();
            var target = CreateQuestManager();
            try
            {
                var known = CreateMockQuest("sr_known");
                var unknown = CreateMockQuest("sr_unknown");
                source.StartQuest(known);
                source.StartQuest(unknown);

                // The resolver only knows one of the two quests (e.g. a quest removed from the game)
                target.RestoreSaveData(source.CaptureSaveData(), Resolver(known));

                if (!target.IsActive(known) || target.ActiveQuests.Count != 1)
                    throw new Exception("Only the resolvable quest should be restored");

                Debug.Log("✓ Unknown quests are skipped (warning logged)");
            }
            finally
            {
                Cleanup(source);
                Cleanup(target);
            }
        }

        private static void TestPrerequisitesStayLockedAfterRestore()
        {
            Debug.Log("\n[TEST] Prerequisites Stay Locked After Restore");

            var source = CreateQuestManager();
            var target = CreateQuestManager();
            try
            {
                var obj1 = new ObjectiveBuilder().WithObjectiveId("obj1")
                    .WithCompletionCondition(ScriptableObject.CreateInstance<MockConditionAsset>()).Build();
                var obj2 = new ObjectiveBuilder().WithObjectiveId("obj2")
                    .WithCompletionCondition(ScriptableObject.CreateInstance<MockConditionAsset>())
                    .AddPrerequisite(obj1).Build();
                var quest = new QuestBuilder().WithQuestId("sr_prereq").AddObjective(obj1).AddObjective(obj2).Build();

                source.StartQuest(quest);
                target.RestoreSaveData(source.CaptureSaveData(), Resolver(quest));

                var restored = target.ActiveQuests[0];
                if (restored.Objectives["obj1"].Status != ObjectiveStatus.InProgress)
                    throw new Exception("obj1 should be in progress after restoring");
                if (restored.Objectives["obj2"].Status != ObjectiveStatus.NotStarted)
                    throw new Exception("obj2 should stay locked behind its prerequisite");

                // Completing obj1 after the restore unlocks obj2
                GetMock(restored, "obj1").SetMet(true);
                target.ProcessPendingEvaluations();
                if (restored.Objectives["obj2"].Status != ObjectiveStatus.InProgress)
                    throw new Exception("obj2 should activate once obj1 completes after the restore");

                Debug.Log("✓ Restored objectives keep prerequisite order and keep progressing");
            }
            finally
            {
                Cleanup(source);
                Cleanup(target);
            }
        }

        private static void TestTimeAndGroupConditionStateRoundTrip()
        {
            Debug.Log("\n[TEST] Time And Group Condition State Round Trip");

            var time = new TimeElapsedConditionInstance(10f);
            time.RestoreState("4.5");
            var restoredTime = new TimeElapsedConditionInstance(10f);
            restoredTime.RestoreState(time.CaptureState());
            restoredTime.Bind(new QuestContext(), () => { });
            if (Mathf.Abs(restoredTime.GetRemainingTime() - 5.5f) > 0.001f)
                throw new Exception($"Expected 5.5s remaining after restore and bind, got {restoredTime.GetRemainingTime()}");

            var group = new ConditionGroupInstance(ConditionOperator.And, new List<IConditionInstance>
            {
                new ItemCollectedConditionInstance(TestEvents.Items, "sr_group_a", 4),
                new MockConditionInstance(), // not saveable: gets an empty entry
                new ItemCollectedConditionInstance(TestEvents.Items, "sr_group_b", 4),
            });
            ((ISaveableCondition)((List<IConditionInstance>)GetChildren(group))[0]).RestoreState("2");
            ((ISaveableCondition)((List<IConditionInstance>)GetChildren(group))[2]).RestoreState("4");

            var restoredGroup = new ConditionGroupInstance(ConditionOperator.And, new List<IConditionInstance>
            {
                new ItemCollectedConditionInstance(TestEvents.Items, "sr_group_a", 4),
                new MockConditionInstance(),
                new ItemCollectedConditionInstance(TestEvents.Items, "sr_group_b", 4),
            });
            restoredGroup.RestoreState(group.CaptureState());

            var children = (List<IConditionInstance>)GetChildren(restoredGroup);
            if (((ItemCollectedConditionInstance)children[0]).CurrentCount != 2 ||
                ((ItemCollectedConditionInstance)children[2]).CurrentCount != 4)
                throw new Exception("Group should restore each saveable child's progress by position");

            Debug.Log("✓ Time elapsed and condition group progress round-trip");
        }

        private static void TestClearAllForgetsEverything()
        {
            Debug.Log("\n[TEST] ClearAll Forgets Everything");

            var questManager = CreateQuestManager();
            try
            {
                var active = CreateMockQuest("sr_clear_active");
                var completed = CreateMockQuest("sr_clear_completed");
                questManager.StartQuest(active);
                questManager.CompleteQuest(questManager.StartQuest(completed));

                bool restoredEvent = false;
                questManager.OnQuestLogRestored += () => restoredEvent = true;
                questManager.ClearAll();

                if (questManager.ActiveQuests.Count != 0 || questManager.CompletedQuests.Count != 0)
                    throw new Exception("ClearAll should forget active quests and history");
                if (!restoredEvent)
                    throw new Exception("ClearAll should raise OnQuestLogRestored so UI can rebuild");

                Debug.Log("✓ ClearAll forgets active quests and history");
            }
            finally
            {
                Cleanup(questManager);
            }
        }

        private static QuestAsset CreateMockQuest(string questId)
        {
            var objective = new ObjectiveBuilder()
                .WithObjectiveId("obj1")
                .WithCompletionCondition(ScriptableObject.CreateInstance<MockConditionAsset>())
                .Build();
            return new QuestBuilder().WithQuestId(questId).AddObjective(objective).Build();
        }

        private static QuestAsset CreateItemQuest(string questId, string itemId, int requiredCount)
        {
            var condition = TestEvents.Wire(ScriptableObject.CreateInstance<ItemCollectedConditionAsset>());
            SetField(typeof(ConditionAsset), condition, "conditionId", itemId);
            SetField(typeof(ItemCollectedConditionAsset), condition, "requiredCount", requiredCount);

            var objective = new ObjectiveBuilder().WithObjectiveId("obj1").WithCompletionCondition(condition).Build();
            return new QuestBuilder().WithQuestId(questId).AddObjective(objective).Build();
        }

        private static Func<string, QuestAsset> Resolver(params QuestAsset[] quests)
        {
            var map = new Dictionary<string, QuestAsset>();
            foreach (var quest in quests)
                map[quest.QuestId] = quest;
            return id => map.TryGetValue(id, out var quest) ? quest : null;
        }

        private static ItemCollectedConditionInstance GetItemCondition(QuestState state, string objectiveId)
        {
            return (ItemCollectedConditionInstance)state.Objectives[objectiveId].CompletionInstance;
        }

        private static MockConditionInstance GetMock(QuestState state, string objectiveId)
        {
            return (MockConditionInstance)state.Objectives[objectiveId].CompletionInstance;
        }

        private static object GetChildren(ConditionGroupInstance group)
        {
            return typeof(ConditionGroupInstance).GetField("_children", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(group);
        }

        private static void SetField(Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new MissingFieldException(type.Name, fieldName);
            field.SetValue(target, value);
        }

        private static QuestManager CreateQuestManager()
        {
            var gameObject = new GameObject("TestQuestManager");
            gameObject.SetActive(false);

            var questManager = gameObject.AddComponent<QuestManager>();
            var playerRef = new GameObject("TestPlayerRef").AddComponent<QuestPlayerRef>();

            typeof(QuestManager).GetField("playerRef", BindingFlags.NonPublic | BindingFlags.Instance)?
                .SetValue(questManager, playerRef);

            gameObject.SetActive(true);
            return questManager;
        }

        private static void Cleanup(QuestManager questManager)
        {
            if (questManager == null)
                return;

            // Unbind from the shared test event assets before destroying
            questManager.ClearAll();

            var playerRef = typeof(QuestManager).GetField("playerRef", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(questManager) as QuestPlayerRef;
            if (playerRef != null)
                UnityEngine.Object.DestroyImmediate(playerRef.gameObject);

            UnityEngine.Object.DestroyImmediate(questManager.gameObject);
        }
    }
}
