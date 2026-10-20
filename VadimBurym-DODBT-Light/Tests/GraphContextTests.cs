// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using VadimBurym.DodBehaviourTree.Light.Editor;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Light.Tests
{
    public sealed class GraphContextTests
    {
        private readonly List<Object> _objects = new();
        private string _folder;

        private BtGraphAsset Graph(ILeaf leaf)
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            var compiled = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
            _objects.Add(graph);
            _objects.Add(compiled);
            graph.CompiledAsset = compiled;
            graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "leaf" };
            graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Kind = BtNodeKind.Leaf, Title = "Leaf" });
            graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = leaf });
            return graph;
        }

        [TearDown]
        public void Cleanup()
        {
            Undo.ClearAll();
            foreach (var obj in _objects)
                if (obj != null && !EditorUtility.IsPersistent(obj)) Object.DestroyImmediate(obj);
            _objects.Clear();
            if (_folder != null && AssetDatabase.IsValidFolder(_folder)) AssetDatabase.DeleteAsset(_folder);
        }

        [UnityTest]
        public IEnumerator LeafInspectorIsVisibleInsideTheNodeInEditAndDebugModes()
        {
            var graph = Graph(new ContextLeaf());
            BtGraphContext.BindLegacy(graph);
            var host = ScriptableObject.CreateInstance<InspectorHost>();
            var views = new[] { new BtGraphView(false), new BtGraphView(true) };
            try
            {
                host.Show();
                host.position = new Rect(20, 20, 900, 700);
                foreach (var view in views)
                {
                    host.rootVisualElement.Clear();
                    host.rootVisualElement.Add(view);
                    view.Bind(graph);
                    yield return null;
                    yield return null;
                    yield return null;
                    var node = view.Query<BtNodeView>().ToList().Single(n => n.Guid == "leaf");
                    var inspector = node.extensionContainer.Q<IMGUIContainer>("inline-node-inspector");
                    Assert.That(inspector, Is.Not.Null, "The Leaf UI must be built as well as the Details inspector.");
                    Assert.That(inspector.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                    Assert.That(inspector.layout.height, Is.GreaterThan(40), "Leaf selector and serialized TargetShots must be visible.");
                }
            }
            finally
            {
                foreach (var view in views) view.DisposeInspectors();
                host.Close();
            }
        }

        [Test]
        public void ChangingOnlyStatePreservesContextAndRejectsOldLeaf()
        {
            var graph = Graph(new ContextLeaf());
            BtGraphContext.BindLegacy(graph);
            BtGraphContext.Set(graph, typeof(ContextA), typeof(StateB));
            Assert.That(BtGraphContext.Resolve(graph), Is.EqualTo(typeof(ContextA)));
            Assert.That(BtGraphContext.AcceptsLeaf(graph, typeof(ContextLeaf)), Is.False);
            Assert.That(BtGraphContext.AcceptsLeaf(graph, typeof(OtherStateLeaf)), Is.True);
            Assert.That(BtGraphContext.Warning(graph), Does.Contain("context/state"));
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Not.Empty);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(BtGraphContext.ResolveState(graph), Is.EqualTo(typeof(StateA)));
        }

        [Test]
        public void CreationRejectsTypeNamesThatHideNamespaces()
        {
            Assert.That(BtScriptCreation.NameExists("VadimBurym", "", new[] { typeof(BtContextAttribute) }), Is.True);
            Assert.That(BtScriptCreation.NameExists("NewContext", "Game.AI", new[] { typeof(BtContextAttribute) }), Is.False);
        }

        private sealed class InspectorHost : EditorWindow { }

        [Test]
        public void DebugAttachReusesComponentAndRebindsStateWithoutCopying()
        {
            var owner = new GameObject("Agent");
            var compiled = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
            _objects.Add(owner);
            _objects.Add(compiled);
            var first = new BtState<StateA>();
            var second = new BtState<StateA>();
            var debug = BtMonoDebug.Attach(owner, compiled, first);
            Assert.That(debug.DebugStatus, Is.SameAs(((IBtDebugState)first).DebugStatus));
            Assert.That(BtMonoDebug.Attach(owner, compiled, second), Is.SameAs(debug));
            Assert.That(owner.GetComponents<BtMonoDebug>(), Has.Length.EqualTo(1));
            Assert.That(debug.RunningLeafs, Is.SameAs(((IBtDebugState)second).DebugRunningLeafs));
            debug.Detach();
            Assert.That(debug.BehaviourTreeAsset, Is.Null);
            Assert.That(debug.DebugStatus, Is.Null);
            Assert.That(debug.RunningLeafs, Is.Null);
            Assert.That(debug.CompiledRevision, Is.Null);
            Assert.That(BtMonoDebug.Attach(owner, compiled, first), Is.SameAs(debug));
            Assert.That(debug.RunningLeafs, Is.SameAs(((IBtDebugState)first).DebugRunningLeafs));
            Assert.Throws<ArgumentNullException>(() => BtMonoDebug.Attach(owner, compiled, null));
        }

        [Test]
        public void LegacyGraphInfersBothTypesAndFiltersByState()
        {
            var graph = Graph(new ContextLeaf());
            BtGraphContext.BindLegacy(graph);
            Assert.That(BtGraphContext.Resolve(graph), Is.EqualTo(typeof(ContextA)));
            Assert.That(BtGraphContext.ResolveState(graph), Is.EqualTo(typeof(StateA)));
            Assert.That(BtGraphContext.AcceptsLeaf(graph, typeof(OtherStateLeaf)), Is.False);
            Assert.That(BtGraphContext.AcceptsLeaf(graph, typeof(OtherContextLeaf)), Is.False);
        }

        [Test]
        public void MixedDetachedLeafDoesNotSilentlyPickAContext()
        {
            var graph = Graph(new ContextLeaf());
            graph.LeafNodes.Add(new BtLeafNodeData { Guid = "detached", Leaf = new OtherContextLeaf() });
            Assert.That(BtGraphContext.Resolve(graph), Is.Null);
            BtGraphContext.Set(graph, typeof(ContextA), typeof(StateA));
            Assert.That(BtGraphContext.Warning(graph), Does.Contain("1 leaf"));
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Empty);
        }

        [Test]
        public void MultipleInterfacesRequireUnambiguousPair()
        {
            var graph = Graph(new MultiLeaf());
            Assert.That(BtGraphContext.Resolve(graph), Is.Null);
            BtGraphContext.Set(graph, typeof(ContextA), typeof(StateA));
            Assert.That(BtGraphContext.AcceptsLeaf(graph, typeof(MultiLeaf)), Is.True);
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Empty);
        }

        [Test]
        public void WrongStateIsRejectedWithoutOverwritingCompiledTree()
        {
            var graph = Graph(new ContextLeaf());
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Empty);
            string revision = graph.CompiledRevision;
            var leaves = graph.CompiledAsset.Leafs;
            graph.LeafNodes[0].Leaf = new OtherStateLeaf();
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Does.Contain("context and leaf state"));
            Assert.That(graph.CompiledRevision, Is.EqualTo(revision));
            Assert.That(graph.CompiledAsset.Leafs, Is.SameAs(leaves));
        }

        [Test]
        public void MissingTypeDoesNotFallBackToInference()
        {
            var graph = Graph(new ContextLeaf());
            graph.ContextIdentity = "RemovedAssembly:Missing.Context";
            graph.LeafStateIdentity = BtGraphContext.Identity(typeof(StateA));
            Assert.That(BtGraphContext.Resolve(graph), Is.Null);
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Does.Contain("missing"));
        }

        [Test]
        public void ChangingPairPreservesLeafAndSupportsUndo()
        {
            var leaf = new ContextLeaf();
            var graph = Graph(leaf);
            BtGraphContext.BindLegacy(graph);
            BtGraphContext.Set(graph, typeof(ContextB), typeof(StateA));
            Assert.That(graph.LeafNodes[0].Leaf, Is.SameAs(leaf));
            Assert.That(graph.NotActualCompiledVersion, Is.True);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(BtGraphContext.Resolve(graph), Is.EqualTo(typeof(ContextA)));
        }

        [Test]
        public void MismatchOutlineAndWarningDisappearWhenPairMatches()
        {
            var graph = Graph(new ContextLeaf());
            BtGraphContext.Set(graph, typeof(ContextB), typeof(StateA));
            var view = new BtGraphView(false);
            try
            {
                view.Bind(graph);
                Assert.That(view.Q<VisualElement>("context-warning-outline").style.display.value, Is.Not.EqualTo(DisplayStyle.None));
                Assert.That(view.Q<Image>("context-warning-icon"), Is.Not.Null);
                BtGraphContext.Set(graph, typeof(ContextA), typeof(StateA));
                view.ReloadWithoutLog();
                Assert.That(view.Query<VisualElement>("context-warning-outline").ToList().All(e => e.style.display.value == DisplayStyle.None), Is.True);
            }
            finally { view.DisposeInspectors(); }
        }

        [Test]
        public void CompiledPathIsNextToGraphAndLinksSurviveMovingBothAssets()
        {
            _folder = "Assets/LightAuthoring_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", _folder.Substring(7));
            var graph = Graph(new ContextLeaf());
            AssetDatabase.CreateAsset(graph, _folder + "/Brain.asset");
            string compiledPath = BtEditorPaths.GetCompiledAssetPath(graph);
            Assert.That(compiledPath, Is.EqualTo(_folder + "/Brain_Compiled.asset"));
            AssetDatabase.CreateAsset(graph.CompiledAsset, compiledPath);
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Empty);
            string graphGuid = AssetDatabase.AssetPathToGUID(_folder + "/Brain.asset");
            AssetDatabase.CreateFolder(_folder, "Moved");
            Assert.That(AssetDatabase.MoveAsset(_folder + "/Brain.asset", _folder + "/Moved/Renamed.asset"), Is.Empty);
            Assert.That(AssetDatabase.MoveAsset(compiledPath, _folder + "/DifferentCompiledName.asset"), Is.Empty);
            Assert.That(graph.CompiledAsset.GUID, Is.EqualTo(graphGuid));
            Assert.That(AssetDatabase.GUIDToAssetPath(graphGuid), Is.EqualTo(_folder + "/Moved/Renamed.asset"));
            Assert.That(graph.CompiledAsset, Is.Not.Null);
        }

        [Serializable] public sealed class ContextA { }
        [Serializable] public sealed class ContextB { }
        public struct StateA { }
        public struct StateB { }
        [Serializable] public class ContextLeaf : ILeaf<ContextA, StateA>
        {
            public int TargetShots = 3;
            public NodeStatus OnTick(ContextA context, ref StateA state) => NodeStatus.Running;
            public void OnEnter(ContextA context, ref StateA state) { }
            public void OnExit(ContextA context, ref StateA state, NodeStatus exitStatus) { }
            public void OnAbort(ContextA context, ref StateA state) { }
        }
        [Serializable] public sealed class OtherStateLeaf : ILeaf<ContextA, StateB>
        {
            public NodeStatus OnTick(ContextA context, ref StateB state) => NodeStatus.Running;
            public void OnEnter(ContextA context, ref StateB state) { }
            public void OnExit(ContextA context, ref StateB state, NodeStatus exitStatus) { }
            public void OnAbort(ContextA context, ref StateB state) { }
        }
        [Serializable] public class OtherContextLeaf : ILeaf<ContextB, StateA>
        {
            public NodeStatus OnTick(ContextB context, ref StateA state) => NodeStatus.Running;
            public void OnEnter(ContextB context, ref StateA state) { }
            public void OnExit(ContextB context, ref StateA state, NodeStatus exitStatus) { }
            public void OnAbort(ContextB context, ref StateA state) { }
        }
        [Serializable] public sealed class MultiLeaf : ContextLeaf, ILeaf<ContextB, StateA>
        {
            public NodeStatus OnTick(ContextB context, ref StateA state) => NodeStatus.Running;
            public void OnEnter(ContextB context, ref StateA state) { }
            public void OnExit(ContextB context, ref StateA state, NodeStatus exitStatus) { }
            public void OnAbort(ContextB context, ref StateA state) { }
        }
    }
}
