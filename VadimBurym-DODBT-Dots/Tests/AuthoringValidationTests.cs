// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VadimBurym.DodBehaviourTree.Dots.Generated;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class AuthoringValidationTests
    {
        [Test]
        public void Compiler_RejectsAParentPointerThatDisagreesWithConnections()
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            try
            {
                graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "leaf" };
                graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Kind = BtNodeKind.Leaf, ParentGuid = "wrong" });
                graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = new SnapshotTestLeaf() });
                Assert.That(BtGraphValidation.Validate(graph), Does.Contain("parent"));
            }
            finally { UnityEngine.Object.DestroyImmediate(graph); }
        }

        [TestCase(7, true)]
        [TestCase(8, false)]
        public void Compiler_ValidatesTheAbortStackBoundary(int lastChildren, bool valid)
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            try
            {
                graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "p0" };
                for (int depth = 0; depth < 5; depth++)
                {
                    string guid = "p" + depth;
                    var children = new List<string>();
                    graph.Nodes.Add(new BtNodeHeader { Guid = guid, Kind = BtNodeKind.Parallel, ParentGuid = depth == 0 ? "root" : "p" + (depth - 1) });
                    graph.ParallelNodes.Add(new BtParallelNodeData { Guid = guid, ChildrenGuids = children, SuccessThreshold = -1, FailureThreshold = 1 });
                    int leaves = depth < 4 ? 254 : lastChildren;
                    for (int i = 0; i < leaves; i++)
                    {
                        string leaf = guid + "-" + i;
                        children.Add(leaf);
                        graph.Nodes.Add(new BtNodeHeader { Guid = leaf, Kind = BtNodeKind.Leaf, ParentGuid = guid });
                        graph.LeafNodes.Add(new BtLeafNodeData { Guid = leaf, Leaf = new SnapshotTestLeaf() });
                    }
                    if (depth < 4) children.Add("p" + (depth + 1));
                }
                string error = BtGraphValidation.Validate(graph);
#if DODBT_SMALL_SIZE
                Assert.That(error, Does.Contain("255 reachable nodes").Or.Contain("abort stack"));
#else
                if (valid) Assert.That(error, Is.Empty);
                else Assert.That(error, Does.Contain("abort stack"));
#endif
            }
            finally { UnityEngine.Object.DestroyImmediate(graph); }
        }

        [Test]
        public void Compiler_RejectsLeafDataWithAnUnrelatedDispatchId()
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            try
            {
                graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "leaf" };
                graph.Nodes.Add(new BtNodeHeader { Guid = "leaf", Kind = BtNodeKind.Leaf, ParentGuid = "root" });
                graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = new WrongIdLeaf() });
                Assert.That(BtGraphValidation.Validate(graph), Does.Contain("LeafId"));
            }
            finally { UnityEngine.Object.DestroyImmediate(graph); }
        }

        [Serializable, LeafCodeGen(2, typeof(AuditContext))]
        public sealed class WrongIdLeaf : ILeaf
        {
            public LeafData GetCompiledData() => new LeafData { LeafId = 99 };
            public static NodeStatus OnTick(ref RunnerState_AuditContext state) => NodeStatus.Success;
            public static void OnEnter(ref RunnerState_AuditContext state) { }
            public static void OnExit(ref RunnerState_AuditContext state) { }
            public static void OnAbort(ref RunnerState_AuditContext state) { }
        }
    }
}
