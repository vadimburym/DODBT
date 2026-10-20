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

    internal sealed class BtCreateGraphWindow : EditorWindow
    {
        private Type _context;
        private Type _state;
        private string _name = "BtGraph";
        private string _folder;
        private string _error;
        private BtCreationSettings _settings;
        private Func<string, Type, Type, bool> _create;

        internal static void Open(Func<string, Type, Type, bool> create)
        {
            var window = CreateInstance<BtCreateGraphWindow>();
            window.titleContent = new GUIContent("New BT Graph");
            window._create = create;
            BtCreationWindowUi.ShowCentered(window, 4);
        }

        private void OnEnable()
        {
            _settings = new BtCreationSettings("Graph");
            _folder = _settings.Folder;
            _context = _settings.Context;
            _state = _settings.LeafState;
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            if (_create == null) { Close(); return; }
            BtCreationWindowUi.Begin(this, "Create BT Graph", "A new tree for your game.", 4);
            EditorGUI.BeginChangeCheck();
            _name = BtCreationWindowUi.Text(this, 0, "Graph name", _name, "graph-name");
            if (EditorGUI.EndChangeCheck()) _error = null;
            BtCreationWindowUi.Context(this, 1, _context, context =>
            {
                _settings.Context = _context = context;
                _error = null;
            });
            BtCreationWindowUi.State(this, 2, _state, state => { _settings.LeafState = _state = state; _error = null; });
            BtCreationWindowUi.Folder(this, 3, _folder, folder =>
            {
                _settings.Folder = _folder = folder;
                _error = null;
            }, error => _error = error);
            string path = _folder + "/" + _name.Trim() + ".asset";
            string validation = BtCreationSettings.GraphNameError(_name.Trim());
            if (validation == null && _context == null)
                validation = BtGraphContext.Available().Length == 0 ? "Create a context with New Context first." : "Choose a context for this graph.";
            if (validation == null && _state == null) validation = "Choose a leaf state for this graph.";
            if (validation == null && !BtCreationSettings.TryAssetFolder(_folder, out _)) validation = "Choose an existing folder inside Assets.";
            if (validation == null && (File.Exists(path) || Directory.Exists(path))) validation = "An asset with this name already exists. Choose another name.";
            if (!BtCreationWindowUi.Create(this, _error ?? validation, path, !EditorApplication.isCompiling && !EditorApplication.isUpdating)) return;
            if (_create(path, _context, _state)) Close();
        }
    }
}
