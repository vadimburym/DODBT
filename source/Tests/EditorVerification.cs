using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Tests
{
    internal sealed class EditorVerification : ICallbacks
    {
        private static TestRunnerApi _runner;
        private static EditorVerification _callbacks;

        [MenuItem("Tools/VadimBurym/Verification/Run EditMode tests")]
        private static void Run()
        {
            if (_runner != null) return;
            _runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            _callbacks = new EditorVerification();
            _runner.RegisterCallbacks(_callbacks);
            _runner.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "VadimBurym.DodBehaviourTree.Tests" }
            }));
        }

        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory("Logs");
            TestRunnerApi.SaveResultToFile(result, "Logs/EditorVerification.xml");
            Debug.Log($"DODBT editor tests: {result.PassCount} passed, {result.FailCount} failed.");
            EditorApplication.delayCall += () =>
            {
                if (_runner == null) return;
                _runner.UnregisterCallbacks(_callbacks);
                Object.DestroyImmediate(_runner);
                _runner = null;
                _callbacks = null;
            };
        }
    }
}
