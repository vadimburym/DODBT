// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(BtGraphAsset))]
    internal sealed class BtGraphAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI() => DrawGraph((BtGraphAsset)target);

        internal static void DrawGraph(BtGraphAsset graph)
        {
            BtGraphContext.BindLegacy(graph);
            BtGraphContext.Draw(graph);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Creation Date", graph.CreationDate ?? "");
                EditorGUILayout.TextField("Last Modified Date", graph.LastModifiedDate ?? "");
                EditorGUILayout.TextField("Compiled Version", graph.CompiledVersion ?? "");
                EditorGUILayout.IntField("Nodes Count", graph.NodesCount);
                EditorGUILayout.IntField("Leaf Nodes Count", graph.LeafNodesCount);
            }
            if (SerializationUtility.HasManagedReferencesWithMissingTypes(graph))
                EditorGUILayout.HelpBox("Some serialized types are missing. Restore their scripts before compiling; the saved data is retained.", MessageType.Error);
            if (graph.NotActualCompiledVersion)
                EditorGUILayout.HelpBox("The graph has changed. Compile it to update the runtime asset.", MessageType.Warning);
            if (GUILayout.Button("Open In Editor", GUILayout.Height(30))) graph.OpenAsset();
        }
    }
}
