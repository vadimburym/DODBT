using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Tests
{
    public sealed class SerializationAndCompilerTests
    {
        private readonly List<Object> _objects = new();
        private string _folder;

        [SetUp]
        public void SetUp() => _folder = "Assets/DODBT_TestTemp_" + Guid.NewGuid().ToString("N");

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objects)
                if (obj != null && !EditorUtility.IsPersistent(obj)) Object.DestroyImmediate(obj);
            _objects.Clear();
            if (AssetDatabase.IsValidFolder(_folder)) AssetDatabase.DeleteAsset(_folder);
        }

        private T Make<T>() where T : ScriptableObject
        {
            var obj = ScriptableObject.CreateInstance<T>();
            _objects.Add(obj);
            return obj;
        }

        private BtGraphAsset Graph(ILeaf leaf = null)
        {
            var graph = Make<BtGraphAsset>();
            graph.CompiledAsset = Make<BehaviourTreeAsset>();
            graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "leaf" };
            graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Kind = BtNodeKind.Leaf, Title = "Fixture" });
            graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = leaf ?? new SnapshotTestLeaf() });
            return graph;
        }

        private void SavePair(BtGraphAsset graph)
        {
            AssetDatabase.CreateFolder("Assets", _folder.Substring("Assets/".Length));
            AssetDatabase.CreateAsset(graph, _folder + "/Graph.asset");
            AssetDatabase.CreateAsset(graph.CompiledAsset, _folder + "/Compiled.asset");
            AssetDatabase.SaveAssets();
        }

        [Test]
        public void Snapshot_PreservesNestedTypesCyclesAliasesAndUnityReferences()
        {
            var texture = new Texture2D(1, 1);
            _objects.Add(texture);
            var value = new SnapshotTestValue { Number = 17 };
            value.Next = value;
            var source = new SnapshotTestLeaf { Number = 42, Value = value, Alias = value, Texture = texture, RuntimeOnly = new object() };
            source.Values.Add(value);
            var clone = (SnapshotTestLeaf)BtLeafSnapshot.Clone(source);
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.Number, Is.EqualTo(42));
            Assert.That(clone.Value, Is.TypeOf<SnapshotTestValue>().And.Not.SameAs(value));
            Assert.That(clone.Value, Is.SameAs(clone.Alias));
            Assert.That(clone.Values[0], Is.SameAs(clone.Value));
            Assert.That(((SnapshotTestValue)clone.Value).Next, Is.SameAs(clone.Value));
            Assert.That(clone.Texture, Is.SameAs(texture));
            Assert.That(clone.RuntimeOnly, Is.Null);
            ((SnapshotTestValue)clone.Value).Number = 99;
            Assert.That(value.Number, Is.EqualTo(17));
        }

        [Test]
        public void Compile_CreatesIndependentSnapshotAndExecutableTree()
        {
            var source = new SnapshotTestLeaf { Number = 5, Value = new SnapshotTestValue { Number = 8 } };
            var graph = Graph(source);
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Empty);
            var result = (SnapshotTestLeaf)graph.CompiledAsset.Leafs[0];
            source.Number = 100;
            ((SnapshotTestValue)source.Value).Number = 200;
            Assert.That(result.Number, Is.EqualTo(5));
            Assert.That(((SnapshotTestValue)result.Value).Number, Is.EqualTo(8));
            var tree = TestTreeFactory.CreateTree(graph.CompiledAsset);
            tree.Tick(new TestContext(), TestTreeFactory.CreateInitializedState(tree));
            Assert.That(result.Ticks, Is.EqualTo(1));
            Assert.That(graph.NotActualCompiledVersion, Is.False);
        }

        [Test]
        public void SavedAssets_ReloadPolymorphicDataAndExternalAssetReferences()
        {
            var graph = Graph(new SnapshotTestLeaf { Number = 7, Value = new SnapshotTestValue { Number = 23 } });
            SavePair(graph);
            var texture = new Texture2D(1, 1);
            _objects.Add(texture);
            AssetDatabase.CreateAsset(texture, _folder + "/Texture.asset");
            ((SnapshotTestLeaf)graph.LeafNodes[0].Leaf).Texture = texture;
            EditorUtility.SetDirty(graph);
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Empty);
            Assert.That(AssetDatabase.CopyAsset(_folder + "/Compiled.asset", _folder + "/Reloaded.asset"), Is.True);
            var reloaded = AssetDatabase.LoadAssetAtPath<BehaviourTreeAsset>(_folder + "/Reloaded.asset");
            var leaf = (SnapshotTestLeaf)reloaded.Leafs.Single();
            Assert.That(leaf.Number, Is.EqualTo(7));
            Assert.That(((SnapshotTestValue)leaf.Value).Number, Is.EqualTo(23));
            Assert.That(AssetDatabase.GetAssetPath(leaf.Texture), Is.EqualTo(_folder + "/Texture.asset"));
            Assert.That(AssetDatabase.CopyAsset(_folder + "/Graph.asset", _folder + "/ReloadedGraph.asset"), Is.True);
            var reloadedGraph = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(_folder + "/ReloadedGraph.asset");
            Assert.That(((SnapshotTestLeaf)reloadedGraph.LeafNodes[0].Leaf).Number, Is.EqualTo(7));
        }

        [Test]
        public void ExistingExample_RecompilesWithSameNodesLeafTypesAndNestedComparisonValues()
        {
            var original = AssetDatabase.LoadAssetAtPath<BtGraphAsset>("Assets/_ExampleProject/Content/AI/BrainEnemy_01_Graph.asset");
            if (original == null) Assert.Ignore("Requires the DODBT example project graph.");
            var graph = Object.Instantiate(original);
            _objects.Add(graph);
            var expected = original.CompiledAsset;
            graph.CompiledAsset = Make<BehaviourTreeAsset>();
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Empty);
            var actual = graph.CompiledAsset;
            Assert.That(actual.Nodes.Length, Is.EqualTo(expected.Nodes.Length));
            for (int i = 0; i < actual.Nodes.Length; i++)
            {
                Assert.That(actual.Nodes[i].Id, Is.EqualTo(expected.Nodes[i].Id));
                Assert.That(actual.Nodes[i].DataIndex, Is.EqualTo(expected.Nodes[i].DataIndex));
            }
            CollectionAssert.AreEqual(expected.SelectorNodes, actual.SelectorNodes);
            CollectionAssert.AreEqual(expected.SequenceNodes, actual.SequenceNodes);
            CollectionAssert.AreEqual(expected.MemorySelectorNodes, actual.MemorySelectorNodes);
            CollectionAssert.AreEqual(expected.MemorySequenceNodes, actual.MemorySequenceNodes);
            CollectionAssert.AreEqual(expected.ParallelNodes, actual.ParallelNodes);
            Assert.That(actual.Leafs.Length, Is.EqualTo(expected.Leafs.Length));
            using var expectedData = new SerializedObject(expected);
            using var actualData = new SerializedObject(actual);
            for (int i = 0; i < actual.Leafs.Length; i++)
            {
                Assert.That(actual.Leafs[i].GetType(), Is.EqualTo(expected.Leafs[i].GetType()));
                string path = "Leafs.Array.data[" + i + "]";
                var before = expectedData.FindProperty(path);
                var after = actualData.FindProperty(path);
                var threshold = before.FindPropertyRelative("_threshold");
                if (threshold != null)
                {
                    Assert.That(after.FindPropertyRelative("_threshold").floatValue, Is.EqualTo(threshold.floatValue));
                    Assert.That(after.FindPropertyRelative("_comparison").managedReferenceFullTypename,
                        Is.EqualTo(before.FindPropertyRelative("_comparison").managedReferenceFullTypename));
                }
            }
        }

        [Test]
        public void SerializedLeafEdit_UndoRedoAndNodeIndexChangesPreserveIdentity()
        {
            var graph = Graph(new SnapshotTestLeaf { Number = 3 });
            using var inspector = new BtNodeInspector(graph, "leaf", BtNodeKind.Leaf, _ => { });
            using (var data = new SerializedObject(graph))
            {
                data.FindProperty("LeafNodes.Array.data[0].Leaf.Number").intValue = 9;
                data.ApplyModifiedProperties();
            }
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(((SnapshotTestLeaf)graph.LeafNodes[0].Leaf).Number, Is.EqualTo(3));
            Undo.PerformRedo();
            Assert.That(((SnapshotTestLeaf)graph.LeafNodes[0].Leaf).Number, Is.EqualTo(9));
            graph.LeafNodes.Insert(0, new BtLeafNodeData { Guid = "another", Leaf = new SnapshotTestLeaf() });
            using var fresh = new BtNodeInspector(graph, "leaf", BtNodeKind.Leaf, _ => { });
            Assert.That(fresh.RootPath(), Is.EqualTo("LeafNodes.Array.data[1]"));
            Undo.ClearUndo(graph);
        }

        [Test]
        public void TypePicker_DiscoversExampleLeavesAndNestedComparisonTypesButNotEditorFixtures()
        {
            var comparison = Type.GetType("Utils.IComparison, Assembly-CSharp");
            if (comparison == null) Assert.Ignore("Requires the DODBT example project leaf and comparison types.");
            var types = BtTypePicker.GetTypes(typeof(ILeaf));
            Assert.That(types.Any(t => t.Name == "DistanceToPlayerThresholdLeaf"), Is.True);
            Assert.That(types.Contains(typeof(SnapshotTestLeaf)), Is.False);
            Assert.That(BtTypePicker.GetTypes(comparison).Any(t => t.Name == "LessOrEqualComparison"), Is.True);
        }

        [Test]
        public void FailedCompile_PreservesLastGoodAsset()
        {
            var graph = Graph(new SnapshotTestLeaf { Number = 7 });
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Empty);
            var oldLeaf = graph.CompiledAsset.Leafs[0];
            var oldNodes = graph.CompiledAsset.Nodes;
            graph.LeafNodes[0].Leaf = null;
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Does.Contain("no ILeaf"));
            Assert.That(graph.CompiledAsset.Leafs[0], Is.SameAs(oldLeaf));
            Assert.That(graph.CompiledAsset.Nodes, Is.SameAs(oldNodes));
        }

        [Test]
        public void Compiler_RejectsMissingRootAndBrokenConnections()
        {
            var graph = Graph();
            graph.RootNode = null;
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Does.Contain("Root"));
            graph.RootNode = new BtRootNodeData { ChildrenGuid = "missing" };
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Does.Contain("missing node"));
        }

        [Test]
        public void Compiler_RejectsCycleWithoutHanging()
        {
            var graph = Graph();
            graph.Nodes[0].Kind = BtNodeKind.Sequence;
            graph.SequenceNodes.Add(new BtSequenceNodeData { Guid = "leaf", ChildrenGuids = new List<string> { "leaf" } });
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Does.Contain("cycle"));
        }

        [Test]
        public void Compiler_RejectsDuplicateGuidsAndOversizedChildLists()
        {
            var graph = Graph();
            graph.Nodes.Add(graph.Nodes[0]);
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Does.Contain("Duplicate"));
            graph.Nodes.RemoveAt(1);
            graph.Nodes[0].Kind = BtNodeKind.Sequence;
            graph.SequenceNodes.Add(new BtSequenceNodeData { Guid = "leaf", ChildrenGuids = Enumerable.Repeat("child", 256).ToList() });
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Does.Contain("255 children"));
        }

        [TestCase(0)]
        [TestCase(-2)]
        [TestCase(256)]
        public void Compiler_RejectsInvalidParallelThreshold(int threshold)
        {
            var graph = Graph();
            graph.Nodes.Add(new BtNodeHeader { Guid = "parallel", Kind = BtNodeKind.Parallel });
            graph.RootNode.ChildrenGuid = "parallel";
            graph.ParallelNodes.Add(new BtParallelNodeData { Guid = "parallel", SuccessThreshold = threshold, FailureThreshold = 1, ChildrenGuids = new List<string> { "leaf" } });
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Does.Contain("thresholds"));
        }

        [Test]
        public void Compiler_HonorsConfiguredNodeIndexCapacity()
        {
            var graph = Graph();
            string child = "leaf";
            for (int i = 0; i < 255; i++)
            {
                string guid = "sequence-" + i;
                graph.Nodes.Add(new BtNodeHeader { Guid = guid, Kind = BtNodeKind.Sequence });
                graph.SequenceNodes.Add(new BtSequenceNodeData { Guid = guid, ChildrenGuids = new List<string> { child } });
                child = guid;
            }
            graph.RootNode.ChildrenGuid = child;
#if DODBT_SMALL_SIZE
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Does.Contain("255 reachable nodes"));
#else
            Assert.That(BtCompiler.TryCompileAsset(graph, graph.CompiledAsset), Is.Empty);
            Assert.That(graph.CompiledAsset.Nodes.Length, Is.EqualTo(256));
            var tree = TestTreeFactory.CreateTree(graph.CompiledAsset);
            tree.Tick(new TestContext(), TestTreeFactory.CreateInitializedState(tree));
            Assert.That(((SnapshotTestLeaf)tree.Leafs[0]).Ticks, Is.EqualTo(1));
