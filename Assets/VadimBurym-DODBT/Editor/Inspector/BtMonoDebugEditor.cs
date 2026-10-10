using UnityEditor;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

[CustomEditor(typeof(BtMonoDebug))]
internal sealed class BtMonoDebugEditor : Editor
{
    private bool _showRunning = true;
    public override bool RequiresConstantRepaint() => EditorApplication.isPlaying;

    public override void OnInspectorGUI()
    {
        var debug = (BtMonoDebug)target;
        var asset = debug.BehaviourTreeAsset;
        var graph = asset == null ? null : AssetDatabase.LoadAssetAtPath<BtGraphAsset>(AssetDatabase.GUIDToAssetPath(asset.GUID));
        bool ready = graph != null && debug.DebugStatus != null && !graph.NotActualCompiledVersion;
        if (graph == null)
            EditorGUILayout.HelpBox("Construct BtMonoDebug with a compiled tree and agent state to inspect its source graph.", MessageType.Info);
        else if (graph.NotActualCompiledVersion)
            EditorGUILayout.HelpBox("The graph has changed. Compile it before starting Debug Mode.", MessageType.Warning);
        using (new EditorGUI.DisabledScope(!ready))
            if (GUILayout.Button("Open In Debug-Mode", GUILayout.Height(30))) BtEditorWindow.OpenWithDebugMode(graph, debug);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Behaviour Tree Asset", asset, typeof(BehaviourTreeAsset), false);
            _showRunning = EditorGUILayout.Foldout(_showRunning, "Running Leafs (" + (debug.RunningLeafs?.Count ?? 0) + ")", true);
            if (_showRunning && debug.RunningLeafs != null)
                for (int i = 0; i < debug.RunningLeafs.Count; i++) EditorGUILayout.TextField(debug.RunningLeafs[i]);
        }
    }
}
