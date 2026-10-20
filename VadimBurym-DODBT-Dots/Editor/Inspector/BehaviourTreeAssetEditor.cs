// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using UnityEditor;
    using VadimBurym.DodBehaviourTree.Dots;

    [CustomEditor(typeof(BehaviourTreeAsset))]
    internal sealed class BehaviourTreeAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (BehaviourTreeAsset)target;
            var path = AssetDatabase.GUIDToAssetPath(asset.GUID);
            var graph = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(path);
            if (graph == null)
            {
                EditorGUILayout.HelpBox("The source graph is unavailable. The compiled runtime asset can still be used.", MessageType.Info);
                return;
            }
            BtGraphAssetEditor.DrawGraph(graph);
        }
    }
}
