// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    internal static class BtScriptCreation
    {
        private static readonly Dictionary<string, (string Target, HashSet<string> References)> AssemblyReferences = new();
        private static UnityEditor.Compilation.Assembly[] _playerAssemblies;

        static BtScriptCreation()
        {
            EditorApplication.projectChanged += () =>
            {
                AssemblyReferences.Clear();
                _playerAssemblies = null;
            };
        }

        private static (string Target, HashSet<string> References) ReferencesFor(string path)
        {
            string folder = Path.GetDirectoryName(path);
            if (AssemblyReferences.TryGetValue(folder, out var references)) return references;
            string target = Path.GetFileNameWithoutExtension(CompilationPipeline.GetAssemblyNameFromScriptPath(path));
            _playerAssemblies ??= CompilationPipeline.GetAssemblies(AssembliesType.Player);
            var assembly = _playerAssemblies.FirstOrDefault(a => a.name == target);
            return AssemblyReferences[folder] = (target, assembly?.assemblyReferences.Select(a => a.name).Append(target).ToHashSet());
        }

        internal const string PendingKey = "DODBT.Light.PendingScriptCreation";
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void Reloaded() => SessionState.EraseString(PendingKey);

        internal static bool Busy => EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorUtility.scriptCompilationFailed || File.Exists(SessionState.GetString(PendingKey, ""));

        internal static Type[] Types() => AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly =>
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException error) { return error.Types.Where(type => type != null); }
        }).ToArray();

        internal static bool NameExists(string name, string space, Type[] types)
        {
            string fullName = LeafSourceTemplate.FullName(name, space);
            return types.Any(type => type.FullName == fullName || type.Namespace == fullName ||
                type.Namespace?.StartsWith(fullName + ".", StringComparison.Ordinal) == true);
        }

        internal static string Validate(string path, string name, string space, params Type[] dependencies)
        {
            if (!LeafSourceTemplate.Identifier(name) || !string.IsNullOrEmpty(space) && !space.Split('.').All(LeafSourceTemplate.Identifier))
                return "Use valid C# names for the type and namespace.";
            string error = BtCreationSettings.GraphNameError(name.TrimStart('@'));
            if (error != null) return error;
            if (!BtCreationSettings.TryAssetFolder(Path.GetDirectoryName(path), out _)) return "Choose an existing folder inside Assets.";
            if (File.Exists(path) || Directory.Exists(path)) return "A file with this name already exists.";
            var (target, references) = ReferencesFor(path);
            if (references == null) return "Choose a folder in a runtime assembly, outside Editor.";
            if (!references.Contains(typeof(ILeaf).Assembly.GetName().Name)) return "The selected assembly must reference DODBT-Light.";
            foreach (var type in dependencies)
            {
                if (type == null) return "Choose a context and leaf state.";
                if (type.Assembly.GetName().Name != target && (!type.IsVisible || !references.Contains(type.Assembly.GetName().Name)))
                    return "The selected assembly cannot access " + type.FullName + ". Add a reference or choose another folder.";
                for (var nested = type; nested != null; nested = nested.DeclaringType)
                    if (nested.IsNestedPrivate || nested.IsNestedFamily || nested.IsNestedFamANDAssem)
                        return "The selected type is not accessible from a new leaf.";
            }
            return null;
        }

        internal static string Write(string path, string source)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(stream)) writer.Write(source);
            }
            catch (IOException error) { return error.Message; }
            catch (UnauthorizedAccessException error) { return error.Message; }
            SessionState.SetString(PendingKey, path);
            AssetDatabase.ImportAsset(path);
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
            return null;
        }
    }
}
