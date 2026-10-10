using UnityEditor;
using VadimBurym.DodBehaviourTree;

[CustomEditor(typeof(BehaviourTreeAsset))]
internal sealed class BehaviourTreeAssetEditor : Editor
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
