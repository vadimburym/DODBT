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
                if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException(report.summary.result.ToString());
            }
            finally
            {
                PlayerSettings.SetManagedStrippingLevel(namedTarget, oldStripping);
                Object.DestroyImmediate(graph);
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
