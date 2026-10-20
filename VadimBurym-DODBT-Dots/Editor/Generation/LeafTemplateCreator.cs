// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    public sealed class LeafTemplateCreator : EditorWindow
    {
        private string _name = "NewLeaf";
        private string _namespace = "Game.AI";
        private Type _context;
        private string _folder;
        private BtCreationSettings _settings;
        private string _error;
        private const string PendingKey = "DODBT.PendingLeafCreation";

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void CompilationFinished() => SessionState.EraseString(PendingKey);

        public static void CreateNewLeaf()
        {
            var window = GetWindow<LeafTemplateCreator>(true, "New Leaf");
            window._name = "NewLeaf";
            window._error = null;
            BtCreationWindowUi.ShowCentered(window, 4);
        }

        private void OnEnable()
        {
            _settings = new BtCreationSettings("Leaf");
            _folder = _settings.Folder;
            _namespace = _settings.Namespace;
            _context = _settings.Context;
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            BtCreationWindowUi.Begin(this, "Create Leaf", "Add a behaviour to your tree.", 4);
            EditorGUI.BeginChangeCheck();
            _name = BtCreationWindowUi.Text(this, 0, "Class name", _name, "leaf-name");
            if (EditorGUI.EndChangeCheck()) _error = null;
            EditorGUI.BeginChangeCheck();
            _namespace = BtCreationWindowUi.Text(this, 1, "Namespace", _namespace, "leaf-namespace");
            if (EditorGUI.EndChangeCheck()) { _settings.Namespace = _namespace; _error = null; }
            BtCreationWindowUi.Context(this, 2, _context, context =>
            {
                _settings.Context = _context = context;
                _error = null;
            });
            BtCreationWindowUi.Folder(this, 3, _folder, folder =>
            {
                _settings.Folder = _folder = folder;
                _error = null;
            }, error => _error = error);
            bool unavailable = EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || EditorUtility.scriptCompilationFailed;
            string pendingPath = SessionState.GetString(PendingKey, "");
            bool pending = !string.IsNullOrEmpty(pendingPath) && File.Exists(pendingPath);
            string className = _name.TrimStart('@');
            string path = _folder + "/" + className + ".cs";
            string validation = Validate(path);
            if (unavailable || pending) validation = "Stop Play Mode and wait for scripts to compile successfully.";
            if (!BtCreationWindowUi.Create(this, _error ?? validation, path, !unavailable && !pending)) return;
            CreateLeaf(path, className);
        }

        private string Validate(string path)
        {
            if (!Identifier(_name) || (!string.IsNullOrEmpty(_namespace) && !_namespace.Split('.').All(Identifier)))
                return "Use valid C# names for the class and namespace.";
            string fileNameError = BtCreationSettings.GraphNameError(_name.TrimStart('@'));
            if (fileNameError != null) return fileNameError;
            if (_context == null) return BtGraphContext.Available().Length == 0
                ? "Add [GenerateBtRunner] to your unmanaged context struct first." : "Choose a context for this leaf.";
            if (!BtCreationSettings.TryAssetFolder(_folder, out _)) return "Choose an existing folder inside Assets.";
            string assembly = CompilationPipeline.GetAssemblyNameFromScriptPath(path);
            if (Path.GetFileNameWithoutExtension(assembly) != _context.Assembly.GetName().Name)
                return "Choose a folder in the same assembly as the context.";
            if (File.Exists(path) || Directory.Exists(path)) return "A file with this name already exists. Choose another class name.";
            return null;
        }

        private void CreateLeaf(string path, string className)
        {
            var context = _context;
            var ids = TypeCache.GetTypesWithAttribute<LeafCodeGenAttribute>().Select(t => t.GetCustomAttribute<LeafCodeGenAttribute>())
                .Where(a => a?.ContextType == context).Select(a => (int)a.Id).ToHashSet();
            int id = LeafSourceTemplate.FirstAvailableId(ids);
            var knownTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetTypes).ToArray();
            string fullName = string.IsNullOrEmpty(_namespace) ? className :
                string.Join(".", _namespace.Split('.').Select(p => p.TrimStart('@'))) + "." + className;
            if (knownTypes.Any(t => t.FullName == fullName))
            { _error = "A type with this namespace and class name already exists."; return; }
            string code = LeafSourceTemplate.Render(className, _namespace, context, id, knownTypes);
            try
            {
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                using var writer = new StreamWriter(stream);
                writer.Write(code);
            }
            catch (IOException error) { _error = error.Message; return; }
            catch (UnauthorizedAccessException error) { _error = error.Message; return; }
            SessionState.SetString(PendingKey, path);
            AssetDatabase.ImportAsset(path);
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
            Close();
        }

        private static Type[] GetTypes(System.Reflection.Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException error) { return error.Types.Where(t => t != null).ToArray(); }
        }

        private static bool Identifier(string value) => Regex.IsMatch(value ?? "", @"^@?[A-Za-z_][A-Za-z0-9_]*$");
    }
}
