// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using UnityEditor;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    [InitializeOnLoad]
    internal static class BtSceneDebugOverlay
    {
        private const string Preference = "DODBT.Light.SceneMarkers";
        private static BtMonoDebug _target;
        private static Texture _icon;
        private static bool _iconLoaded;
        private static GUIStyle _label;
        private static bool _enabled = EditorPrefs.GetBool(Preference, true);
        internal static bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                EditorPrefs.SetBool(Preference, value);
                SceneView.RepaintAll();
            }
        }

        static BtSceneDebugOverlay()
        {
            Selection.selectionChanged += SelectTarget;
            EditorApplication.playModeStateChanged += _ => SelectTarget();
            SceneView.duringSceneGui += Draw;
            SelectTarget();
        }

        private static void SelectTarget()
        {
            _target = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<BtMonoDebug>() : null;
            SceneView.RepaintAll();
        }

        private static void Draw(SceneView scene)
        {
            if (!Enabled || !EditorApplication.isPlaying || _target == null || _target.BehaviourTreeAsset == null) return;
            Vector3 position = _target.transform.position;
            if (scene.camera.WorldToViewportPoint(position).z <= 0) return;
            var previous = Handles.color;
            Handles.color = new Color(.3f, .8f, 1f, .9f);
            Handles.DrawWireDisc(position, scene.camera.transform.forward, HandleUtility.GetHandleSize(position) * .15f);
            Handles.color = previous;
            Vector2 screen = HandleUtility.WorldToGUIPoint(position);
            var rect = new Rect(screen.x - 135, screen.y - 62, 270, 34);
            rect.x = Mathf.Clamp(rect.x, 4, Mathf.Max(4, scene.position.width - rect.width - 4));
            rect.y = Mathf.Max(4, rect.y);
            Handles.BeginGUI();
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            _label ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            if (!_iconLoaded)
            {
                _iconLoaded = true;
                _icon = AssetDatabase.LoadAssetAtPath<Texture2D>(System.IO.Path.GetDirectoryName(BtEditorPaths.GetEditorWindowFolderPath()).Replace('\\', '/') + "/Icons/dodbt-icon.png");
            }
            GUI.Label(new Rect(rect.x + 8, rect.y + 5, 174, 24), new GUIContent(_target.name, _icon), _label);
            if (GUI.Button(new Rect(rect.xMax - 80, rect.y + 6, 72, 22), "Observe")) BtDebugTarget.Observe(_target);
            Handles.EndGUI();
        }
    }
}
