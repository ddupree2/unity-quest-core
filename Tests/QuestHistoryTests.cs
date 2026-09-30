using System;
using System.Linq;
using System.Reflection;
using DynamicBox.Quest.Core;
using DynamicBox.Quest.Core.Conditions;
using DynamicBox.Quest.Core.Events;
using UnityEngine;

namespace DynamicBox.Quest.Tests
{
    /// <summary>
    /// Tests for completed/failed quest history on QuestLog / QuestManager.
    /// </summary>
    public static class QuestHistoryTests
    {
        public static void RunAllHistoryTests()
        {
            Debug.Log("\n=== Running Quest History Tests ===");
            TestAutoCompletedQuestIsRecorded();
            TestAutoFailedQuestIsRecordedAndUnbound();
            TestManualCompleteAndFailAreRecorded();
            TestStoppedQuestIsNotRecorded();
            TestRestartClearsHistoryAndLatestOutcomeWins();
            TestCaptureSaveDataIncludesHistory();
            Debug.Log("✓ All quest history tests passed!");
        }

        private static void TestAutoCompletedQuestIsRecorded()
        {
            Debug.Log("\n[TEST] Auto-Completed Quest Is Recorded");

            var questManager = CreateQuestManager();
            try
            {
                var quest = CreateMockQuest("history_complete");
                var state = questManager.StartQuest(quest);

                GetCompletion(state, "obj1").SetMet(true);
                questManager.ProcessPendingEvaluations();

                if (!questManager.IsCompleted(quest))
                    throw new Exception("Completed quest should be reported by IsCompleted");
                if (questManager.IsActive(quest) || questManager.IsFailed(quest))
                    throw new Exception("Completed quest should not be active or failed");
                if (!questManager.CompletedQuests.Contains(state))
                    throw new Exception("CompletedQuests should hold the quest's final state");
                if (state.Objectives["obj1"].Status != ObjectiveStatus.Completed)
                    throw new Exception("History should keep the final objective statuses");

                Debug.Log("✓ Auto-completed quest is recorded");
            }
            finally
            {
                Cleanup(questManager);
            }
        }

        private static void TestAutoFailedQuestIsRecordedAndUnbound()
        {
            Debug.Log("\n[TEST] Auto-Failed Quest Is Recorded And Unbound");

            var questManager = CreateQuestManager();
            try
            {
                // obj1 can fail; obj2 listens to item events and must stop listening once the quest fails
                var obj1 = new ObjectiveBuilder()
                    .WithObjectiveId("obj1")
                    .WithCompletionCondition(ScriptableObject.CreateInstance<MockConditionAsset>())
                    .WithFailCondition(ScriptableObject.CreateInstance<MockConditionAsset>())
                    .Build();
                var itemCondition = TestEvents.Wire(ScriptableObject.CreateInstance<ItemCollectedConditionAsset>());
                SetConditionId(itemCondition, "history_fail_item");
                var obj2 = new ObjectiveBuilder()
                    .WithObjectiveId("obj2")
                    .WithCompletionCondition(itemCondition)
                    .Build();
                var quest = new QuestBuilder().WithQuestId("history_fail").AddObjective(obj1).AddObjective(obj2).Build();

                var state = questManager.StartQuest(quest);
                ((MockConditionInstance)state.Objectives["obj1"].FailInstance).SetMet(true);
                questManager.ProcessPendingEvaluations();

                if (!questManager.IsFailed(quest))
                    throw new Exception("Failed quest should be reported by IsFailed");
                if (questManager.IsActive(quest) || questManager.IsCompleted(quest))
                    throw new Exception("Failed quest should not be active or completed");
                if (!questManager.FailedQuests.Contains(state))
                    throw new Exception("FailedQuests should hold the quest's final state");

                // The failed quest's other objectives must be unbound
                int conditionChanges = 0;
                questManager.OnConditionStatusChanged += (o, c, met) => conditionChanges++;
                TestEvents.Items.Raise(new ItemCollectedEvent("history_fail_item", 1));
                if (conditionChanges != 0)
                    throw new Exception("Objectives of a failed quest should no longer receive events");

                Debug.Log("✓ Auto-failed quest is recorded and fully unbound");
            }
            finally
            {
                Cleanup(questManager);
            }
        }

        private static void TestManualCompleteAndFailAreRecorded()
        {
            Debug.Log("\n[TEST] Manual Complete And Fail Are Recorded");

            var questManager = CreateQuestManager();
            try
            {
                var completeQuest = CreateMockQuest("history_manual_complete");
                var failQuest = CreateMockQuest("history_manual_fail");

                questManager.CompleteQuest(questManager.StartQuest(completeQuest));
                questManager.FailQuest(questManager.StartQuest(failQuest));

                if (!questManager.IsCompleted(completeQuest))
                    throw new Exception("CompleteQuest should record the quest as completed");
                if (!questManager.IsFailed(failQuest))
                    throw new Exception("FailQuest should record the quest as failed");
                if (questManager.ActiveQuests.Count != 0)
                    throw new Exception("Manually ended quests should leave the active list");

                Debug.Log("✓ Manual complete and fail are recorded");
            }
            finally
            {
                Cleanup(questManager);
            }
        }

