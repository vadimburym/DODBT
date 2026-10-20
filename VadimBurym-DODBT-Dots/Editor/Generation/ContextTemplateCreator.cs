// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    internal sealed class ContextTemplateCreator : EditorWindow
    {
        private const string PendingKey = "DODBT.PendingContextCreation";
        private string _name = "NewContext";
        private string _namespace;
        private string _folder;
        private string _error;
        private string _assemblyDefinition;
        private bool _editorOnly;
        private BtCreationSettings _settings;

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void CompilationFinished() => SessionState.EraseString(PendingKey);

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
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            BtCreationWindowUi.Begin(this, "Create Context", "The ECS data shared with your tree's leaves.", 3);
            EditorGUI.BeginChangeCheck();
            _name = BtCreationWindowUi.Text(this, 0, "Context name", _name, "context-name");
            if (EditorGUI.EndChangeCheck()) _error = null;
            EditorGUI.BeginChangeCheck();
            _namespace = BtCreationWindowUi.Text(this, 1, "Namespace", _namespace, "context-namespace");
            if (EditorGUI.EndChangeCheck()) { _settings.Namespace = _namespace; _error = null; }
            BtCreationWindowUi.Folder(this, 2, _folder, folder =>
            {
                _settings.Folder = _folder = folder;
                _error = null;
            }, error => _error = error);
            string path = _folder + "/" + _name.TrimStart('@') + ".cs";
            string validation = Validate(path);
            string pending = SessionState.GetString(PendingKey, "");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || EditorUtility.scriptCompilationFailed || !string.IsNullOrEmpty(pending) && File.Exists(pending))
                validation = "Stop Play Mode and wait for scripts to compile successfully.";
            if (BtCreationWindowUi.Create(this, _error ?? validation, path)) CreateContext(path);
        }

        private string Validate(string path)
        {
            if (!ContextSourceTemplate.Identifier(_name) || !string.IsNullOrEmpty(_namespace) && !_namespace.Split('.').All(ContextSourceTemplate.Identifier))
                return "Use valid C# names for the context and namespace.";
            string nameError = BtCreationSettings.GraphNameError(_name.TrimStart('@'));
            if (nameError != null) return nameError;
            if (!BtCreationSettings.TryAssetFolder(_folder, out _)) return "Choose an existing folder inside Assets.";
            if (File.Exists(path) || Directory.Exists(path)) return "A file with this name already exists. Choose another context name.";
            string assembly = Path.GetFileNameWithoutExtension(CompilationPipeline.GetAssemblyNameFromScriptPath(path));
            if (string.IsNullOrEmpty(assembly)) return "Choose a folder in a runtime assembly.";
            string definition = CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(path);
            if (string.IsNullOrEmpty(definition))
            {
                if (assembly == "Assembly-CSharp-Editor" || assembly == "Assembly-CSharp-Editor-firstpass")
                    return "Contexts belong in a runtime assembly, outside Editor folders.";
            }
            else
            {
                if (_assemblyDefinition != definition)
                {
                    try
                    {
                        var platforms = JsonUtility.FromJson<AssemblyPlatforms>(File.ReadAllText(definition));
                        _editorOnly = platforms.includePlatforms?.Length > 0 && platforms.includePlatforms.All(p => p == "Editor");
                        _assemblyDefinition = definition;
                    }
                    catch (IOException) { return "The assembly definition is unavailable. Check the selected folder."; }
                    catch (UnauthorizedAccessException) { return "The assembly definition could not be read."; }
                    catch (ArgumentException) { return "Fix the selected assembly definition before creating a context."; }
                }
                if (_editorOnly)
                    return "Contexts belong in a runtime assembly, not an Editor-only assembly.";
            }
            return null;
        }

        private void CreateContext(string path)
        {
            string assemblyName = Path.GetFileNameWithoutExtension(CompilationPipeline.GetAssemblyNameFromScriptPath(path));
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName);
            string name = _name.TrimStart('@');
            string fullName = ContextSourceTemplate.FullName(_name, _namespace);
            if (assembly != null)
            {
                _error = NameConflict(name, fullName, GetTypes(assembly));
                if (_error != null) return;
            }
            try
            {
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                using var writer = new StreamWriter(stream);
                writer.Write(ContextSourceTemplate.Render(name, _namespace));
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

        internal static string NameConflict(string name, string fullName, Type[] types)
        {
            if (types.Any(t => t.FullName == fullName))
                return "A type with this namespace and name already exists in this assembly.";
            if (types.Any(t => t.Name == name && t.IsDefined(typeof(GenerateBtRunnerAttribute), false)))
                return "Contexts in one assembly need distinct names, even in different namespaces.";
            return null;
        }

        [Serializable]
        private sealed class AssemblyPlatforms
        {
            public string[] includePlatforms = Array.Empty<string>();
        }
    }
}
