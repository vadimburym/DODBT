// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using UnityEditor;

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    internal static class BtDebugTarget
    {
        private static readonly System.Collections.Generic.Dictionary<string, BtGraphAsset> Graphs = new();

        static BtDebugTarget() => EditorApplication.projectChanged += Graphs.Clear;

        internal static bool TryGetGraph(BtMonoDebug target, out BtGraphAsset graph, out string error)
        {
            graph = null;
            error = null;
            if (!EditorApplication.isPlaying || target == null)
            { error = "The agent is no longer available in Play Mode."; return false; }
            var compiled = target.BehaviourTreeAsset;
            if (compiled == null || target.DebugStatus == null)
            { error = "Call BtMonoDebug.Construct with the agent's tree and state first."; return false; }
            string guid = compiled.GUID;
            if (!string.IsNullOrEmpty(guid) && (!Graphs.TryGetValue(guid, out graph) || graph == null))
            {
                graph = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (graph != null) Graphs[guid] = graph;
            }
            if (graph == null || graph.CompiledAsset != compiled)
            { error = "The source graph for this compiled tree is unavailable."; return false; }
            // Legacy assets have no revision field. Unity may deserialize it as either null or empty.
            if (graph.NotActualCompiledVersion || (graph.CompiledRevision ?? "") != (target.CompiledRevision ?? "") ||
                (compiled.CompiledRevision ?? "") != (target.CompiledRevision ?? ""))
            { error = "The graph or compiled tree changed. Compile it and restart the agent before observing."; return false; }
            return true;
        }

        internal static void Observe(BtMonoDebug target)
        {
            if (TryGetGraph(target, out var graph, out var error)) BtEditorWindow.OpenWithDebugMode(graph, target);
            else UnityEngine.Debug.LogWarning(error);
        }
    }
}
