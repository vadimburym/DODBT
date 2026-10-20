// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    internal sealed class ContextTemplateCreator : EditorWindow
    {
        private string _name = "NewContext", _namespace, _folder, _error;
        private BtCreationSettings _settings;

        internal static void Open()
        {
            var window = GetWindow<ContextTemplateCreator>(true, "New Context");
            window._name = "NewContext";
            window._error = null;
            BtCreationWindowUi.ShowCentered(window, 3);
        }

        private void OnEnable()
        {
            _settings = new BtCreationSettings("Context");
            _folder = _settings.Folder;
            _namespace = _settings.Namespace;
        }

        private void OnGUI()
        {
            BtCreationWindowUi.Begin(this, "Create Context", "References and data shared by one agent's leaves.", 3);
            EditorGUI.BeginChangeCheck();
            _name = BtCreationWindowUi.Text(this, 0, "Context class", _name, "context-name");
            _namespace = BtCreationWindowUi.Text(this, 1, "Namespace", _namespace, "context-namespace");
            if (EditorGUI.EndChangeCheck()) { _settings.Namespace = _namespace; _error = null; }
            BtCreationWindowUi.Folder(this, 2, _folder, value => { _settings.Folder = _folder = value; _error = null; }, error => _error = error);
            string path = _folder + "/" + _name.TrimStart('@') + ".cs";
            string validation = BtScriptCreation.Validate(path, _name, _namespace);
            if (BtScriptCreation.Busy) validation = "Stop Play Mode and wait for scripts to compile successfully.";
            if (!BtCreationWindowUi.Create(this, _error ?? validation, path)) return;
            if (BtScriptCreation.NameExists(_name, _namespace, BtScriptCreation.Types()))
            { _error = "This name is already used by a type or namespace."; return; }
            _error = BtScriptCreation.Write(path, LeafSourceTemplate.RenderContext(_name, _namespace));
            if (_error == null) Close();
        }
    }
}
