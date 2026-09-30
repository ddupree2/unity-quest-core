using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DynamicBox.Quest.Tests
{
    /// <summary>
    /// Runs the existing static test suites through Unity's Test Runner. Each suite throws on the
    /// first failed check, which NUnit reports as a failure with that message. New tests for
    /// fixes go here as ordinary [Test] / [UnityTest] methods.
    /// </summary>
    /// <remarks>
    /// PlayMode, not EditMode: the suites rely on Unity running Awake when a component's object
    /// is activated, and on SendMessage("Update"), neither of which Unity does in Edit mode.
    /// </remarks>
    [TestFixture]
    public class QuestCoreTests
    {
        private readonly HashSet<GameObject> _rootsBeforeTest = new();

        [SetUp]
        public void SetUp()
        {
            _rootsBeforeTest.Clear();
            _rootsBeforeTest.UnionWith(SceneManager.GetActiveScene().GetRootGameObjects());
        }

        // Some suites create GameObjects with `new GameObject` rather than through
        // ServiceTestHelpers, so destroy whatever a suite leaves behind.
        [TearDown]
        public void TearDown()
        {
            ServiceTestHelpers.CleanupAll();

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!_rootsBeforeTest.Contains(root))
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void TestSetupIsValid() => Assert.IsTrue(TestValidation.ValidateAllComponents(),
            "Test validation failed; see the Console for which component.");

        [Test] public void QuestSystem() => QuestSystemTests.RunAllTests();
        [Test] public void QuestSystemAdvanced() => QuestSystemAdvancedTests.RunAdvancedTests();
        [Test] public void ProgressReporting() => ProgressReportingTests.RunAllProgressTests();
        [Test] public void QuestContext() => QuestContextTests.RunAllContextTests();
        [Test] public void Serialization() => QuestSerializationTests.RunAllSerializationTests();
        [Test] public void StateRestoration() => QuestStateRestorationTests.RunAllRestorationTests();
        [Test] public void ServiceImplementations() => ServiceImplementationTests.RunAllServiceTests();
        [Test] public void EventDrivenConditions() => EventDrivenConditionTests.RunAllEventDrivenTests();
        [Test] public void FactoryMethods() => FactoryMethodTests.RunAllFactoryMethodTests();
        [Test] public void ImmutableEvents() => ImmutableEventTests.RunAllImmutableEventTests();

        // Fix: DirtyQueueProcessor threw "collection was modified" when completing an objective
        // activated a next one whose flag was already set.
        [Test] public void DirtyQueueChainedFlagObjectives() => QuestSystemAdvancedTests.TestSequentialFlagObjectivesAlreadyMet();

        // Completed/failed quest history (QuestLog / QuestManager)
        [Test] public void QuestHistory() => QuestHistoryTests.RunAllHistoryTests();

        // Full save/restore: condition progress, history, RestoreSaveData / ClearAll
        [Test] public void QuestSaveRestore() => QuestSaveRestoreTests.RunAllSaveRestoreTests();

        // What quest UI reads: CompletionProgress and OnQuestStarted
        [Test] public void QuestUiSupport() => QuestUiSupportTests.RunAllUiSupportTests();

        // Frame-based suite (QuestManager polling, events across frames). Its failures surface as
        // logged exceptions, which the Test Runner also treats as failures.
        [UnityTest]
        public IEnumerator Integration()
        {
            var runnerObject = new GameObject("QuestIntegrationTestRunner");
            runnerObject.SetActive(false);
            var runner = runnerObject.AddComponent<QuestSystemIntegrationTests>();

            // It starts itself from Start() by default; run it here instead so the test waits for it.
            typeof(QuestSystemIntegrationTests)
                .GetField("runTestsOnStart", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(runner, false);

            runnerObject.SetActive(true);
            yield return runner.StartCoroutine(runner.RunAllIntegrationTests());
        }

        // Timing measurements, slow and machine-dependent: run on demand only.
        [Test, Explicit] public void PerformanceBenchmarks() => PerformanceBenchmarkTests.RunAllBenchmarks();
    }
}
