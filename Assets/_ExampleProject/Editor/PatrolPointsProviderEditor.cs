using _ExampleProject.Code.Features.Enemy.Behaviours;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PatrolPointsProvider))]
internal sealed class PatrolPointsProviderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_markersRoot"));
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_patrolPositions"), true);
        serializedObject.ApplyModifiedProperties();
        if (GUILayout.Button("Collect Patrol Points", GUILayout.Height(30)))
        {
            Undo.RecordObject(target, "Collect Patrol Points");
            ((PatrolPointsProvider)target).Collect();
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }
    }
}
