// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    internal sealed class LeafTemplateCreator : EditorWindow
    {
        private string _name = "NewLeaf", _namespace, _folder, _error;
        private Type _context, _state;
        private BtCreationSettings _settings;

        internal static void Open()
        {
            var window = GetWindow<LeafTemplateCreator>(true, "New Leaf");
            window._name = "NewLeaf";
            window._error = null;
            BtCreationWindowUi.ShowCentered(window, 5);
        }

        private void OnEnable()
        {
            _settings = new BtCreationSettings("Leaf");
            _folder = _settings.Folder;
            _namespace = _settings.Namespace;
            _context = _settings.Context;
            _state = _settings.LeafState;
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            BtCreationWindowUi.Begin(this, "Create Leaf", "Add a behaviour to your tree.", 5);
            EditorGUI.BeginChangeCheck();
            _name = BtCreationWindowUi.Text(this, 0, "Class name", _name, "leaf-name");
            _namespace = BtCreationWindowUi.Text(this, 1, "Namespace", _namespace, "leaf-namespace");
            if (EditorGUI.EndChangeCheck()) { _settings.Namespace = _namespace; _error = null; }
            BtCreationWindowUi.Context(this, 2, _context, value =>
            {
                _settings.Context = _context = value;
                _error = null;
            });
            BtCreationWindowUi.State(this, 3, _state, value => { _settings.LeafState = _state = value; _error = null; });
            BtCreationWindowUi.Folder(this, 4, _folder, value => { _settings.Folder = _folder = value; _error = null; }, error => _error = error);
            string path = _folder + "/" + _name.TrimStart('@') + ".cs";
            string validation = BtScriptCreation.Validate(path, _name, _namespace, _context, _state);
            if (BtScriptCreation.Busy) validation = "Stop Play Mode and wait for scripts to compile successfully.";
            if (!BtCreationWindowUi.Create(this, _error ?? validation, path)) return;
            var types = BtScriptCreation.Types();
            if (BtScriptCreation.NameExists(_name, _namespace, types))
            { _error = "This name is already used by a type or namespace."; return; }
            _error = BtScriptCreation.Write(path, LeafSourceTemplate.Render(_name, _namespace, _context, _state, types));
            if (_error == null) Close();
        }
    }
}
