// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using UnityEditor;
    using UnityEngine;
    using Unity.Profiling;

    [InitializeOnLoad]
    internal static class BtSceneDebugOverlay
    {
        private const string Preference = "DODBT.SceneDebugMarker";
        private const string BrainIconGuid = "655062e0711a4a0418908facc9a5dfe4";
        private static readonly ProfilerMarker SampleMarker = new("DODBT.Debugger.SceneSample");
        private static readonly ProfilerMarker DrawMarker = new("DODBT.Debugger.SceneGUI");
        private static readonly Color Accent = new(.2f, .78f, 1f, 1f);
        private static BtSceneDebugTarget _target;
        private static GUIStyle _nameStyle, _buttonStyle;
        private static Texture2D _brainIcon;
        private static bool _iconLoaded;
        private static readonly GUIContent Button = new("Observe", "Open this entity's behaviour tree in Debug Mode");

        static BtSceneDebugOverlay()
        {
            Selection.selectionChanged += SelectionChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            SceneView.duringSceneGui += Draw;
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.delayCall += SelectionChanged;
        }

        internal static bool Enabled
        {
            get => EditorPrefs.GetBool(Preference, true);
            set
            {
                if (Enabled == value) return;
                EditorPrefs.SetBool(Preference, value);
                SelectionChanged();
            }
        }

        private static void PlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) SelectionChanged();
            else Clear();
        }

        private static void Clear()
        {
            bool hadTarget = _target != null;
            _target?.Dispose();
            _target = null;
            if (hadTarget) SceneView.RepaintAll();
        }

        private static void SelectionChanged()
        {
            Clear();
            if (!EditorApplication.isPlaying || !EditorPrefs.GetBool(Preference, true)) return;
            if (!BtEntitySelectionAdapter.TryRead(Selection.activeObject, out var world, out var entity) &&
                !BtEntitySelectionAdapter.TryRead(Selection.activeContext, out world, out entity)) return;
            var target = new BtSceneDebugTarget(world, entity);
            if (!target.Refresh()) { target.Dispose(); return; }
            _target = target;
            SceneView.RepaintAll();
        }

        private static void Draw(SceneView view)
        {
            if (_target == null || !EditorApplication.isPlaying || view.camera == null) return;
            // Follow the scene's own render cadence. No editor timer or extra scene
            // renders; no position reads at all while Scene View is hidden.
            using (SampleMarker.Auto())
                if (!_target.Refresh()) { Clear(); return; }
            if (!_target.HasPosition) return;
            var viewport = view.camera.WorldToViewportPoint(_target.Position);
            if (viewport.z <= 0 || viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) return;
            using var profile = DrawMarker.Auto();
            Vector3 feet = _target.Position + Vector3.up * .06f;
            Vector2 screen = HandleUtility.WorldToGUIPoint(feet);
            if (Event.current.type == EventType.Repaint)
            {
                using var color = new Handles.DrawingScope(Accent);
                Handles.DrawWireDisc(feet, Vector3.up, .7f, 2f);
            }
            var card = GetCardRect(screen, view.position.width);
            int cardControl = GUIUtility.GetControlID(FocusType.Passive);
            if (Event.current.type == EventType.Layout && card.Contains(Event.current.mousePosition))
                HandleUtility.AddDefaultControl(cardControl);
            Handles.BeginGUI();
            try
            {
                _nameStyle ??= new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip, fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                _nameStyle.normal.textColor = new Color(.92f, .95f, .98f);
                _buttonStyle ??= new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Bold };
                if (!_iconLoaded)
                {
                    _brainIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(BrainIconGuid));
                    _iconLoaded = true;
                }
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(new Rect(card.x + 2, card.y + 2, card.width, card.height), new Color(0, 0, 0, .25f));
                    EditorGUI.DrawRect(card, new Color(.22f, .36f, .43f, .95f));
                    EditorGUI.DrawRect(new Rect(card.x + 1, card.y + 1, card.width - 2, card.height - 2),
                        new Color(.075f, .105f, .13f, .98f));
                    EditorGUI.DrawRect(new Rect(card.x, card.y, 3, card.height), Accent);
                    if (_brainIcon != null)
                        GUI.DrawTexture(new Rect(card.x + 9, card.y + 4, 22, 22), _brainIcon, ScaleMode.ScaleToFit);
                }
                var nameRect = new Rect(card.x + 36, card.y + 4, 116, 22);
                var buttonRect = new Rect(card.x + 158, card.y + 5, 70, 20);
                GUI.Label(nameRect, _target.Label, _nameStyle);
                if (GUI.Button(buttonRect, Button, _buttonStyle))
                    if (!_target.Observe(out var error)) view.ShowNotification(new GUIContent(error));
                // Prevent clicks on the card from selecting the ground behind it.
                if (card.Contains(Event.current.mousePosition) &&
                    (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp)) Event.current.Use();
            }
            finally { Handles.EndGUI(); }
        }

        internal static Rect GetCardRect(Vector2 screen, float width) =>
            new(Mathf.Clamp(screen.x - 118, 8, Mathf.Max(8, width - 244)), Mathf.Max(30, screen.y - 70), 236, 30);
    }
}
