// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using System;
using System.IO;
using UnityEditor;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public static class CompiledBindingVerification
    {
        public static void RecompileProjectTrees()
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:BtGraphAsset"))
            {
                var graph = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (graph.CompiledAsset == null) continue;
                string error = BtCompiler.TryCompileAsset(graph, graph.CompiledAsset);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(graph.name + ": " + error);
                using var blob = graph.CompiledAsset.CreateBlob();
                count++;
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/Leaf-Binding-Migration.txt", "Recompiled and validated " + count + " trees.");
        }
    }
}
