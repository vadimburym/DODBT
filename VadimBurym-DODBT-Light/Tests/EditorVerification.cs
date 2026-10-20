// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Light.Editor;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Light.Tests
{
    internal sealed class EditorVerification : ICallbacks
    {
        private static TestRunnerApi _runner;
        private static EditorVerification _callbacks;

        // The Tools launcher is a command; registering children there hides it in Unity.
        [MenuItem("Window/General/DODBT-Light Tests")]
        private static void Run()
        {
            if (_runner != null) return;
            _runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            _callbacks = new EditorVerification();
            _runner.RegisterCallbacks(_callbacks);
            _runner.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "VadimBurym.DodBehaviourTree.Light.Tests" }
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
