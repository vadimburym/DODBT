// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    using UnityEditor;
    using UnityEngine;
    using VadimBurym.DodBehaviourTree.Light;

    [CustomEditor(typeof(BtMonoDebug))]
    internal sealed class BtMonoDebugEditor : UnityEditor.Editor
    {
        private static readonly Unity.Profiling.ProfilerMarker DrawMarker = new("DODBT.Light.AgentInspector");
        private bool _showRunning = true;
        private double _nextRepaint;
        private readonly System.Collections.Generic.List<string> _lastLeaves = new();
        private BehaviourTreeAsset _lastAsset;
        private string _lastError;
        private bool _lastReady, _hasSnapshot;
        public override bool RequiresConstantRepaint()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < _nextRepaint) return false;
            _nextRepaint = EditorApplication.timeSinceStartup + .2;
            var debug = target as BtMonoDebug;
            if (debug == null) return false;
            bool ready = BtDebugTarget.TryGetGraph(debug, out _, out var error);
            var leaves = debug.RunningLeafs;
            int count = leaves?.Count ?? 0;
            bool changed = !_hasSnapshot || ready != _lastReady || error != _lastError ||
                debug.BehaviourTreeAsset != _lastAsset || count != _lastLeaves.Count;
            if (!changed)
                for (int i = 0; i < count; i++)
                    if (leaves[i] != _lastLeaves[i]) { changed = true; break; }
            if (!changed) return false;
            _hasSnapshot = true;
            _lastReady = ready;
            _lastError = error;
            _lastAsset = debug.BehaviourTreeAsset;
            _lastLeaves.Clear();
            for (int i = 0; i < count; i++) _lastLeaves.Add(leaves[i]);
            return true;
        }

        public override void OnInspectorGUI()
        {
            using var profile = DrawMarker.Auto();
            var debug = (BtMonoDebug)target;
            var asset = debug.BehaviourTreeAsset;
            bool ready = BtDebugTarget.TryGetGraph(debug, out var graph, out var error);
            if (graph == null)
                EditorGUILayout.HelpBox("Construct BtMonoDebug with a compiled tree and agent state to inspect its source graph.", MessageType.Info);
            else if (!ready)
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            using (new EditorGUI.DisabledScope(!ready))
                if (GUILayout.Button("Open In Debug-Mode", GUILayout.Height(30))) BtEditorWindow.OpenWithDebugMode(graph, debug);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Behaviour Tree Asset", asset, typeof(BehaviourTreeAsset), false);
            _showRunning = EditorGUILayout.Foldout(_showRunning, "Running Leafs (" + (debug.RunningLeafs?.Count ?? 0) + ")", true);
            if (_showRunning && debug.RunningLeafs != null)
                using (new EditorGUI.DisabledScope(true))
                    for (int i = 0; i < debug.RunningLeafs.Count; i++) EditorGUILayout.TextField(debug.RunningLeafs[i]);
        }
    }
}