#endif
        }

        [Test]
        public void Inspector_LeafChildrenIncludePrivateSerializedFieldsAndRespectHiddenFields()
        {
            var graph = Graph();
            using var serialized = new SerializedObject(graph);
            var leaf = serialized.FindProperty("LeafNodes.Array.data[0].Leaf");
            var children = new List<string>();
            var cursor = leaf.Copy();
            var end = cursor.GetEndProperty();
            if (cursor.NextVisible(true))
                do
                {
                    if (SerializedProperty.EqualContents(cursor, end)) break;
                    children.Add(cursor.name);
                } while (cursor.NextVisible(false));
            Assert.That(children, Does.Contain("_privateNumber"));
            Assert.That(children, Does.Contain("Value"));
            Assert.That(children, Does.Not.Contain("HiddenNumber"));
            Assert.That(children, Does.Not.Contain("RuntimeOnly"));
        }

        [Test]
        public void Inspector_ExampleLeafExposesThresholdAndNestedComparison()
        {
            var graph = AssetDatabase.LoadAssetAtPath<BtGraphAsset>("Assets/_ExampleProject/Content/AI/BrainEnemy_01_Graph.asset");
            if (graph == null) Assert.Ignore("Requires the DODBT example project graph.");
            int index = graph.LeafNodes.FindIndex(n => n.Leaf?.GetType().Name == "DistanceToPlayerThresholdLeaf");
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            using var serialized = new SerializedObject(graph);
            var cursor = serialized.FindProperty($"LeafNodes.Array.data[{index}].Leaf");
            var end = cursor.GetEndProperty();
            Assert.That(cursor.NextVisible(true), Is.True);
            Assert.That(SerializedProperty.EqualContents(cursor, end), Is.False);
            Assert.That(cursor.name, Is.EqualTo("_threshold"));
            Assert.That(cursor.NextVisible(false), Is.True);
            Assert.That(cursor.name, Is.EqualTo("_comparison"));
        }

        [Test]
        public void Inspector_LeafExpansionRequiresVisibleSerializedFields()
        {
            var graph = Graph();
            using var serialized = new SerializedObject(graph);
            var leaf = serialized.FindProperty("LeafNodes.Array.data[0].Leaf");
            Assert.That(BtSerializedFields.HasVisibleFields(leaf), Is.True);
            leaf.managedReferenceValue = new EmptyInspectorLeaf();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(BtSerializedFields.HasVisibleFields(leaf), Is.False);
            leaf.managedReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(BtSerializedFields.HasVisibleFields(leaf), Is.False);
        }

        [Serializable]
        private sealed class EmptyInspectorLeaf : ILeaf<TestContext, TestLeafState>
        {
            [HideInInspector] public int Hidden;
            [NonSerialized] public int RuntimeOnly;
            public void OnEnter(TestContext context, ref TestLeafState state) { }
            public NodeStatus OnTick(TestContext context, ref TestLeafState state) => NodeStatus.Success;
            public void OnExit(TestContext context, ref TestLeafState state, NodeStatus status) { }
            public void OnAbort(TestContext context, ref TestLeafState state) { }
        }

    }

    public interface ISnapshotTestValue { }

    [Serializable]
    public sealed class SnapshotTestValue : ISnapshotTestValue
    {
        public int Number;
        [SerializeReference] public ISnapshotTestValue Next;
    }

    [Serializable]
    internal sealed class SnapshotTestLeaf : ILeaf<TestContext, TestLeafState>
    {
        [SerializeField] private int _privateNumber = 11;
        [HideInInspector] public int HiddenNumber;
        public int PrivateNumber => _privateNumber;
        public int Number;
        public Texture2D Texture;
        [SerializeReference] public ISnapshotTestValue Value;
        [SerializeReference] public ISnapshotTestValue Alias;
        [SerializeReference] public List<ISnapshotTestValue> Values = new();
        [NonSerialized] public object RuntimeOnly;
        [NonSerialized] public int Ticks;
        public NodeStatus OnTick(TestContext context, ref TestLeafState state) { Ticks++; return NodeStatus.Success; }
        public void OnEnter(TestContext context, ref TestLeafState state) { }
        public void OnExit(TestContext context, ref TestLeafState state, NodeStatus status) { }
        public void OnAbort(TestContext context, ref TestLeafState state) { }
    }
}
