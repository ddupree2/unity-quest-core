using System;
using System.Reflection;
using DynamicBox.Quest.Core;
using DynamicBox.Quest.Core.Conditions;
using DynamicBox.Quest.Core.Events;
using UnityEngine;

namespace DynamicBox.Quest.Tests
{
    /// <summary>
    /// Tests for what quest UI reads: ObjectiveState.CompletionProgress and QuestManager.OnQuestStarted.
    /// </summary>
    public static class QuestUiSupportTests
    {
        public static void RunAllUiSupportTests()
        {
            Debug.Log("\n=== Running Quest UI Support Tests ===");
            TestCompletionProgressReportsCounter();
            TestCompletionProgressNullWithoutReporting();
            TestOnQuestStartedFiresOnStartOnly();
            Debug.Log("✓ All quest UI support tests passed!");
        }

        private static void TestCompletionProgressReportsCounter()
        {
            Debug.Log("\n[TEST] CompletionProgress Reports Counter");

            var questManager = CreateQuestManager();
            try
            {
                var condition = TestEvents.Wire(ScriptableObject.CreateInstance<ItemCollectedConditionAsset>());
                SetField(typeof(ConditionAsset), condition, "conditionId", "ui_progress_item");
                SetField(typeof(ItemCollectedConditionAsset), condition, "requiredCount", 8);
                var objective = new ObjectiveBuilder().WithObjectiveId("obj1").WithCompletionCondition(condition).Build();
                var quest = new QuestBuilder().WithQuestId("ui_progress").AddObjective(objective).Build();

                var state = questManager.StartQuest(quest);
                for (int i = 0; i < 3; i++)
                    TestEvents.Items.Raise(new ItemCollectedEvent("ui_progress_item", 1));

                var progress = state.Objectives["obj1"].CompletionProgress;
                if (progress == null)
                    throw new Exception("An item collected condition should expose progress");
                if (Mathf.Abs(progress.Progress - 3f / 8f) > 0.001f)
                    throw new Exception($"Expected progress 3/8, got {progress.Progress}");
                if (!progress.ProgressDescription.StartsWith("3/8"))
                    throw new Exception($"Expected description starting '3/8', got '{progress.ProgressDescription}'");

                Debug.Log("✓ CompletionProgress reports counter progress");
            }
            finally
            {
                Cleanup(questManager);
            }
        }

        private static void TestCompletionProgressNullWithoutReporting()
        {
            Debug.Log("\n[TEST] CompletionProgress Null Without Reporting");

            var withMock = new ObjectiveState(new ObjectiveBuilder().WithObjectiveId("obj1")
                .WithCompletionCondition(ScriptableObject.CreateInstance<MockConditionAsset>()).Build());
            if (withMock.CompletionProgress != null)
                throw new Exception("A condition without progress reporting should give null progress");

            var withoutCondition = new ObjectiveState(new ObjectiveBuilder().WithObjectiveId("obj2").Build());
            if (withoutCondition.CompletionProgress != null)
                throw new Exception("An objective without a condition should give null progress");

            Debug.Log("✓ CompletionProgress is null when there's no progress to report");
        }

        private static void TestOnQuestStartedFiresOnStartOnly()
        {
            Debug.Log("\n[TEST] OnQuestStarted Fires On Start Only");

            var source = CreateQuestManager();
            var target = CreateQuestManager();
            try
            {
                var objective = new ObjectiveBuilder().WithObjectiveId("obj1")
                    .WithCompletionCondition(ScriptableObject.CreateInstance<MockConditionAsset>()).Build();
                var quest = new QuestBuilder().WithQuestId("ui_started").AddObjective(objective).Build();

                QuestState started = null;
                int startedCount = 0;
                source.OnQuestStarted += q => { started = q; startedCount++; };

                var state = source.StartQuest(quest);
                if (startedCount != 1 || started != state)
                    throw new Exception("OnQuestStarted should fire once with the new quest state");
                if (state.Objectives["obj1"].Status != ObjectiveStatus.InProgress)
                    throw new Exception("Objectives should already be active when OnQuestStarted fires");

                int restoredStarts = 0;
                target.OnQuestStarted += q => restoredStarts++;
                target.RestoreSaveData(source.CaptureSaveData(), id => id == quest.QuestId ? quest : null);
                if (restoredStarts != 0)
                    throw new Exception("Restoring a save should not raise OnQuestStarted");

                Debug.Log("✓ OnQuestStarted fires on StartQuest and not on restore");
            }
            finally
            {
                Cleanup(source);
                Cleanup(target);
            }
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
