using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using VadimBurym.DodBehaviourTree.PlayerSmoke;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Tests
{
    public static class BuildVerification
    {
        [MenuItem("Tools/VadimBurym/Verification/Open temporary example graph", true)]
        private static bool CanOpenTemporaryGraph() =>
            AssetDatabase.LoadAssetAtPath<BtGraphAsset>("Assets/_ExampleProject/Content/AI/BrainEnemy_01_Graph.asset") != null;

        [MenuItem("Tools/VadimBurym/Verification/Open temporary example graph")]
        public static void OpenTemporaryGraph()
        {
            var original = AssetDatabase.LoadAssetAtPath<BtGraphAsset>("Assets/_ExampleProject/Content/AI/BrainEnemy_01_Graph.asset");
            if (original == null) throw new InvalidOperationException("The example graph was not found.");
            var copy = Object.Instantiate(original);
            copy.name = "Temporary verification graph";
            copy.hideFlags = HideFlags.DontSave;
            copy.CompiledAsset = Object.Instantiate(original.CompiledAsset);
            copy.CompiledAsset.hideFlags = HideFlags.DontSave;
            BtEditorWindow.OpenWithAsset(copy);
        }

        public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Builds/WindowsSmoke/DODBT.exe");
        public static void WebGL() => Build(BuildTarget.WebGL, "Builds/WebGLSmoke");

        private static void Build(BuildTarget target, string output)
        {
            string folder = "Assets/DODBT_BuildTemp_" + Guid.NewGuid().ToString("N");
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
            var oldStripping = PlayerSettings.GetManagedStrippingLevel(namedTarget);
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            try
            {
                AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
                AssetDatabase.CreateFolder(folder, "Resources");
                var texture = new Texture2D(2, 2);
                AssetDatabase.CreateAsset(texture, folder + "/Texture.asset");
                var value = new SmokeValue { StoredNumber = 17 };
                value.Next = value;
                var leaf = new SmokeLeaf { Number = 42, Texture = texture, Value = value, Alias = value, RuntimeOnly = new object() };
                leaf.Values.Add(value);
                graph.CompiledAsset = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
                AssetDatabase.CreateAsset(graph.CompiledAsset, folder + "/Resources/DODBT_SmokeTree.asset");
                graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "leaf" };
                graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Kind = BtNodeKind.Leaf, Title = "Smoke" });
                graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = leaf });
                string error = BtCompiler.TryCompileAsset(graph, graph.CompiledAsset);
                if (!string.IsNullOrEmpty(error)) throw new BuildFailedException(error);
                CreateRuntimeAuditAssets(folder);
#if DODBT_DECORATORS
                CreateDecoratorAssets(folder);