        private static void TestStoppedQuestIsNotRecorded()
        {
            Debug.Log("\n[TEST] Stopped Quest Is Not Recorded");

            var questManager = CreateQuestManager();
            try
            {
                var quest = CreateMockQuest("history_stopped");
                questManager.StartQuest(quest);

                if (!questManager.StopQuest(quest))
                    throw new Exception("StopQuest should find the active quest");

                if (questManager.IsActive(quest) || questManager.IsCompleted(quest) || questManager.IsFailed(quest))
                    throw new Exception("A stopped quest should be in neither the active list nor history");

                Debug.Log("✓ Stopped quest is not recorded");
            }
            finally
            {
                Cleanup(questManager);
            }
        }

        private static void TestRestartClearsHistoryAndLatestOutcomeWins()
        {
            Debug.Log("\n[TEST] Restart Clears History And Latest Outcome Wins");

            var questManager = CreateQuestManager();
            try
            {
                var quest = CreateMockQuest("history_restart");

                questManager.FailQuest(questManager.StartQuest(quest));
                if (!questManager.IsFailed(quest))
                    throw new Exception("Quest should be failed after the first attempt");

                var retry = questManager.StartQuest(quest);
                if (questManager.IsFailed(quest) || !questManager.IsActive(quest))
                    throw new Exception("Restarting should take the quest out of history and make it active");

                GetCompletion(retry, "obj1").SetMet(true);
                questManager.ProcessPendingEvaluations();

                if (!questManager.IsCompleted(quest) || questManager.IsFailed(quest))
                    throw new Exception("Only the latest outcome (completed) should be recorded");
                if (questManager.CompletedQuests.Count != 1 || questManager.FailedQuests.Count != 0)
                    throw new Exception($"Expected 1 completed / 0 failed, got {questManager.CompletedQuests.Count} / {questManager.FailedQuests.Count}");
                if (questManager.CompletedQuests[0] != retry)
                    throw new Exception("History should hold the state from the latest attempt");

                Debug.Log("✓ Restart clears history and the latest outcome wins");
            }
            finally
            {
                Cleanup(questManager);
            }
        }

        private static void TestCaptureSaveDataIncludesHistory()
        {
            Debug.Log("\n[TEST] CaptureSaveData Includes History");

            var questManager = CreateQuestManager();
            try
            {
                var activeQuest = CreateMockQuest("history_save_active");
                var completedQuest = CreateMockQuest("history_save_completed");
                var failedQuest = CreateMockQuest("history_save_failed");

                questManager.StartQuest(activeQuest);
                questManager.CompleteQuest(questManager.StartQuest(completedQuest));
                questManager.FailQuest(questManager.StartQuest(failedQuest));

                var saveData = questManager.CaptureSaveData("slot1");

                if (saveData.Quests.Count != 3)
                    throw new Exception($"Expected 3 snapshots (active + history), got {saveData.Quests.Count}");
                if (saveData.FindQuest("history_save_active")?.Status != QuestStatus.InProgress)
                    throw new Exception("Active quest snapshot should be InProgress");
                if (saveData.FindQuest("history_save_completed")?.Status != QuestStatus.Completed)
                    throw new Exception("Completed quest snapshot should be Completed");
                if (saveData.FindQuest("history_save_failed")?.Status != QuestStatus.Failed)
                    throw new Exception("Failed quest snapshot should be Failed");
                if (saveData.Metadata != "slot1")
                    throw new Exception("Metadata should be stored on the save data");

                Debug.Log("✓ CaptureSaveData includes active quests and history");
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

        private static MockConditionInstance GetCompletion(QuestState state, string objectiveId)
        {
            return (MockConditionInstance)state.Objectives[objectiveId].CompletionInstance;
        }

        private static void SetConditionId(ConditionAsset condition, string id)
        {
            typeof(ConditionAsset).GetField("conditionId", BindingFlags.NonPublic | BindingFlags.Instance)?
                .SetValue(condition, id);
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

            var playerRef = typeof(QuestManager).GetField("playerRef", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(questManager) as QuestPlayerRef;
            if (playerRef != null)
                UnityEngine.Object.DestroyImmediate(playerRef.gameObject);

            UnityEngine.Object.DestroyImmediate(questManager.gameObject);
        }
    }
}
