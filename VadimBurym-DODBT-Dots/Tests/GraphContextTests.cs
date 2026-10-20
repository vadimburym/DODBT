// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class GraphContextTests
    {
        private BtGraphAsset _graph;

        [SetUp]
        public void Setup()
        {
            _graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            _graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "leaf" };
            _graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Title = "Original", Kind = BtNodeKind.Leaf, ParentGuid = "root" });
            _graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = new SnapshotTestLeaf { Number = 42 } });
        }

        [TearDown]
        public void Teardown() => Object.DestroyImmediate(_graph);

        [Test]
        public void LegacyBinding_PreservesCompiledStatusAndSurvivesSerialization()
        {
            BtGraphContext.BindLegacy(_graph);
            Assert.That(BtGraphContext.Resolve(_graph), Is.EqualTo(typeof(AuditContext)));
            Assert.That(_graph.NotActualCompiledVersion, Is.False);
            var copy = ScriptableObject.CreateInstance<BtGraphAsset>();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(_graph), copy);
                Assert.That(copy.ContextIdentity, Is.EqualTo(_graph.ContextIdentity));
                Assert.That(BtGraphContext.Resolve(copy), Is.EqualTo(typeof(AuditContext)));
            }
            finally { Object.DestroyImmediate(copy); }
        }

        [Test]
        public void MixedLegacyGraph_DoesNotGuessContextFromTheFirstLeaf()
        {
            AddDetachedForeignLeaf();
            BtGraphContext.BindLegacy(_graph);
            Assert.That(_graph.ContextIdentity, Is.Null.Or.Empty);
            Assert.That(BtGraphValidation.Validate(_graph), Does.Contain("Select a graph context"));
        }

        [Test]
        public void ContextChange_PreservesFieldsAndSupportsUndoRedo()
        {
            BtGraphContext.BindLegacy(_graph);
            BtGraphContext.Set(_graph, typeof(BenchmarkContext));
            Assert.That(((SnapshotTestLeaf)_graph.LeafNodes[0].Leaf).Number, Is.EqualTo(42));
            Assert.That(_graph.NotActualCompiledVersion, Is.True);
            Assert.That(BtGraphContext.Warning(_graph), Does.Contain("Original"));
            Undo.PerformUndo();
            Assert.That(BtGraphContext.Resolve(_graph), Is.EqualTo(typeof(AuditContext)));
            Undo.PerformRedo();
            Assert.That(BtGraphContext.Resolve(_graph), Is.EqualTo(typeof(BenchmarkContext)));
        }

        [Test]
        public void CompileRejectsMismatch_WithoutOverwritingPreviousRuntimeAsset()
        {
            var compiled = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
            try
            {
                Assert.That(BtCompiler.TryCompileAsset(_graph, compiled), Is.Empty);
                string previous = EditorJsonUtility.ToJson(compiled);
                BtGraphContext.Set(_graph, typeof(BenchmarkContext));
                Assert.That(BtCompiler.TryCompileAsset(_graph, compiled), Does.Contain("Original").And.Contain("BenchmarkContext"));
                Assert.That(EditorJsonUtility.ToJson(compiled), Is.EqualTo(previous));
            }
            finally { Object.DestroyImmediate(compiled); }
        }

        [Test]
        public void MissingContext_DoesNotSilentlyRebindToExistingLeaves()
        {
            _graph.ContextIdentity = "Deleted.Assembly:Game.DeletedContext";
            BtGraphContext.BindLegacy(_graph);
            Assert.That(BtGraphContext.Resolve(_graph), Is.Null);
            Assert.That(BtGraphValidation.Validate(_graph), Does.Contain("missing"));
            Assert.That(_graph.LeafNodes[0].Leaf, Is.TypeOf<SnapshotTestLeaf>());
        }

        [Test]
        public void DetachedForeignLeaf_WarnsButDoesNotPreventCompilingTheRootBranch()
        {
            BtGraphContext.BindLegacy(_graph);
            AddDetachedForeignLeaf();
            Assert.That(BtGraphContext.Warning(_graph), Does.Contain("Foreign"));
            Assert.That(BtGraphValidation.Validate(_graph), Is.Empty);
        }

        [Test]
        public void ForeignClipboard_PreservesDataButCannotCompileAfterConnection()
        {
            BtGraphContext.Set(_graph, typeof(BenchmarkContext));
            var destination = ScriptableObject.CreateInstance<BtGraphAsset>();
            try
            {
                destination.ContextIdentity = BtGraphContext.Identity(typeof(BenchmarkContext));
                var ids = BtGraphClipboard.Paste(destination, BtGraphClipboard.Copy(_graph, new[] { "leaf" }), Vector2.zero);
                Assert.That(BtGraphContext.Warning(destination), Does.Contain("Original"));
                destination.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = ids.Single() };
                destination.Nodes.Single().ParentGuid = "root";
                Assert.That(((SnapshotTestLeaf)destination.LeafNodes.Single().Leaf).Number, Is.EqualTo(42));
                Assert.That(BtGraphValidation.Validate(destination), Does.Contain("BenchmarkContext"));
            }
            finally { Object.DestroyImmediate(destination); }
        }

        [Test]
        public void PickerRejectsStaleLeafSelection_ButNestedReferencesRemainEditable()
        {
            BtGraphContext.Set(_graph, typeof(BenchmarkContext));
            Assert.That(BtGraphContext.AcceptsLeaf(_graph, typeof(BenchmarkLeaf)), Is.True);
            Assert.That(BtGraphContext.AcceptsLeaf(_graph, typeof(SnapshotTestLeaf)), Is.False);
            using var fields = new BtSerializedFields(_graph, path => path, () => { });
            var assign = typeof(BtSerializedFields).GetMethod("Assign", BindingFlags.Instance | BindingFlags.NonPublic);
            const string root = "LeafNodes.Array.data[0].Leaf";
            assign.Invoke(fields, new object[] { root, typeof(SnapshotTestLeaf), true });
            Assert.That(((SnapshotTestLeaf)_graph.LeafNodes[0].Leaf).Number, Is.EqualTo(42));
            assign.Invoke(fields, new object[] { root + ".Value", typeof(SnapshotTestValue), false });
            Assert.That(((SnapshotTestLeaf)_graph.LeafNodes[0].Leaf).Value, Is.TypeOf<SnapshotTestValue>());
            assign.Invoke(fields, new object[] { root, typeof(BenchmarkLeaf), true });
            Assert.That(_graph.LeafNodes[0].Leaf, Is.TypeOf<BenchmarkLeaf>());
        }

        [Test]
        public void NodeShowsMismatch_AndClearsItWhenContextMatchesAgain()
        {
            BtGraphContext.Set(_graph, typeof(BenchmarkContext));
            var view = new BtGraphView(false);
            try
            {
                view.Bind(_graph);
                var warning = view.Query<Label>().ToList().Single(label => label.text == "Context mismatch");
                Assert.That(warning.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                BtGraphContext.Set(_graph, typeof(AuditContext));
                view.ReloadWithoutLog();
                Assert.That(view.Query<Label>().ToList().Where(label => label.text == "Context mismatch")
                    .All(label => label.style.display.value == DisplayStyle.None), Is.True);
            }
            finally { view.DisposeInspectors(); }
        }

        private void AddDetachedForeignLeaf()
        {
            _graph.Nodes.Add(new BtNodeHeader { Guid = "foreign", Title = "Foreign", Kind = BtNodeKind.Leaf });
            _graph.LeafNodes.Add(new BtLeafNodeData { Guid = "foreign", Leaf = new BenchmarkLeaf() });
        }
    }
}
