// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using VadimBurym.DodBehaviourTree;

[CustomEditor(typeof(BehaviourTreeAsset))]
internal sealed class BehaviourTreeAssetEditor : OdinEditor
{
    private Editor compiledEditor;
    private BtGraphAsset graphAsset;

    protected override void OnEnable()
    {
        base.OnEnable();
        var btAsset = (BehaviourTreeAsset)target;
        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(btAsset.GUID);
        graphAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<BtGraphAsset>(path);
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (graphAsset == null)
        {
            EditorGUILayout.HelpBox("Could not find data about the Graph. If you renamed the asset from outside the Editor window - find this graph in Editor and rename through rename-button.", MessageType.Warning);
            return;
        }
        CreateCachedEditor(graphAsset, null, ref compiledEditor);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            compiledEditor.OnInspectorGUI();
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (compiledEditor != null)
        {
            DestroyImmediate(compiledEditor);
            compiledEditor = null;
        }
    }
}
#else
using UnityEditor;
using VadimBurym.DodBehaviourTree;

[CustomEditor(typeof(BehaviourTreeAsset))]
internal sealed class BehaviourTreeAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Odin Inspector is required for the full BehaviourTree editor experience.\n" +
            "Please install Odin Inspector to enable this inspector." +
            "If you have already installed Odin Inspector and this window did not disappear,\n" +
            "make sure that 'ODIN_INSPECTOR' is present in:\n" +
            "Project Settings → Player → Scripting Define Symbols.",
            MessageType.Info);
    }
}
#endif