// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System.IO;
using UnityEditor;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    internal static class BtGraphAssets
    {
        internal static bool OwnsCompiledAsset(BtGraphAsset graph)
        {
            if (graph == null || graph.CompiledAsset == null) return false;
            string path = AssetDatabase.GetAssetPath(graph);
            return !string.IsNullOrEmpty(path) &&
                graph.CompiledAsset.GUID == AssetDatabase.AssetPathToGUID(path);
        }

        internal static BehaviourTreeAsset EnsureCompiledAsset(BtGraphAsset graph) =>
            OwnsCompiledAsset(graph) ? graph.CompiledAsset : CreateCompiledAsset(graph, graph.CompiledAsset);

        internal static BehaviourTreeAsset CreateCompiledAsset(BtGraphAsset graph, BehaviourTreeAsset source = null)
        {
            string path = BtEditorPaths.GetCompiledAssetPath(graph);
            var compiled = source != null ? Object.Instantiate(source) : ScriptableObject.CreateInstance<BehaviourTreeAsset>();
            compiled.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(compiled, path);
            compiled.InternalGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(graph));
            graph.CompiledAsset = compiled;
            EditorUtility.SetDirty(compiled);
            EditorUtility.SetDirty(graph);
            return compiled;
        }

        // Unity's Duplicate command initially preserves the reference to the original graph's compiled asset.
        // Only the owning graph may rename or delete that companion.
        internal static bool TryDelete(BtGraphAsset graph, out string error)
        {
            error = null;
            string graphPath = AssetDatabase.GetAssetPath(graph);
            string compiledPath = OwnsCompiledAsset(graph) ? AssetDatabase.GetAssetPath(graph.CompiledAsset) : null;
            if (string.IsNullOrEmpty(graphPath) || !AssetDatabase.DeleteAsset(graphPath))
            {
                error = "Cannot delete the graph asset. Its compiled asset was preserved.";
                return false;
            }
            if (!string.IsNullOrEmpty(compiledPath) && !AssetDatabase.DeleteAsset(compiledPath))
                error = "Graph deleted, but its compiled asset could not be deleted: " + compiledPath;
            return true;
        }
    }
}
