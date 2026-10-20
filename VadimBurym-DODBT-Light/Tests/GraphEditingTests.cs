// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Light.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Light.Tests
{
    internal sealed class GraphEditingTests
    {
        private readonly List<Object> _objects = new();

        private BtGraphAsset Graph()
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            _objects.Add(graph);
            graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "sequence" };
            graph.Nodes.Add(new BtNodeHeader { Guid = "sequence", Kind = BtNodeKind.Sequence, Title = "Patrol", Position = new Vector2(0, 0), ParentGuid = "root" });
            graph.Nodes.Add(new BtNodeHeader { Guid = "left", Kind = BtNodeKind.Leaf, Title = "Find Target", Position = new Vector2(-100, 100), ParentGuid = "sequence" });
            graph.Nodes.Add(new BtNodeHeader { Guid = "right", Kind = BtNodeKind.Leaf, Title = "Move To Target", Position = new Vector2(100, 100), ParentGuid = "sequence" });
            graph.SequenceNodes.Add(new BtSequenceNodeData { Guid = "sequence", ChildrenGuids = new() { "left", "right" } });
            var value = new SnapshotTestValue { Number = 17 };
            value.Next = value;
            var texture = new Texture2D(1, 1);
            _objects.Add(texture);
            graph.LeafNodes.Add(new BtLeafNodeData { Guid = "left", Leaf = new SnapshotTestLeaf { Number = 42, Value = value, Alias = value, Texture = texture } });
            graph.LeafNodes.Add(new BtLeafNodeData { Guid = "right", Leaf = new SnapshotTestLeaf { Number = 99 } });
            return graph;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var obj in _objects)
            {
                if (obj is BtGraphAsset) Undo.ClearUndo(obj);
                Object.DestroyImmediate(obj);
            }
            _objects.Clear();
        }

        [Test]
        public void Clipboard_BlockPreservesInternalOrderAndCreatesIndependentNativeSnapshots()
        {
            var source = Graph();
            string data = BtGraphClipboard.Copy(source, new[] { "sequence", "left", "right" });
            var original = (SnapshotTestLeaf)source.LeafNodes[0].Leaf;
            var added = BtGraphClipboard.Paste(source, data, new Vector2(40, 40));
            Assert.That(added.Count, Is.EqualTo(3));
            Assert.That(added.Intersect(new[] { "sequence", "left", "right" }), Is.Empty);
            var group = source.FindHeader(added[0]);
            Assert.That(group.ParentGuid, Is.Null);
            Assert.That(group.Position, Is.EqualTo(new Vector2(40, 40)));
            Assert.That(source.FindSequenceData(added[0]).ChildrenGuids, Is.EqualTo(added.Skip(1).ToArray()));
            Assert.That(source.FindHeader(added[1]).ParentGuid, Is.EqualTo(added[0]));
            Assert.That(source.FindHeader(added[1]).Position.x, Is.LessThan(source.FindHeader(added[2]).Position.x));
            var clone = (SnapshotTestLeaf)source.FindLeafData(added[1]).Leaf;
            Assert.That(clone.Number, Is.EqualTo(42));
            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Value, Is.Not.SameAs(original.Value));
            Assert.That(clone.Value, Is.SameAs(clone.Alias));
            Assert.That(((SnapshotTestValue)clone.Value).Next, Is.SameAs(clone.Value));
            Assert.That(clone.Texture, Is.SameAs(original.Texture));
            clone.Number = 2;
            Assert.That(original.Number, Is.EqualTo(42));
            Assert.That(source.RootNode.ChildrenGuid, Is.EqualTo("sequence"));
        }

        [Test]
        public void Clipboard_SingleNodeHasNoExternalConnectionsAndPasteCanBeUndoneAsOneOperation()
        {
            var graph = Graph();
            var added = BtGraphClipboard.Paste(graph, BtGraphClipboard.Copy(graph, new[] { "left" }), Vector2.one);
            Assert.That(graph.FindHeader(added[0]).ParentGuid, Is.Null);
            Assert.That(graph.FindSequenceData("sequence").ChildrenGuids.Count, Is.EqualTo(2));
            Assert.That(graph.Nodes.Count, Is.EqualTo(4));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(graph.Nodes.Count, Is.EqualTo(3));
            Assert.That(graph.FindHeader(added[0]), Is.Null);
            Undo.PerformRedo();
            Assert.That(graph.FindHeader(added[0]), Is.Not.Null);
            Assert.That(graph.Nodes.Count, Is.EqualTo(4));
        }

        [Test]
        public void Clipboard_CompositeAloneDoesNotCopyUnselectedChildrenAndRejectsForeignData()
        {
            var graph = Graph();
            var added = BtGraphClipboard.Paste(graph, BtGraphClipboard.Copy(graph, new[] { "sequence" }), Vector2.zero);
            Assert.That(graph.FindSequenceData(added[0]).ChildrenGuids, Is.Empty);
            int before = graph.Nodes.Count;
            Assert.Throws<InvalidOperationException>(() => BtGraphClipboard.Paste(graph, "unrelated clipboard", Vector2.zero));
            Assert.That(graph.Nodes.Count, Is.EqualTo(before));
            Assert.That(BtGraphClipboard.Copy(graph, new[] { "root" }), Is.Empty);
        }

        [Test]
        public void Clipboard_CanPasteIntoAnotherGraphAndRepeatWithDifferentGuids()
        {
            var source = Graph();
            var destination = Graph();
            string text = BtGraphClipboard.Copy(source, new[] { "left" });
            var first = BtGraphClipboard.Paste(destination, text, Vector2.zero);
            var second = BtGraphClipboard.Paste(destination, text, Vector2.one);
            Assert.That(first[0], Is.Not.EqualTo(second[0]));
            Assert.That(destination.FindLeafData(first[0]).Leaf, Is.Not.SameAs(destination.FindLeafData(second[0]).Leaf));
            Assert.That(source.Nodes.Count, Is.EqualTo(3));
        }

        [TestCase("cycle")]
        [TestCase("duplicate-child")]
        [TestCase("shared-child")]
        [TestCase("parent-mismatch")]
        [TestCase("nonfinite-position")]
        public void Clipboard_InvalidTopologyIsRejectedBeforeChangingDestination(string fault)
        {
            var source = Graph();
            var destination = Graph();
            if (fault == "cycle") source.FindSequenceData("sequence").ChildrenGuids.Add("sequence");
            if (fault == "duplicate-child") source.FindSequenceData("sequence").ChildrenGuids.Add("left");
            if (fault == "parent-mismatch") source.FindHeader("left").ParentGuid = "right";
            if (fault == "nonfinite-position") source.FindHeader("left").Position = new Vector2(float.PositiveInfinity, 0);
            if (fault == "shared-child")
            {
                source.Nodes.Add(new BtNodeHeader { Guid = "other", Kind = BtNodeKind.Sequence });
                source.SequenceNodes.Add(new BtSequenceNodeData { Guid = "other", ChildrenGuids = new() { "left" } });
            }
            string text = BtGraphClipboard.Copy(source, source.Nodes.Select(n => n.Guid));
            string before = EditorJsonUtility.ToJson(destination);
            Assert.Throws<InvalidOperationException>(() => BtGraphClipboard.Paste(destination, text, Vector2.zero));
            Assert.That(EditorJsonUtility.ToJson(destination), Is.EqualTo(before));
        }

        [Test]
        public void Clipboard_PersistentAssetReferenceSurvivesRoundTrip()
        {
            var graph = Graph();
            var texture = ((SnapshotTestLeaf)graph.LeafNodes[0].Leaf).Texture;
            string path = "Assets/DODBT_ClipboardTest_" + Guid.NewGuid().ToString("N") + ".asset";
            try
            {
                AssetDatabase.CreateAsset(texture, path);
                AssetDatabase.SaveAssets();
                var added = BtGraphClipboard.Paste(graph, BtGraphClipboard.Copy(graph, new[] { "left" }), Vector2.zero);
                var copiedTexture = ((SnapshotTestLeaf)graph.FindLeafData(added[0]).Leaf).Texture;
                Assert.That(AssetDatabase.GetAssetPath(copiedTexture), Is.EqualTo(path));
                Assert.That(GlobalObjectId.GetGlobalObjectIdSlow(copiedTexture).ToString(),
                    Is.EqualTo(GlobalObjectId.GetGlobalObjectIdSlow(texture).ToString()));
            }
            finally
            {
                _objects.Remove(texture);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void Clipboard_MissingObjectRejectsPasteBeforeMutatingDestination()
        {
            var graph = Graph();
            var texture = ((SnapshotTestLeaf)graph.LeafNodes[0].Leaf).Texture;
            string text = BtGraphClipboard.Copy(graph, new[] { "left" });
            _objects.Remove(texture);
            Object.DestroyImmediate(texture);
            Assert.Throws<InvalidOperationException>(() => BtGraphClipboard.Paste(graph, text, Vector2.zero));
            Assert.That(graph.Nodes.Count, Is.EqualTo(3));
        }

        [Test]
        public void Search_MatchesNamesTypesAndNamespaceWithoutChangingTheGraph()
        {
            var graph = Graph();
            string before = EditorJsonUtility.ToJson(graph);
            Assert.That(BtGraphSearch.Find(graph, "tArGeT"), Is.EqualTo(new[] { "left", "right" }));
            Assert.That(BtGraphSearch.Find(graph, "SnapshotTestLeaf DodBehaviourTree"), Is.EqualTo(new[] { "left", "right" }));
            Assert.That(BtGraphSearch.Find(graph, "PATROL"), Is.EqualTo(new[] { "sequence" }));
            Assert.That(BtGraphSearch.Find(graph, "does-not-exist"), Is.Empty);
            Assert.That(BtGraphSearch.Find(graph, "   "), Is.Empty);
            Assert.That(EditorJsonUtility.ToJson(graph), Is.EqualTo(before));
        }

        [Test]
        public void GraphView_SearchWorksInDebugModeAndPastingIsDisabled()
        {
            var graph = Graph();
            var view = new BtGraphView(true);
            try
            {
                view.Bind(graph);
                int results = 0;
                view.SearchResultsChanged += (_, total) => results = total;
                view.SetSearchQuery("target");
                Assert.That(results, Is.EqualTo(2));
                Assert.That(view.canPasteSerializedData(BtGraphClipboard.Copy(graph, new[] { "left" })), Is.False);
                view.AddNode(BtNodeKind.Leaf, Vector2.zero);
                Assert.That(graph.Nodes.Count, Is.EqualTo(3));
            }
            finally { view.DisposeInspectors(); }
        }
    }
}
