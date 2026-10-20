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
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Light.Tests
{
    internal sealed class LinkedGroupTests
    {
        private BtGraphAsset _graph;
        private Texture2D _texture;

        [SetUp]
        public void Setup()
        {
            _graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            _graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "branch" };
            _graph.Nodes.Add(new BtNodeHeader { Guid = "branch", Kind = BtNodeKind.Sequence, Title = "Patrol", ParentGuid = "root" });
            _graph.SequenceNodes.Add(new BtSequenceNodeData { Guid = "branch", ChildrenGuids = new() { "leaf" } });
            _graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Kind = BtNodeKind.Leaf, Title = "Action", ParentGuid = "branch", Position = new Vector2(10, 100) });
            var value = new SnapshotTestValue { Number = 5 }; value.Next = value;
            _texture = new Texture2D(1, 1);
            _graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = new SnapshotTestLeaf { Number = 7, Value = value, Alias = value, Texture = _texture } });
        }

        [TearDown]
        public void Cleanup()
        {
            Undo.ClearUndo(_graph);
            Object.DestroyImmediate(_graph);
            Object.DestroyImmediate(_texture);
        }

        private string Corresponding(BtGroupInstance instance, string original = "leaf")
        {
            var source = _graph.LinkedGroups[0].Instances[0];
            string key = source.Members.Single(m => m.Guid == original).Key;
            return instance.Members.Single(m => m.Key == key).Guid;
        }

        [Test]
        public void DuplicateOfDuplicate_SynchronizesFromEitherSideWithIndependentManagedData()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            var first = group.Instances[0];
            var second = BtLinkedGroups.Duplicate(_graph, first, new Vector2(400, 0));
            var third = BtLinkedGroups.Duplicate(_graph, second, new Vector2(400, 0));
            var edited = (SnapshotTestLeaf)_graph.FindLeafData(Corresponding(second)).Leaf;
            edited.Number = 98;
            ((SnapshotTestValue)edited.Value).Number = 32;
            BtLinkedGroups.Synchronize(_graph, second);
            foreach (var instance in new[] { first, third })
            {
                var copy = (SnapshotTestLeaf)_graph.FindLeafData(Corresponding(instance)).Leaf;
                Assert.That(copy.Number, Is.EqualTo(98));
                Assert.That(((SnapshotTestValue)copy.Value).Number, Is.EqualTo(32));
                Assert.That(copy, Is.Not.SameAs(edited));
                Assert.That(copy.Value, Is.Not.SameAs(edited.Value));
                Assert.That(copy.Value, Is.SameAs(copy.Alias));
                Assert.That(((SnapshotTestValue)copy.Value).Next, Is.SameAs(copy.Value));
                Assert.That(copy.Texture, Is.SameAs(_texture));
            }
            Assert.That(_graph.FindHeader(third.RootGuid).Position, Is.EqualTo(new Vector2(800, 0)));
            Assert.That(_graph.FindHeader("branch").ParentGuid, Is.EqualTo("root"));
        }

        [Test]
        public void LeafTypeAndCompositeSettings_AreReplacedEverywhere()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            _graph.FindLeafData("leaf").Leaf = new SmokeLeafForGroups { Amount = 13 };
            _graph.FindHeader("branch").Kind = BtNodeKind.Parallel;
            _graph.SequenceNodes.Clear();
            _graph.ParallelNodes.Add(new BtParallelNodeData { Guid = "branch", ChildrenGuids = new() { "leaf" }, CacheChildStatus = true, SuccessThreshold = 4, FailureThreshold = 2 });
            BtLinkedGroups.Synchronize(_graph, group.Instances[0]);
            Assert.That(_graph.FindLeafData(Corresponding(copy)).Leaf, Is.TypeOf<SmokeLeafForGroups>());
            Assert.That(((SmokeLeafForGroups)_graph.FindLeafData(Corresponding(copy)).Leaf).Amount, Is.EqualTo(13));
            Assert.That(_graph.FindHeader(copy.RootGuid).Kind, Is.EqualTo(BtNodeKind.Parallel));
            Assert.That(_graph.FindParallelData(copy.RootGuid).SuccessThreshold, Is.EqualTo(4));
            Assert.That(_graph.FindParallelData(copy.RootGuid).CacheChildStatus, Is.True);
        }

        [Test]
        public void AddingThenDeletingMember_PropagatesAndKeepsStableExistingGuids()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            string originalCopy = Corresponding(copy);
            _graph.Nodes.Add(new BtNodeHeader { Guid = "new", Kind = BtNodeKind.Leaf, ParentGuid = "branch" });
            _graph.LeafNodes.Add(new BtLeafNodeData { Guid = "new", Leaf = new SnapshotTestLeaf() });
            _graph.FindSequenceData("branch").ChildrenGuids.Add("new");
            BtLinkedGroups.Synchronize(_graph, group.Instances[0]);
            string addedCopy = Corresponding(copy, "new");
            Assert.That(_graph.FindHeader(addedCopy).ParentGuid, Is.EqualTo(copy.RootGuid));
            Assert.That(Corresponding(copy), Is.EqualTo(originalCopy));
            BtLinkedGroups.DeleteNodes(_graph, new HashSet<string> { "new" });
            BtLinkedGroups.Synchronize(_graph, group.Instances[0]);
            Assert.That(_graph.FindHeader(addedCopy), Is.Null);
            Assert.That(copy.Members.Count, Is.EqualTo(2));
        }

        [Test]
        public void Disconnect_DetachesCorrespondingBranchesWithoutDeletingNodes()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            string other = Corresponding(copy);
            _graph.FindSequenceData("branch").ChildrenGuids.Clear();
            _graph.FindHeader("leaf").ParentGuid = null;
            BtLinkedGroups.Synchronize(_graph, group.Instances[0]);
            Assert.That(BtLinkedGroups.Find(_graph, "leaf"), Is.Null);
            Assert.That(BtLinkedGroups.Find(_graph, other), Is.Null);
            Assert.That(_graph.FindHeader(other), Is.Not.Null);
            Assert.That(_graph.FindHeader(other).ParentGuid, Is.Null);
            Assert.That(_graph.FindSequenceData(copy.RootGuid).ChildrenGuids, Is.Empty);
        }

        [Test]
        public void MoveRoot_TranslatesOnlyOneInstance_InteriorMovePropagatesRelativePosition()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            string leaf = Corresponding(copy);
            BtLinkedGroups.Move(_graph, new Dictionary<string, Vector2> { [copy.RootGuid] = new Vector2(700, 50) });
            Assert.That(_graph.FindHeader(leaf).Position, Is.EqualTo(new Vector2(710, 150)));
            Assert.That(_graph.FindHeader("leaf").Position, Is.EqualTo(new Vector2(10, 100)));
            BtLinkedGroups.Move(_graph, new Dictionary<string, Vector2> { [leaf] = new Vector2(720, 170) });
            Assert.That(_graph.FindHeader("leaf").Position, Is.EqualTo(new Vector2(20, 120)));
        }

        [Test]
        public void GroupsCannotOverlapNestOrAcceptExternalConnectionsToInterior()
        {
            var child = BtLinkedGroups.Create(_graph, "leaf");
            Assert.Throws<InvalidOperationException>(() => BtLinkedGroups.Create(_graph, "branch"));
            BtLinkedGroups.Unlink(_graph, child.Instances[0]);
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            Assert.Throws<InvalidOperationException>(() => BtLinkedGroups.Create(_graph, "leaf"));
            Assert.Throws<InvalidOperationException>(() => BtLinkedGroups.Create(_graph, "root"));
            Assert.That(BtLinkedGroups.ConnectionError(_graph, "branch", copy.RootGuid), Is.Not.Null);
            Assert.That(BtLinkedGroups.ConnectionError(_graph, "root", "leaf"), Is.Not.Null);
            Assert.That(BtLinkedGroups.ConnectionError(_graph, "root", copy.RootGuid), Is.Null);
        }

        [Test]
        public void UnlinkLastInstance_RemovesFamilyOnly_AndUndoRedoRestoresMembership()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            Undo.IncrementCurrentGroup();
            BtLinkedGroups.Unlink(_graph, group.Instances[0]);
            Assert.That(_graph.LinkedGroups, Is.Empty);
            Assert.That(_graph.Nodes.Count, Is.EqualTo(2));
            Undo.PerformUndo();
            Assert.That(_graph.LinkedGroups.Count, Is.EqualTo(1));
            Undo.PerformRedo();
            Assert.That(_graph.LinkedGroups, Is.Empty);
        }

        [Test]
        public void NativeSerialization_RetainsMembership_OrdinaryClipboardDropsIt()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            var loaded = Object.Instantiate(_graph);
            try
            {
                Assert.That(loaded.LinkedGroups[0].Instances.Count, Is.EqualTo(2));
                var added = BtGraphClipboard.Paste(loaded, BtGraphClipboard.Copy(_graph, new[] { "branch", "leaf" }), Vector2.one);
                Assert.That(added.All(id => BtLinkedGroups.Find(loaded, id) == null), Is.True);
                Assert.That(loaded.LinkedGroups[0].Instances.Count, Is.EqualTo(2));
            }
            finally { Undo.ClearUndo(loaded); Object.DestroyImmediate(loaded); }
        }

        [Test]
        public void GraphView_CommandsHighlightEveryInstanceAndReadOnlyRejectsMutation()
        {
            var view = new BtGraphView(false);
            try
            {
                view.Bind(_graph);
                view.RunGroupCommand("branch", "create");
                view.RunGroupCommand("branch", "duplicate");
                var group = _graph.LinkedGroups.Single();
                view.PingGroup(group.Id);
                foreach (var node in view.nodes.OfType<BtNodeView>().Where(n => n.Kind != BtNodeKind.Root))
                    Assert.That(node.Q("linked-group-outline").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                foreach (var instance in group.Instances)
                    Assert.That(view.nodes.OfType<BtNodeView>().Single(n => n.Guid == instance.RootGuid)
                        .Q("linked-group-icon").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                var readOnly = new BtGraphView(true);
                try
                {
                    readOnly.Bind(_graph);
                    readOnly.RunGroupCommand("branch", "unlink");
                    Assert.That(_graph.LinkedGroups.Single().Instances.Count, Is.EqualTo(2));
                }
                finally { readOnly.DisposeInspectors(); }
            }
            finally { view.DisposeInspectors(); }
        }

        [Test]
        public void GraphView_DeleteRootDeletesOnlyItsWholeInstance_AndUndoRestoresIt()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            var view = new BtGraphView(false);
            try
            {
                view.Bind(_graph);
                Undo.IncrementCurrentGroup();
                view.DeleteElements(new[] { view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "branch") });
                Assert.That(_graph.FindHeader("branch"), Is.Null);
                Assert.That(_graph.FindHeader("leaf"), Is.Null);
                Assert.That(_graph.LinkedGroups.Single().Instances.Single().RootGuid, Is.EqualTo(copy.RootGuid));
                Assert.That(_graph.Nodes.Count, Is.EqualTo(2));
                Undo.PerformUndo();
                Assert.That(_graph.Nodes.Count, Is.EqualTo(4));
                Assert.That(_graph.LinkedGroups.Single().Instances.Count, Is.EqualTo(2));
                Undo.PerformRedo();
                Assert.That(_graph.Nodes.Count, Is.EqualTo(2));
            }
            finally { view.DisposeInspectors(); }
        }

        [Test]
        public void GraphView_DeletingInteriorNodePropagatesToCopiesInOneUndo()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            var view = new BtGraphView(false);
            try
            {
                view.Bind(_graph);
                Undo.IncrementCurrentGroup();
                view.DeleteElements(new[] { view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "leaf") });
                Assert.That(_graph.LeafNodes, Is.Empty);
                Assert.That(_graph.Nodes.Count, Is.EqualTo(2));
                Assert.That(_graph.LinkedGroups.Single().Instances.All(i => i.Members.Count == 1), Is.True);
                Undo.PerformUndo();
                Assert.That(_graph.LeafNodes.Count, Is.EqualTo(2));
                Assert.That(_graph.LinkedGroups.Single().Instances.All(i => i.Members.Count == 2), Is.True);
            }
            finally { view.DisposeInspectors(); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GraphView_DeletingDifferentMembersAcrossInstances_CombinesChangesInOneUndo(bool disconnectOnly)
        {
            _graph.Nodes.Add(new BtNodeHeader { Guid = "other", Kind = BtNodeKind.Leaf, ParentGuid = "branch" });
            _graph.LeafNodes.Add(new BtLeafNodeData { Guid = "other", Leaf = new SnapshotTestLeaf() });
            _graph.FindSequenceData("branch").ChildrenGuids.Add("other");
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            string otherCopy = Corresponding(copy, "other");
            var view = new BtGraphView(false);
            try
            {
                view.Bind(_graph);
                Undo.IncrementCurrentGroup();
                if (disconnectOnly)
                    view.DeleteElements(view.edges.Where(e => e.input.node is BtNodeView n && (n.Guid == "leaf" || n.Guid == otherCopy)).ToArray());
                else
                    view.DeleteElements(view.nodes.OfType<BtNodeView>().Where(n => n.Guid == "leaf" || n.Guid == otherCopy).ToArray());
                Assert.That(_graph.LeafNodes.Count, Is.EqualTo(disconnectOnly ? 4 : 0));
                Assert.That(_graph.LinkedGroups.Single().Instances.All(i => i.Members.Count == 1), Is.True);
                Assert.That(_graph.SequenceNodes.All(n => n.ChildrenGuids.Count == 0), Is.True);
                Undo.PerformUndo();
                Assert.That(_graph.LeafNodes.Count, Is.EqualTo(4));
                Assert.That(_graph.SequenceNodes.All(n => n.ChildrenGuids.Count == 2), Is.True);
                Undo.PerformRedo();
                Assert.That(_graph.LeafNodes.Count, Is.EqualTo(disconnectOnly ? 4 : 0));
                Assert.That(_graph.SequenceNodes.All(n => n.ChildrenGuids.Count == 0), Is.True);
            }
            finally { view.DisposeInspectors(); }
        }

        [Test]
        public void SerializedFieldEdit_AndSynchronizedCopies_UndoRedoTogether()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            var view = new BtGraphView(false);
            try
            {
                view.Bind(_graph);
                Undo.IncrementCurrentGroup();
                using (var serialized = new SerializedObject(_graph))
                {
                    serialized.FindProperty("LeafNodes.Array.data[0].Leaf.Number").intValue = 123;
                    serialized.ApplyModifiedProperties();
                }
                view.NotifyNodeDataChanged("leaf");
                Assert.That(((SnapshotTestLeaf)_graph.FindLeafData(Corresponding(copy)).Leaf).Number, Is.EqualTo(123));
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.That(_graph.LeafNodes.All(n => ((SnapshotTestLeaf)n.Leaf).Number == 7), Is.True);
                Undo.PerformRedo();
                Assert.That(_graph.LeafNodes.All(n => ((SnapshotTestLeaf)n.Leaf).Number == 123), Is.True);
            }
            finally { view.DisposeInspectors(); }
        }

        [Test]
        public void GraphView_ReparentInsideGroup_IsOneTransactionAndKeepsMembership()
        {
            _graph.Nodes.Add(new BtNodeHeader { Guid = "inner", Kind = BtNodeKind.Sequence, ParentGuid = "branch" });
            _graph.SequenceNodes.Add(new BtSequenceNodeData { Guid = "inner" });
            _graph.FindSequenceData("branch").ChildrenGuids.Add("inner");
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            string peerLeaf = Corresponding(copy);
            string peerParent = Corresponding(copy, "inner");
            var view = new BtGraphView(false);
            try
            {
                view.Bind(_graph);
                var parent = view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "inner");
                var child = view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "leaf");
                Undo.IncrementCurrentGroup();
                new BtEdgeConnectorListener(view).OnDrop(view, new Edge { input = child.InputPort, output = parent.OutputPort });
                Assert.That(_graph.FindHeader(peerLeaf).ParentGuid, Is.EqualTo(peerParent));
                Assert.That(_graph.FindSequenceData(peerParent).ChildrenGuids, Does.Contain(peerLeaf));
                Assert.That(copy.Members.Count, Is.EqualTo(3));
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.That(_graph.FindHeader("leaf").ParentGuid, Is.EqualTo("branch"));
                Assert.That(_graph.FindHeader(peerLeaf).ParentGuid, Is.EqualTo(copy.RootGuid));
                Undo.PerformRedo();
                Assert.That(_graph.FindHeader(peerLeaf).ParentGuid, Is.EqualTo(peerParent));
            }
            finally { view.DisposeInspectors(); }
        }

        [Test]
        public void Compiler_EmitsOrdinaryIndependentNodesAndDoesNotNeedGroupMetadata()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            _graph.Nodes.Add(new BtNodeHeader { Guid = "outer", Kind = BtNodeKind.Sequence, ParentGuid = "root" });
            _graph.SequenceNodes.Add(new BtSequenceNodeData { Guid = "outer", ChildrenGuids = new() { "branch", copy.RootGuid } });
            _graph.FindHeader("branch").ParentGuid = "outer";
            _graph.FindHeader(copy.RootGuid).ParentGuid = "outer";
            _graph.RootNode.ChildrenGuid = "outer";
            var compiled = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
            try
            {
                Assert.That(BtCompiler.TryCompileAsset(_graph, compiled), Is.Empty);
                Assert.That(compiled.Nodes.Length, Is.EqualTo(5));
                Assert.That(compiled.Leafs.Length, Is.EqualTo(2));
                Assert.That(compiled.Leafs[0], Is.Not.SameAs(compiled.Leafs[1]));
                _graph.LinkedGroups.Clear();
                Assert.That(BtCompiler.TryCompileAsset(_graph, compiled), Is.Empty);
                Assert.That(compiled.Nodes.Length, Is.EqualTo(5));
            }
            finally { Object.DestroyImmediate(compiled); }
        }

        [Test]
        public void GroupRootClipboard_ContainsTheWholeBranchAsIndependentNodes()
        {
            BtLinkedGroups.Create(_graph, "branch");
            var view = new BtGraphView(false);
            try
            {
                view.Bind(_graph);
                string text = view.serializeGraphElements(new[] { view.nodes.OfType<BtNodeView>().Single(n => n.Guid == "branch") });
                var added = BtGraphClipboard.Paste(_graph, text, Vector2.right * 400);
                Assert.That(added.Count, Is.EqualTo(2));
                Assert.That(added.All(id => BtLinkedGroups.Find(_graph, id) == null), Is.True);
            }
            finally { view.DisposeInspectors(); }
        }

        [Test]
        public void AssetReload_RetainsGroupIdentityAndPrivateFieldSynchronization()
        {
            string path = "Assets/DODBT-GroupTest-" + Guid.NewGuid().ToString("N") + ".asset";
            var group = BtLinkedGroups.Create(_graph, "branch");
            BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            foreach (var leaf in _graph.LeafNodes) ((SnapshotTestLeaf)leaf.Leaf).Texture = null;
            AssetDatabase.CreateAsset(_graph, path);
            try
            {
                AssetDatabase.SaveAssetIfDirty(_graph);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var loaded = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(path);
                Assert.That(loaded.LinkedGroups[0].Id, Is.EqualTo(group.Id));
                Assert.That(loaded.LinkedGroups[0].Instances.Count, Is.EqualTo(2));
                using (var serialized = new SerializedObject(loaded))
                {
                    serialized.FindProperty("LeafNodes.Array.data[0].Leaf._privateNumber").intValue = 87;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                BtLinkedGroups.Synchronize(loaded, loaded.LinkedGroups[0].Instances[0]);
                Assert.That(loaded.LeafNodes.All(l => ((SnapshotTestLeaf)l.Leaf).PrivateNumber == 87), Is.True);
                AssetDatabase.SaveAssetIfDirty(loaded);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Assert.That(AssetDatabase.LoadAssetAtPath<BtGraphAsset>(path).LeafNodes.All(l => ((SnapshotTestLeaf)l.Leaf).PrivateNumber == 87), Is.True);
            }
            finally { Undo.ClearUndo(_graph); AssetDatabase.DeleteAsset(path); }
        }

        [Test]
        public void MovingMultipleInstances_AppliesRelativeDeltaOnlyOnceAndKeepsLeftToRightOrder()
        {
            _graph.Nodes.Add(new BtNodeHeader { Guid = "other", Kind = BtNodeKind.Leaf, ParentGuid = "branch", Position = new Vector2(30, 100) });
            _graph.LeafNodes.Add(new BtLeafNodeData { Guid = "other", Leaf = new SnapshotTestLeaf() });
            _graph.FindSequenceData("branch").ChildrenGuids.Add("other");
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            BtLinkedGroups.Move(_graph, new Dictionary<string, Vector2>
            {
                ["branch"] = new Vector2(100, 0),
                [Corresponding(copy)] = new Vector2(450, 100)
            });
            Assert.That(_graph.FindHeader("leaf").Position, Is.EqualTo(new Vector2(150, 100)));
            Assert.That(_graph.FindSequenceData("branch").ChildrenGuids, Is.EqualTo(new[] { "other", "leaf" }));
            Assert.That(_graph.FindSequenceData(copy.RootGuid).ChildrenGuids,
                Is.EqualTo(new[] { Corresponding(copy, "other"), Corresponding(copy) }));
        }

        [Test]
        public void UnlinkedInstanceAndDifferentFamilies_AreUnaffectedByFurtherEdits()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            string copiedLeaf = Corresponding(copy);
            BtLinkedGroups.Unlink(_graph, copy);
            var other = BtLinkedGroups.Create(_graph, copy.RootGuid);
            ((SnapshotTestLeaf)_graph.FindLeafData("leaf").Leaf).Number = 51;
            BtLinkedGroups.Synchronize(_graph, group.Instances[0]);
            Assert.That(((SnapshotTestLeaf)_graph.FindLeafData(copiedLeaf).Leaf).Number, Is.EqualTo(7));
            Assert.That(_graph.LinkedGroups.Count, Is.EqualTo(2));
            Assert.That(group.Id, Is.Not.EqualTo(other.Id));
        }

        [Test]
        public void MixedDeletion_RootAndAnotherInstanceMember_DoesNotSynchronizeIntoDeletedRoot()
        {
            var group = BtLinkedGroups.Create(_graph, "branch");
            var copy = BtLinkedGroups.Duplicate(_graph, group.Instances[0], Vector2.right * 400);
            string peerLeaf = Corresponding(copy);
            var view = new BtGraphView(false);
            try
            {
                view.Bind(_graph);
                var selected = view.nodes.OfType<BtNodeView>().Where(n => n.Guid == peerLeaf || n.Guid == "branch").Reverse().ToArray();
                view.DeleteElements(selected);
                Assert.That(_graph.Nodes.Count, Is.EqualTo(1));
                Assert.That(_graph.LinkedGroups.Single().Instances.Single().RootGuid, Is.EqualTo(copy.RootGuid));
                view.ReloadWithoutLog();
                view.DeleteElements(view.nodes.OfType<BtNodeView>().Where(n => n.Guid == copy.RootGuid).ToArray());
                Assert.That(_graph.LinkedGroups, Is.Empty);
            }
            finally { view.DisposeInspectors(); }
        }

        [Serializable]
        private sealed class SmokeLeafForGroups : ILeaf<object, int>
        {
            [SerializeField] private int _amount;
            public int Amount { get => _amount; set => _amount = value; }
            public void OnEnter(object context, ref int state) { }
            public NodeStatus OnTick(object context, ref int state) => NodeStatus.Success;
            public void OnExit(object context, ref int state, NodeStatus status) { }
            public void OnAbort(object context, ref int state) { }
        }
    }
}
