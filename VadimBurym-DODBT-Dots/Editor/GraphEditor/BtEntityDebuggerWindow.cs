// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using System;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;
    using Unity.Profiling;
    using VadimBurym.DodBehaviourTree.Dots;

    internal sealed class BtEntityDebuggerWindow : EditorWindow
    {
        private const int PageSize = 100;
        private static readonly ProfilerMarker SnapshotMarker = new("DODBT.Debugger.ListSnapshot");
        private static readonly ProfilerMarker PageMarker = new("DODBT.Debugger.PageRead");
        private static readonly ProfilerMarker DrawMarker = new("DODBT.Debugger.ListGUI");
        private const WorldFlags RuntimeWorldKinds = (WorldFlags.Game | WorldFlags.Simulation | WorldFlags.GameServer |
            WorldFlags.GameClient | WorldFlags.GameThinClient) & ~WorldFlags.Live;
        private World _world;
        private readonly List<World> _worlds = new();
        private string[] _worldNames = Array.Empty<string>();
        private readonly List<Entity> _entities = new();
        private readonly Dictionary<string, BtGraphAsset> _graphs = new();
        private readonly List<Row> _rows = new();
        private string _filter = "";
        private string _error;
        private int _page;
        private double _nextRefresh;
        private Vector2 _scroll;
        private GUIStyle _title, _muted, _cell, _active, _warning, _column, _badge;
        private bool _dark;
        private sealed class Row
        {
            internal Entity Entity;
            internal BtGraphAsset Graph;
            internal string Name, Identity;
            internal BtActiveLeafMap.Summary Leaves;
        }

        private void OnEnable()
        {
            minSize = new Vector2(740, 360);
            wantsMouseMove = true;
        }

        private void OnDisable()
        {
            _rows.Clear();
            _graphs.Clear();
            _entities.Clear();
            _worlds.Clear();
            _world = null;
        }

        internal static void Open() => GetWindow<BtEntityDebuggerWindow>("DOTS BT Debugger");

        private void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextRefresh) return;
            _nextRefresh = EditorApplication.timeSinceStartup + .5;
            RefreshEntities();
            Repaint();
        }

        private void RefreshEntities()
        {
            using var profile = SnapshotMarker.Auto();
            _entities.Clear();
            _rows.Clear();
            if (!EditorApplication.isPlaying || _world == null || !_world.IsCreated) return;
            using var query = _world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BtDebugTree>(), ComponentType.ReadOnly<NodeStateElement>(), ComponentType.ReadOnly<LeafStateElement>());
            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (string.IsNullOrWhiteSpace(_filter) || entity.Index.ToString().Contains(_filter) ||
                    _world.EntityManager.GetName(entity).IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    _entities.Add(entity);
            }
            _entities.Sort((a, b) => a.Index.CompareTo(b.Index));
            _page = Mathf.Clamp(_page, 0, Math.Max(0, (_entities.Count - 1) / PageSize));
            RefreshPage();
        }

        private void RefreshPage()
        {
            using var profile = PageMarker.Auto();
            _rows.Clear();
            if (_world == null || !_world.IsCreated || _entities.Count == 0) return;
            var manager = _world.EntityManager;
            // Synchronize once per snapshot, never once per repaint or for every entity in the World.
            manager.CompleteDependencyBeforeRO<NodeStateElement>();
            manager.CompleteDependencyBeforeRO<LeafStateElement>();
            var maps = new Dictionary<BtGraphAsset, BtActiveLeafMap>();
            for (int i = _page * PageSize; i < Math.Min(_entities.Count, (_page + 1) * PageSize); i++)
            {
                var entity = _entities[i];
                if (!manager.Exists(entity) || !manager.HasComponent<BtDebugTree>(entity) ||
                    !manager.HasBuffer<NodeStateElement>(entity) || !manager.HasBuffer<LeafStateElement>(entity)) continue;
                var tree = manager.GetSharedComponent<BtDebugTree>(entity);
                string guid = tree.GraphGuid.ToString();
                if (!_graphs.TryGetValue(guid, out var graph) || graph == null)
                    _graphs[guid] = graph = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(AssetDatabase.GUIDToAssetPath(guid));
                var summary = new BtActiveLeafMap.Summary("Source unavailable");
                if (graph != null)
                {
                    if (!maps.TryGetValue(graph, out var map)) maps.Add(graph, map = new BtActiveLeafMap(graph));
                    summary = map.Read(tree, manager.GetBuffer<NodeStateElement>(entity, true), manager.GetBuffer<LeafStateElement>(entity, true));
                }
                _rows.Add(new Row { Entity = entity, Graph = graph, Name = manager.GetName(entity),
                    Identity = $"{entity.Index}:{entity.Version}", Leaves = summary });
            }
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect entities with a behaviour tree.", MessageType.Info);
                return;
            }
            DrawDebugger();
        }

        internal void DrawDebugger()
        {
            using var profile = DrawMarker.Auto();
            EnsureStyles();
            DrawHeading();
            _worlds.Clear();
            foreach (var world in World.All)
                if (world.IsCreated && (world.Flags & RuntimeWorldKinds) != 0) _worlds.Add(world);
            if (_worlds.Count == 0)
            {
                _world = null;
                _entities.Clear();
                _rows.Clear();
                _page = 0;
                _error = null;
                EditorGUILayout.LabelField("No game worlds available.");
                return;
            }
            if (_worldNames.Length != _worlds.Count) _worldNames = new string[_worlds.Count];
            for (int i = 0; i < _worlds.Count; i++) _worldNames[i] = _worlds[i].Name;
            int old = _worlds.IndexOf(_world);
            int selected;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("WORLD", _column, GUILayout.Width(48));
                selected = EditorGUILayout.Popup(Math.Max(0, old), _worldNames, EditorStyles.toolbarPopup);
                GUILayout.Space(16);
                GUILayout.Label("FIND", _column, GUILayout.Width(36));
                string filter = GUILayout.TextField(_filter, EditorStyles.toolbarSearchField, GUILayout.MinWidth(180));
                if (filter != _filter) { _filter = filter; _page = 0; _scroll = Vector2.zero; RefreshEntities(); }
            }
            if (old != selected) { _world = _worlds[selected]; _page = 0; _error = null; RefreshEntities(); }
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Warning);
            GUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                GUILayout.Label($"{_entities.Count:N0} entities", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                int page = _page;
                using (new EditorGUI.DisabledScope(_page == 0)) if (GUILayout.Button("Previous", EditorStyles.miniButton, GUILayout.Width(68))) page--;
                GUILayout.Label($"{_page + 1} / {Math.Max(1, (_entities.Count + PageSize - 1) / PageSize)}", _muted, GUILayout.Width(70));
                using (new EditorGUI.DisabledScope((_page + 1) * PageSize >= _entities.Count)) if (GUILayout.Button("Next", EditorStyles.miniButton, GUILayout.Width(54))) page++;
                GUILayout.Space(8);
                if (page != _page) { _page = page; _scroll = Vector2.zero; RefreshPage(); }
            }
            DrawTable();
            GUILayout.Label(new GUIContent("  Running leaves: 2 Hz  |  Observe: full graph at 10 Hz",
                "The list reads existing entered-leaf state for this page only. Instant Success/Failure leaves are not retained. Reading ECS state can wait for jobs; close this window for performance measurements."), _muted, GUILayout.Height(26));
            if (Event.current.type == EventType.MouseMove) Repaint();
        }

        private void DrawTable()
        {
            var header = GUILayoutUtility.GetRect(0, 26, GUILayout.ExpandWidth(true));
            var viewport = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(100));
            float width = Mathf.Max(700, viewport.width - 16);
            float entityWidth = width * .30f, treeWidth = width * .21f;
            float activeX = entityWidth + treeWidth, buttonX = width - 88;
            EditorGUI.DrawRect(header, _dark ? new Color(.13f, .15f, .18f) : new Color(.77f, .80f, .84f));
            GUI.Label(new Rect(12, header.y, entityWidth - 16, 26), "ENTITY / INDEX", _column);
            GUI.Label(new Rect(entityWidth + 8, header.y, treeWidth - 16, 26), "BEHAVIOUR TREE", _column);
            GUI.Label(new Rect(activeX + 8, header.y, buttonX - activeX - 12, 26), "RUNNING LEAVES", _column);
            const float rowHeight = 42;
            _scroll = GUI.BeginScrollView(viewport, _scroll, new Rect(0, 0, width, _rows.Count * rowHeight));
            int first = Mathf.Max(0, (int)(_scroll.y / rowHeight));
            int last = Mathf.Min(_rows.Count, Mathf.CeilToInt((_scroll.y + viewport.height) / rowHeight));
            for (int i = first; i < last; i++)
            {
                var row = _rows[i];
                var rect = new Rect(0, i * rowHeight, width, rowHeight);
                bool hover = rect.Contains(Event.current.mousePosition);
                Color background = _dark ? new Color(.18f, .19f, .21f) : new Color(.88f, .89f, .91f);
                if ((i & 1) != 0) background *= _dark ? .9f : .97f;
                if (hover) background = _dark ? new Color(.20f, .27f, .33f) : new Color(.73f, .84f, .92f);
                EditorGUI.DrawRect(rect, background);
                GUI.Label(new Rect(12, rect.y + 3, entityWidth - 20, 20), new GUIContent(string.IsNullOrEmpty(row.Name) ? "Unnamed entity" : row.Name, row.Name), _cell);
                GUI.Label(new Rect(12, rect.y + 22, entityWidth - 20, 16), row.Identity, _muted);
                string graphName = row.Graph != null ? row.Graph.name : "Source unavailable";
                GUI.Label(new Rect(entityWidth + 8, rect.y + 11, treeWidth - 16, 20), new GUIContent(graphName, graphName), _cell);
                var style = !row.Leaves.IsValid ? _warning : row.Leaves.Count > 0 ? _active : _muted;
                GUI.Label(new Rect(activeX + 8, rect.y + 11, buttonX - activeX - 16, 20), new GUIContent(row.Leaves.Text, row.Leaves.Text), style);
                using (new EditorGUI.DisabledScope(row.Graph == null))
                    if (GUI.Button(new Rect(buttonX, rect.y + 10, 78, 23), "Observe", EditorStyles.miniButton))
                    {
                        try
                        {
                            if (!_world.IsCreated || !_world.EntityManager.Exists(row.Entity))
                                throw new InvalidOperationException("This entity no longer exists. Wait for the next list refresh.");
                            BtEditorWindow.OpenWithEntity(row.Graph, _world, row.Entity);
                            _error = null;
                        }
                        catch (Exception exception) { _error = exception.Message; }
                    }
            }
            GUI.EndScrollView();
            if (_rows.Count == 0)
                GUI.Label(new Rect(viewport.x + 12, viewport.y + 16, viewport.width - 24, 24), "No matching entities with a behaviour tree.", _muted);
        }

        private void DrawHeading()
        {
            var rect = GUILayoutUtility.GetRect(0, 62, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, _dark ? new Color(.10f, .12f, .15f) : new Color(.84f, .87f, .91f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3, rect.height), new Color(.28f, .72f, 1));
            GUI.Label(new Rect(14, rect.y + 7, 200, 28), "DOTS BT", _title);
            GUI.Label(new Rect(15, rect.y + 35, 230, 20), "Entity debugger", _muted);
            GUI.Label(new Rect(rect.xMax - 185, rect.y + 18, 170, 24), "LIVE STATE / 2 Hz", _badge);
        }

        private void EnsureStyles()
        {
            if (_title != null && _dark == EditorGUIUtility.isProSkin) return;
            _dark = EditorGUIUtility.isProSkin;
            _title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 };
            _cell = new GUIStyle(EditorStyles.label) { clipping = TextClipping.Clip };
            _muted = new GUIStyle(EditorStyles.miniLabel) { clipping = TextClipping.Clip };
            _muted.normal.textColor = _dark ? new Color(.60f, .65f, .70f) : new Color(.32f, .37f, .42f);
            _active = new GUIStyle(_cell);
            _active.normal.textColor = _dark ? new Color(.44f, .82f, 1) : new Color(.03f, .35f, .55f);
            _warning = new GUIStyle(_cell);
            _warning.normal.textColor = _dark ? new Color(1, .76f, .38f) : new Color(.55f, .30f, .02f);
            _column = new GUIStyle(_muted) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _badge = new GUIStyle(_column) { alignment = TextAnchor.MiddleRight };
        }
    }
}
