// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{

    using System.IO;
    using UnityEditor;

    internal static class BtEditorPaths
    {
        internal static string GetCompiledAssetPath(BtGraphAsset graph)
        {
            string graphPath = AssetDatabase.GetAssetPath(graph);
            if (string.IsNullOrEmpty(graphPath))
                throw new System.InvalidOperationException("Save the graph before creating its compiled asset.");
            string folder = Path.GetDirectoryName(graphPath).Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(graphPath);
            return AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}_Compiled.asset");
        }

        internal static string GetEditorWindowFolderPath()
        {
            foreach (string guid in AssetDatabase.FindAssets("BtEditorWindow t:MonoScript"))
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (script != null && script.GetClass() == typeof(BtEditorWindow))
                    return Path.GetDirectoryName(scriptPath)?.Replace('\\', '/') ?? "Assets";
            }
            return "Assets";
        }

    }
}
