using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Tests
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
