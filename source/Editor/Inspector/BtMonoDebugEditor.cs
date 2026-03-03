// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using VadimBurym.DodBehaviourTree;

[CustomEditor(typeof(BtMonoDebug))]
internal sealed class BtMonoDebugEditor : OdinEditor
{
    private Editor compiledEditor;
    private BtGraphAsset graphAsset;
    
    private void OpenDebugMode()
    {
        if (graphAsset == null)
        {
            return;
        }
        BtEditorWindow.OpenWithDebugMode(graphAsset, (BtMonoDebug)target);
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        var monoDebug = (BtMonoDebug)target;
        var btAsset = monoDebug.BehaviourTreeAsset;
        if (btAsset == null)
            return;
        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(btAsset.GUID);
        graphAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<BtGraphAsset>(path);
    }
    
    public override void OnInspectorGUI()
    {
        if (graphAsset == null)
        {
            EditorGUILayout.HelpBox("Could not find data about the Graph. If you renamed the asset from outside the Editor window - find this graph in Editor and rename through rename-button. If you haven't used the Construct method in BtMonoDebug, use it.", MessageType.Warning);
            return;
        }

        if (graphAsset.LastModifiedDate != graphAsset.CompiledVersion)
        {
            EditorGUILayout.HelpBox("The compiled version is different from the current version of the graph. Open in Editor and compile the asset to the current version.", MessageType.Warning);
        }
        
        using (new EditorGUI.DisabledScope(graphAsset == null || graphAsset.LastModifiedDate != graphAsset.CompiledVersion))
        {
            if (SirenixEditorGUI.Button("Open In Debug-Mode", ButtonSizes.Large))
            {
                OpenDebugMode();
            }
        }
        base.OnInspectorGUI();
    }
}
#else
using UnityEditor;
using VadimBurym.DodBehaviourTree;

[CustomEditor(typeof(BtMonoDebug))]
internal sealed class BtMonoDebugEditor : Editor
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