// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    internal static class BtCreationWindowUi
    {
        private static GUIStyle _title, _subtitle, _label, _input, _button, _message;

        internal static void ShowCentered(EditorWindow window, int fields)
        {
            ShowCentered(window, new Vector2(500, 206 + fields * 62));
        }

        internal static void ShowCentered(EditorWindow window, Vector2 size)
        {
            window.minSize = window.maxSize = size;
            window.wantsMouseMove = true;
            window.wantsMouseEnterLeaveWindow = true;
            window.ShowUtility();
            var desktop = EditorGUIUtility.GetMainWindowPosition();
            window.position = new Rect(desktop.center - size * .5f, size);
            window.Focus();
        }

        internal static void Begin(EditorWindow window, string title, string subtitle, int fields)
        {
            EnsureStyles();
            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseEnterWindow ||
                Event.current.type == EventType.MouseLeaveWindow) window.Repaint();
            EditorGUI.DrawRect(new Rect(Vector2.zero, window.position.size), new Color(.12f, .12f, .12f));
            GUI.Label(new Rect(22, 17, window.position.width - 44, 27), title, _title);
            GUI.Label(new Rect(22, 46, window.position.width - 44, 20), subtitle, _subtitle);
            Rounded(new Rect(22, 76, window.position.width - 44, fields * 62 + 16), new Color(.09f, .09f, .095f), new Color(.22f, .23f, .24f), 10);
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Event.current.Use();
                window.Close();
                GUIUtility.ExitGUI();
            }
        }

        private static Rect Field(EditorWindow window, int row, string label)
        {
            float y = 88 + row * 62;
            GUI.Label(new Rect(38, y, window.position.width - 76, 18), label, _label);
            return new Rect(38, y + 20, window.position.width - 76, 30);
        }

        internal static string Text(EditorWindow window, int row, string label, string value, string control)
        {
            var rect = Field(window, row, label);
            bool focused = GUI.GetNameOfFocusedControl() == control;
            Rounded(rect, new Color(.135f, .14f, .145f), focused ? new Color(.40f, .61f, .80f) : new Color(.24f, .25f, .26f), 6);
            GUI.SetNextControlName(control);
            return EditorGUI.TextField(rect, value, _input);
        }

        internal static void Context(EditorWindow window, int row, Type current, Action<Type> select)
        {
            var rect = Field(window, row, "Context");
            if (BtLeafSelector.Draw(rect, current?.Name ?? "Select context...", current == null ? "Choose the context for this tree's leaves" : BtGraphContext.Label(current),
                "context", BtSelectorIcons.Context, current != null))
                new BtTypePicker(BtGraphContext.Available(), context =>
                {
                    if (window == null) return;
                    select(context);
                    window.Repaint();
                }, current).Show(rect);
        }

        internal static void Folder(EditorWindow window, int row, string folder, Action<string> select, Action<string> error)
        {
            var rect = Field(window, row, "Folder");
            if (!BtLeafSelector.Draw(rect, folder, folder, "folder", BtSelectorIcons.Folder)) return;
            string chosen = EditorUtility.OpenFolderPanel("Choose a folder in Assets", Path.GetFullPath(folder), "");
            if (string.IsNullOrEmpty(chosen)) return;
            if (BtCreationSettings.TryAssetFolder(chosen, out string relative)) select(relative);
            else error("Choose an existing folder inside this project's Assets folder.");
            GUIUtility.ExitGUI();
        }

        internal static void State(EditorWindow window, int row, Type current, Action<Type> select)
        {
            var rect = Field(window, row, "Leaf state");
            if (BtLeafSelector.Draw(rect, current?.Name ?? "Select leaf state...", BtGraphContext.Label(current),
                "leaf-state", BtSelectorIcons.Context, current != null))
                new BtTypePicker(BtGraphContext.States(), state =>
                {
                    if (window == null) return;
                    select(state);
                    window.Repaint();
                }, current, "Select Leaf State").Show(rect);
        }

        internal static bool Create(EditorWindow window, string error, string preview, bool enabled = true)
        {
            bool invalid = !string.IsNullOrEmpty(error);
            _message.normal.textColor = invalid ? new Color(.95f, .66f, .54f) : new Color(.59f, .62f, .66f);
            GUI.Label(new Rect(28, window.position.height - 100, window.position.width - 56, 36),
                new GUIContent(invalid ? error : preview, invalid ? error : preview), _message);
            using (new EditorGUI.DisabledScope(!enabled || invalid))
            {
                var rect = new Rect((window.position.width - 208) * .5f, window.position.height - 54, 208, 36);
                bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
                var fill = !GUI.enabled ? new Color(.19f, .23f, .27f) : hover ? new Color(.18f, .46f, .75f) : new Color(.12f, .38f, .66f);
                Rounded(rect, fill, new Color(.37f, .55f, .72f, GUI.enabled ? .7f : .2f), 8);
                if (GUI.Button(rect, "Create", _button)) return true;
                if (GUI.enabled && Event.current.type == EventType.KeyDown &&
                    (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                {
                    Event.current.Use();
                    return true;
                }
            }
            return false;
        }

        private static void Rounded(Rect rect, Color fill, Color border, float radius)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, border, 0, radius);
            GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2),
                Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, fill, 0, radius - 1);
        }

        private static void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            _subtitle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            _label = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            _message = new GUIStyle(EditorStyles.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _input = new GUIStyle(EditorStyles.textField) { fontSize = 12, padding = new RectOffset(10, 10, 0, 0), alignment = TextAnchor.MiddleLeft };
            _button = new GUIStyle(EditorStyles.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            foreach (var style in new[] { _title, _label, _input, _button })
                foreach (var state in new[] { style.normal, style.hover, style.focused, style.active })
                    state.textColor = new Color(.92f, .93f, .94f);
            _subtitle.normal.textColor = new Color(.63f, .66f, .69f);
            foreach (var state in new[] { _input.normal, _input.hover, _input.focused, _input.active })
            {
                state.background = null;
                state.scaledBackgrounds = null;
            }
        }
    }
}
