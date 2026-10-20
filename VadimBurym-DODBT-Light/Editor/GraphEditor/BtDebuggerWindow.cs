// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    internal sealed class BtDebuggerWindow : EditorWindow
    {
        private const int PageSize = 100;
        private readonly List<BtMonoDebug> _agents = new();
        private readonly List<BtMonoDebug> _filtered = new();
        private readonly List<(BtMonoDebug Agent, string Name, string Tree, string Leaves)> _rows = new();
        private SearchField _search;
        private string _query = "";
        private bool _activeLeaves = true;
        private int _page;
        private Vector2 _scroll;
        private double _nextDiscovery, _nextSample;
        private GUIStyle _title, _small, _row;

        internal static void Open() => GetWindow<BtDebuggerWindow>("Light BT Debugger");

        private void OnEnable()
        {
            minSize = new Vector2(600, 320);
            _search = new SearchField();
            EditorApplication.update += UpdateSnapshot;
        }
        private void OnDisable() => EditorApplication.update -= UpdateSnapshot;

        private void UpdateSnapshot()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now < _nextSample) return;
            _nextSample = now + .2;
            if (!EditorApplication.isPlaying)
            { _agents.Clear(); _filtered.Clear(); _rows.Clear(); _nextDiscovery = 0; Repaint(); return; }
            if (now >= _nextDiscovery)
            {
                _nextDiscovery = now + 1;
                _agents.Clear();
                _agents.AddRange(UnityEngine.Object.FindObjectsByType<BtMonoDebug>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(a => a.gameObject.scene.IsValid() && a.BehaviourTreeAsset != null));
                _agents.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
            }
            _filtered.Clear();
            foreach (var agent in _agents)
                if (agent != null && (_query.Length == 0 || agent.name.IndexOf(_query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    agent.GetInstanceID().ToString().Contains(_query))) _filtered.Add(agent);
            _page = Mathf.Clamp(_page, 0, Math.Max(0, (_filtered.Count - 1) / PageSize));
            _rows.Clear();
            foreach (var agent in _filtered.Skip(_page * PageSize).Take(PageSize))
                _rows.Add((agent, agent.name, agent.BehaviourTreeAsset != null ? agent.BehaviourTreeAsset.name : "Missing tree",
                    _activeLeaves && agent.RunningLeafs != null ? string.Join(", ", agent.RunningLeafs) : ""));
            Repaint();
        }

        private void OnGUI()
        {
            _title ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
            _small ??= new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(.66f, .7f, .75f) } };
            _row ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            EditorGUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                GUILayout.Label("Behaviour Trees", _title);
                GUILayout.FlexibleSpace();
                GUILayout.Label(_agents.Count + " agents", EditorStyles.boldLabel);
                GUILayout.Space(12);
            }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                _query = _search.OnToolbarGUI(_query);
                _activeLeaves = GUILayout.Toggle(_activeLeaves, "Active leaves", EditorStyles.toolbarButton, GUILayout.Width(90));
                if (EditorGUI.EndChangeCheck()) { _page = 0; _nextSample = 0; }
            }
            if (!EditorApplication.isPlaying)
            { EditorGUILayout.HelpBox("Enter Play Mode to inspect agents connected through BtMonoDebug.", MessageType.Info); return; }
            EditorGUILayout.LabelField("Existing debug states are sampled at 5 Hz. Agent discovery runs only while this window is open.", _small);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(_page == 0))
                    if (GUILayout.Button("Previous", EditorStyles.toolbarButton, GUILayout.Width(70))) { _page--; _nextSample = 0; }
                GUILayout.FlexibleSpace();
                GUILayout.Label((_filtered.Count == 0 ? 0 : _page + 1) + " / " + Mathf.CeilToInt(_filtered.Count / (float)PageSize));
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope((_page + 1) * PageSize >= _filtered.Count))
                    if (GUILayout.Button("Next", EditorStyles.toolbarButton, GUILayout.Width(55))) { _page++; _nextSample = 0; }
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row.Agent == null) continue;
                var rect = EditorGUILayout.GetControlRect(false, 29);
                if ((i & 1) == 0) EditorGUI.DrawRect(rect, new Color(1, 1, 1, .035f));
                float width = (rect.width - 92) / (_activeLeaves ? 3 : 2);
                GUI.Label(new Rect(rect.x + 8, rect.y, width - 8, rect.height), new GUIContent(row.Name, row.Agent.GetInstanceID().ToString()), _row);
                GUI.Label(new Rect(rect.x + width, rect.y, width, rect.height), row.Tree, _row);
                if (_activeLeaves) GUI.Label(new Rect(rect.x + 2 * width, rect.y, width, rect.height), new GUIContent(row.Leaves, row.Leaves), _row);
                if (GUI.Button(new Rect(rect.xMax - 85, rect.y + 3, 78, 23), "Observe")) BtDebugTarget.Observe(row.Agent);
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
