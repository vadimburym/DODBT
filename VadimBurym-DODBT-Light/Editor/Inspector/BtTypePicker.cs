// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;

    internal sealed class BtTypePicker : PopupWindowContent
    {
        private const float RowHeight = 26;
        private const float HeaderHeight = 66;
        private const float FooterHeight = 26;
        private readonly string _title;
        private readonly Type[] _types;
        private readonly Action<Type> _select;
        private readonly Type _currentType;
        private readonly bool _leafSelection;
        private readonly bool _contextSelection;
        private static readonly Vector3[] ObjectOutline = new Vector3[7];
        private readonly List<Type> _visible = new();
        private readonly SearchField _searchField = new();
        private static readonly Dictionary<Type, Type[]> Cache = new();
        private string _query = "";
        private Vector2 _scroll;
        private int _selected;
        private float _width = 600;
        private GUIStyle _nameStyle;
        private GUIStyle _namespaceStyle;
        private GUIStyle _hintStyle;

        internal BtTypePicker(Type baseType, Action<Type> select, Type currentType = null, bool leafSelection = false, Func<Type, bool> allowed = null)
            : this(GetTypes(baseType).Where(t => allowed == null || allowed(t)).ToArray(), select,
                currentType, "Select " + (leafSelection ? "Leaf" : baseType?.Name ?? "type"), leafSelection, false)
        {
        }

        internal BtTypePicker(Type[] contexts, Action<Type> select, Type currentType = null, string title = "Select Context")
            : this(contexts, select, currentType, title, false, true)
        {
        }

        private BtTypePicker(Type[] types, Action<Type> select, Type currentType, string title, bool leafSelection, bool contextSelection)
        {
            _title = title;
            _select = select;
            _currentType = currentType;
            _leafSelection = leafSelection;
            _contextSelection = contextSelection;
            _types = types;
            Filter();
            _selected = Math.Max(0, _visible.IndexOf(currentType));
        }

        internal void Show(Rect rect)
        {
            _width = Mathf.Clamp(Mathf.Max(rect.width, 600), 420, 760);
            PopupWindow.Show(rect, this);
        }

        public override Vector2 GetWindowSize() =>
            new(_width, HeaderHeight + FooterHeight + Mathf.Clamp((_types.Length + (_contextSelection ? 0 : 1)) * RowHeight, 104, 312));

        public override void OnOpen()
        {
            editorWindow.wantsMouseMove = true;
            _searchField.SetFocus();
            RevealSelection(GetWindowSize().y - HeaderHeight - FooterHeight);
        }

        internal static Type Resolve(string serializedTypeName)
        {
            if (string.IsNullOrEmpty(serializedTypeName)) return null;
            if (ResolvedNames.TryGetValue(serializedTypeName, out var cached)) return cached;
            int space = serializedTypeName.IndexOf(' ');
            var type = space < 0 ? null : Type.GetType(serializedTypeName.Substring(space + 1) + ", " + serializedTypeName.Substring(0, space));
            if (type != null) ResolvedNames[serializedTypeName] = type;
            return type;
        }

        private static readonly Dictionary<string, Type> ResolvedNames = new();

        internal static Type[] GetTypes(Type baseType)
        {
            if (baseType == null) return Array.Empty<Type>();
            if (Cache.TryGetValue(baseType, out var types)) return types;
            var assemblies = new HashSet<string>(CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(a => a.name));
            types = TypeCache.GetTypesDerivedFrom(baseType).Append(baseType)
                .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters && t.IsSerializable
                    && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                    && assemblies.Contains(t.Assembly.GetName().Name)
                    && t.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null)
                .Distinct().OrderBy(t => t.Name, StringComparer.Ordinal)
                .ThenBy(t => t.Assembly.GetName().Name, StringComparer.Ordinal)
                .ThenBy(t => t.FullName, StringComparer.Ordinal).ToArray();
            Cache[baseType] = types;
            return types;
        }

        private void Filter()
        {
            _visible.Clear();
            var words = _query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (!_contextSelection && words.All(w => "None".IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)) _visible.Add(null);
            foreach (var type in _types)
            {
                string searchable = type.FullName ?? type.Name;
                if (_contextSelection) searchable += " " + type.Assembly.GetName().Name;
                if (words.All(w => searchable.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0))
                    _visible.Add(type);
            }
            _selected = 0;
            _scroll = Vector2.zero;
        }

        public override void OnGUI(Rect rect)
        {
            EnsureStyles();
            var evt = Event.current;
            float listHeight = rect.height - HeaderHeight - FooterHeight;
            HandleKeys(evt, listHeight);
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(.16f, .16f, .16f) : new Color(.86f, .86f, .86f));
            GUI.Label(new Rect(12, 7, rect.width - 90, 22), _title, EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.width - 78, 8, 66, 20), _visible.Count + " results", _hintStyle);
            string query = _searchField.OnGUI(new Rect(12, 35, rect.width - 24, 20), _query);
            if (query != _query)
            {
                _query = query;
                Filter();
            }

            var viewport = new Rect(4, HeaderHeight, rect.width - 8, listHeight);
            float rowWidth = viewport.width - (_visible.Count * RowHeight > listHeight ? 16 : 0);
            _scroll = GUI.BeginScrollView(viewport, _scroll, new Rect(0, 0, rowWidth, _visible.Count * RowHeight));
            int first = Mathf.Max(0, Mathf.FloorToInt(_scroll.y / RowHeight));
            int end = Mathf.Min(_visible.Count, Mathf.CeilToInt((_scroll.y + listHeight) / RowHeight));
            int clicked = -1;
            for (int i = first; i < end; i++)
            {
                var row = new Rect(0, i * RowHeight, rowWidth, RowHeight);
                if (row.Contains(evt.mousePosition))
                {
                    if (evt.type == EventType.MouseMove) { _selected = i; editorWindow.Repaint(); }
                    if (evt.type == EventType.MouseDown && evt.button == 0) { clicked = i; evt.Use(); }
                }
                DrawRow(row, _visible[i], i == _selected);
            }
            GUI.EndScrollView();
            if (_visible.Count == 0)
                GUI.Label(new Rect(16, HeaderHeight + 18, rect.width - 32, 24),
                    _contextSelection ? (_types.Length == 0 ? "No runtime contexts found" : "No matching contexts") : "No matching types",
                    EditorStyles.centeredGreyMiniLabel);
            EditorGUI.DrawRect(new Rect(12, rect.height - FooterHeight, rect.width - 24, 1), new Color(.5f, .5f, .5f, .25f));
            GUI.Label(new Rect(12, rect.height - FooterHeight + 4, rect.width - 24, 20),
                "↑ ↓  Navigate     Enter  Select     Esc  Close", _hintStyle);
            if (clicked >= 0) Select(clicked);
        }

        private void EnsureStyles()
        {
            if (_nameStyle != null) return;
            _nameStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, clipping = TextClipping.Clip, padding = new RectOffset() };
            _namespaceStyle = new GUIStyle(EditorStyles.label) { clipping = TextClipping.Clip, padding = new RectOffset() };
            _hintStyle = new GUIStyle(EditorStyles.miniLabel);
            _namespaceStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(.64f, .64f, .64f) : new Color(.37f, .37f, .37f);
            _hintStyle.normal.textColor = _namespaceStyle.normal.textColor;
        }

        private void DrawRow(Rect row, Type type, bool selected)
        {
            if (selected)
            {
                EditorGUI.DrawRect(row, EditorGUIUtility.isProSkin ? new Color(.23f, .31f, .40f) : new Color(.68f, .77f, .87f));
                EditorGUI.DrawRect(new Rect(row.x, row.y + 4, 2, row.height - 8), new Color(.40f, .68f, .95f));
            }
            if (type != null) DrawTypeIcon(new Rect(row.x + 10, row.y + 5, 16, 16));
            else GUI.Label(new Rect(row.x + 11, row.y + 3, 16, 20), "—", _namespaceStyle);
            string name = type?.Name ?? "None";
            string tooltip = type == null ? "Clear the selected type" : _contextSelection ? BtGraphContext.Label(type) : type.FullName;
            float left = row.x + 36;
            float available = Mathf.Max(0, row.width - 62);
            float nameWidth = Mathf.Min(_nameStyle.CalcSize(new GUIContent(name)).x, available);
            GUI.Label(new Rect(left, row.y + 4, nameWidth, 20), new GUIContent(name, tooltip), _nameStyle);
            if (type != null)
            {
                float namespaceWidth = Mathf.Max(0, available - nameWidth - 6);
                string detail = type.Namespace ?? "global namespace";
                GUI.Label(new Rect(left + nameWidth + 6, row.y + 4, namespaceWidth, 20),
                    new GUIContent("(" + detail + ")", tooltip), _namespaceStyle);
            }
            if (type == _currentType)
                GUI.Label(new Rect(row.xMax - 22, row.y + 4, 18, 20), new GUIContent("✓", "Current type"), _namespaceStyle);
        }

        private void DrawTypeIcon(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (_contextSelection)
            {
                BtSelectorIcons.Context(rect, Color.white);
                return;
            }
            if (_leafSelection)
            {
                if (!EditorGUIUtility.isProSkin) EditorGUI.DrawRect(rect, new Color(.38f, .42f, .45f));
                BtLeafSelector.DrawIcon(rect, Color.white);
                return;
            }
            ObjectOutline[0] = new Vector3(rect.x + 8, rect.y + 1);
            ObjectOutline[1] = new Vector3(rect.x + 14, rect.y + 4);
            ObjectOutline[2] = new Vector3(rect.x + 14, rect.y + 11);
            ObjectOutline[3] = new Vector3(rect.x + 8, rect.y + 15);
            ObjectOutline[4] = new Vector3(rect.x + 2, rect.y + 11);
            ObjectOutline[5] = new Vector3(rect.x + 2, rect.y + 4);
            ObjectOutline[6] = ObjectOutline[0];
            var center = new Vector3(rect.x + 8, rect.y + 8);
            var saved = Handles.color;
            Handles.color = EditorGUIUtility.isProSkin ? new Color(.73f, .77f, .80f) : new Color(.30f, .34f, .38f);
            Handles.DrawAAPolyLine(1.5f, ObjectOutline);
            Handles.DrawAAPolyLine(1.5f, ObjectOutline[5], center, ObjectOutline[1]);
            Handles.DrawAAPolyLine(1.5f, center, ObjectOutline[3]);
            Handles.color = saved;
        }

        private void HandleKeys(Event evt, float listHeight)
        {
            if (evt.type != EventType.KeyDown) return;
            if (evt.keyCode == KeyCode.Escape) { evt.Use(); editorWindow.Close(); GUIUtility.ExitGUI(); }
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                evt.Use();
                if (_visible.Count > 0) Select(_selected);
                return;
            }
            int delta = evt.keyCode switch
            {
                KeyCode.DownArrow => 1,
                KeyCode.UpArrow => -1,
                KeyCode.PageDown => Math.Max(1, (int)(listHeight / RowHeight)),
                KeyCode.PageUp => -Math.Max(1, (int)(listHeight / RowHeight)),
                _ => 0
            };
            if (delta == 0) return;
            _selected = Mathf.Clamp(_selected + delta, 0, Math.Max(0, _visible.Count - 1));
            RevealSelection(listHeight);
            evt.Use();
            editorWindow.Repaint();
        }

        private void RevealSelection(float listHeight)
        {
            float top = _selected * RowHeight;
            if (top < _scroll.y) _scroll.y = top;
            else if (top + RowHeight > _scroll.y + listHeight) _scroll.y = top + RowHeight - listHeight;
        }

        private void Select(int index)
        {
            Type selected = _visible[index];
            editorWindow.Close();
            _select(selected);
            GUIUtility.ExitGUI();
        }
    }
}