#endif
                PlayerSettings.SetManagedStrippingLevel(namedTarget, ManagedStrippingLevel.High);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    target = target,
                    locationPathName = output,
                    extraScriptingDefines = new[] { "DODBT_PLAYER_SMOKE" },
                    options = BuildOptions.None
                });
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/BuildVerification-" + target + ".txt",
                    $"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nBytes: {report.summary.totalSize}\nStripping: High\n");
                if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors > 0)
                    throw new BuildFailedException(report.summary.result + ": " + report.summary.totalErrors + " errors");
            }
            finally
            {
                PlayerSettings.SetManagedStrippingLevel(namedTarget, oldStripping);
                Object.DestroyImmediate(graph);
                AssetDatabase.DeleteAsset(folder);
            }
        }

        private static void CreateRuntimeAuditAssets(string folder)
        {
            for (int mode = 0; mode < 5; mode++)
            {
                var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
                try
                {
                    graph.CompiledAsset = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
                    string name = mode < 4 ? "DODBT_SelectorSmoke_" + mode : "DODBT_ParallelSmoke";
                    AssetDatabase.CreateAsset(graph.CompiledAsset, folder + "/Resources/" + name + ".asset");
                    graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "outer" };
                    if (mode < 4)
                    {
                        graph.Nodes.Add(new BtNodeHeader { Guid = "outer", Kind = BtNodeKind.MemorySelector });
                        graph.Nodes.Add(new BtNodeHeader { Guid = "inner", Kind = BtNodeKind.MemorySelector });
                        graph.MemorySelectorNodes.Add(new BtMemorySelectorNodeData { Guid = "outer", PickRandom = (mode & 1) != 0, ChildrenGuids = new() { "inner", "leaf0", "leaf1" } });
                        graph.MemorySelectorNodes.Add(new BtMemorySelectorNodeData { Guid = "inner", PickRandom = (mode & 2) != 0, ChildrenGuids = new() { "leaf2", "leaf3", "leaf4" } });
                        for (int i = 0; i < 5; i++) AddLeaf(graph, i, NodeStatus.Failure, NodeStatus.Failure, NodeStatus.Failure);
                    }
                    else
                    {
                        graph.Nodes.Add(new BtNodeHeader { Guid = "outer", Kind = BtNodeKind.Parallel });
                        graph.Nodes.Add(new BtNodeHeader { Guid = "inner", Kind = BtNodeKind.MemorySequence });
                        graph.ParallelNodes.Add(new BtParallelNodeData { Guid = "outer", CacheChildStatus = false, SuccessThreshold = 2, FailureThreshold = 2, ChildrenGuids = new() { "inner", "leaf2" } });
                        graph.MemorySequenceNodes.Add(new BtMemorySequenceNodeData { Guid = "inner", ResetOnFailure = false, ResetOnAbort = true, ChildrenGuids = new() { "leaf0", "leaf1" } });
                        AddLeaf(graph, 0, NodeStatus.Success, NodeStatus.Success, NodeStatus.Success);
                        AddLeaf(graph, 1, NodeStatus.Running, NodeStatus.Failure, NodeStatus.Success);
                        AddLeaf(graph, 2, NodeStatus.Running, NodeStatus.Running, NodeStatus.Running);
                    }
                    string error = BtCompiler.TryCompileAsset(graph, graph.CompiledAsset);
                    if (!string.IsNullOrEmpty(error)) throw new BuildFailedException(error);
                }
                finally { Object.DestroyImmediate(graph); }
            }
        }

        private static void AddLeaf(BtGraphAsset graph, int slot, NodeStatus first, NodeStatus second, NodeStatus later)
        {
            string guid = "leaf" + slot;
            graph.Nodes.Add(new BtNodeHeader { Guid = guid, Kind = BtNodeKind.Leaf, Title = guid });
            graph.LeafNodes.Add(new BtLeafNodeData { Guid = guid, Leaf = new SmokeTraversalLeaf { Slot = slot, First = first, Second = second, Later = later } });
        }

#if DODBT_DECORATORS
        private static void CreateDecoratorAssets(string folder)
        {
            foreach (var kind in new[] { BtNodeKind.Timeout, BtNodeKind.Cooldown, BtNodeKind.Inverter })
            {
                var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
                try
                {
                    graph.CompiledAsset = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
                    AssetDatabase.CreateAsset(graph.CompiledAsset, folder + "/Resources/DODBT_" + kind + "Smoke.asset");
                    graph.RootNode = new BtRootNodeData { ChildrenGuid = "decorator" };
                    graph.Nodes.Add(new BtNodeHeader { Guid = "decorator", Kind = kind });
                    graph.DecoratorNodes.Add(new BtDecoratorNodeData { Guid = "decorator", Duration = 2, ChildrenGuids = new() { "leaf0" } });
                    AddLeaf(graph, 0, NodeStatus.Running, NodeStatus.Success, NodeStatus.Running);
                    string error = BtCompiler.TryCompileAsset(graph, graph.CompiledAsset);
                    if (!string.IsNullOrEmpty(error)) throw new BuildFailedException(error);
                }
                finally { Object.DestroyImmediate(graph); }
            }
        }
#endif
    }
}
