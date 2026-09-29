using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DynamicBox.Quest.Tests
{
    /// <summary>
    /// Runs the existing static test suites through Unity's Test Runner (EditMode). Each suite
    /// throws on the first failed check, which NUnit reports as a failure with that message.
    /// New tests for fixes go here as ordinary [Test] methods.
    /// </summary>
    /// <remarks>
    /// QuestSystemIntegrationTests is a MonoBehaviour coroutine that needs Update to run, so it
    /// can only run in PlayMode and isn't covered here.
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
        // ServiceTestHelpers, so destroy whatever a suite leaves in the open scene.
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

        // Timing measurements, slow and machine-dependent: run on demand only.
        [Test, Explicit] public void PerformanceBenchmarks() => PerformanceBenchmarkTests.RunAllBenchmarks();
    }
}
