// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class LeafBindingTests
    {
        [Test]
        public void ReusedOrDeletedId_RejectsOldTree_ButUnusedChangesDoNot()
        {
            var asset = TestTreeFactory.CreateAsset(TestNodeSpec.RecordingLeaf("leaf", NodeStatus.Success));
            const string context = "BindingTests:Context";
            asset.InternalContextIdentity = context;
            asset.InternalLeafBindings = new[] { new LeafBinding { Id = 0, TypeIdentity = "Tests:OldLeaf" } };
            try
            {
                LeafBindingRegistry.Register(context, id => id == 0 ? "Tests:OldLeaf" : "Tests:NewUnusedLeaf");
                Assert.That(asset.GetCompatibilityError(), Is.Null);
                using (var blob = asset.CreateBlob()) Assert.That(blob.IsCreated, Is.True);
                LeafBindingRegistry.Register(context, id => null);
                Assert.That(asset.GetCompatibilityError(), Does.Contain("missing leaf"));
                Assert.Throws<InvalidOperationException>(() => asset.CreateBlob());
                LeafBindingRegistry.Register(context, id => id == 0 ? "Tests:ReplacementLeaf" : null);
                Assert.That(asset.GetCompatibilityError(), Does.Contain("Tests:OldLeaf").And.Contain("Tests:ReplacementLeaf"));
                Assert.Throws<InvalidOperationException>(() => asset.CreateBlob());
                asset.InternalLeafBindings[0].TypeIdentity = "Tests:ReplacementLeaf";
                Assert.That(asset.GetCompatibilityError(), Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [Test]
        public void LegacyFormat_MissingContext_MissingAndDuplicateBindings_FailClosed()
        {
            var asset = TestTreeFactory.CreateAsset(TestNodeSpec.RecordingLeaf("leaf", NodeStatus.Success));
            try
            {
                asset.InternalFormatVersion = 0;
                Assert.That(asset.GetCompatibilityError(), Does.Contain("data format"));
                asset.InternalFormatVersion = BehaviourTreeAsset.CurrentFormatVersion;
                asset.InternalContextIdentity = "Unregistered:Context";
                Assert.That(asset.GetCompatibilityError(), Does.Contain("No generated leaf table"));
                TestTreeFactory.SetBindings(asset, typeof(AuditContext), typeof(AuditLeaf));
                Assert.That(asset.GetCompatibilityError(), Is.Null, "Generated editor registration must be automatic.");
                asset.InternalLeafBindings = Array.Empty<LeafBinding>();
                Assert.That(asset.GetCompatibilityError(), Does.Contain("Missing compiled binding"));
                TestTreeFactory.SetBindings(asset, typeof(AuditContext), typeof(AuditLeaf));
                asset.InternalLeafBindings = new[] { asset.InternalLeafBindings[0], asset.InternalLeafBindings[0] };
                Assert.That(asset.GetCompatibilityError(), Does.Contain("duplicate"));
                asset.InternalLeafBindings[0].Id = -1;
                Assert.That(asset.GetCompatibilityError(), Does.Contain("Invalid"));
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        // The pre-migration native layout. Keep this fixture as byte to detect size changes.
        private struct LegacyLeafData
        {
            public byte LeafId;
            public int Int0;
            public float Float0;
            public byte Byte0;
            public FixedList32Bytes<byte> Bytes;
        }

        [Test]
        public void IntId_UsesExistingPadding_WithoutGrowingNativeLeafData()
        {
            int before = UnsafeUtility.SizeOf<LegacyLeafData>();
            int after = UnsafeUtility.SizeOf<LeafData>();
            NUnit.Framework.TestContext.WriteLine($"LeafData byte/int: {before}/{after} bytes; node state: {UnsafeUtility.SizeOf<NodeStateElement>()}; leaf state: {UnsafeUtility.SizeOf<LeafStateElement>()}");
            Assert.That(after, Is.EqualTo(before));
            var asset = TestTreeFactory.CreateAsset(TestNodeSpec.RecordingLeaf("leaf", NodeStatus.Success));
            try
            {
                const string context = "BindingTests:WideContext";
                asset.Leafs[0].LeafId = int.MaxValue;
                asset.InternalContextIdentity = context;
                asset.InternalLeafBindings = new[] { new LeafBinding { Id = int.MaxValue, TypeIdentity = "Tests:WideLeaf" } };
                LeafBindingRegistry.Register(context, id => id == int.MaxValue ? "Tests:WideLeaf" : null);
                using var blob = asset.CreateBlob();
                Assert.That(blob.Value.Leafs[0].LeafId, Is.EqualTo(int.MaxValue));
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }
    }
}
