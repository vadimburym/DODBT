// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
#if DODBT_SMALL_SIZE
using NodeIndex = System.Byte;
#else
using NodeIndex = System.UInt16;
#endif

using System;
using System.Collections.Generic;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    internal sealed class TestTreeRunner : IDisposable
    {
        private const NodeIndex None = NodeIndex.MaxValue;

        public DynamicBuffer<NodeStateElement> NodeStates => _entityManager.GetBuffer<NodeStateElement>(_entity);
        public DynamicBuffer<RecordingLeafState> LeafStates => _entityManager.GetBuffer<RecordingLeafState>(_entity);
        public IReadOnlyList<string> Events => _context.Events;

        private readonly World _world;
        private readonly EntityManager _entityManager;
        private readonly Entity _entity;
        private readonly BTRunner_TestContext _runner;
        private readonly BlobAssetReference<BehaviourTreeBlob> _blob;
        private readonly TestContext _context;
        private Random _random;

        public TestTreeRunner(
            BlobAssetReference<BehaviourTreeBlob> blob,
            TestContext context,
            int randomSeed = 1)
        {
            _context = context;
            _blob = blob;
            _runner = new BTRunner_TestContext();
            _random = new Random((uint)Math.Max(1, randomSeed));

            _world = new World("DODBT.Tests.World");
            _entityManager = _world.EntityManager;
            _entity = _entityManager.CreateEntity();
            _entityManager.AddBuffer<NodeStateElement>(_entity);
            _entityManager.AddBuffer<RecordingLeafState>(_entity);

            InitializeStateBuffers();
        }

        public RecordingLeafState Recording(string name)
        {
            var buffer = _entityManager.GetBuffer<RecordingLeafState>(_entity);
            var index = _context.GetLeafIndex(name);
            return buffer[index];
        }

        public NodeStatus Tick()
        {
            ref var blob = ref _blob.Value;
            return _runner.Tick(ref blob, ref _random, NodeStates, LeafStates, _context);
        }

        public void Abort()
        {
            ref var blob = ref _blob.Value;
            _runner.Abort(ref blob, NodeStates, LeafStates, _context);
        }

        public void Dispose()
        {
            if (_blob.IsCreated)
                _blob.Dispose();

            if (_world.IsCreated)
                _world.Dispose();
        }

        private void InitializeStateBuffers()
        {
            var nodeStates = NodeStates;
            nodeStates.Clear();

            NodeIndex leafIndex = 0;
            ref var blob = ref _blob.Value;
            for (var i = 0; i < blob.Nodes.Length; i++)
            {
                var isLeaf = blob.Nodes[i].Id == NodeId.Leaf;
                nodeStates.Add(new NodeStateElement
                {
                    CachedStatus = 0,
                    Cursor = 0xFF,
                    LeafStateIndex = isLeaf ? leafIndex : None,
                    MemoryCursor = 0xFF,
                    TmpA = 0,
                    TmpB = 0,
                });

                if (isLeaf)
                    leafIndex++;
            }

            var leafStates = LeafStates;
            leafStates.Clear();
            for (ushort i = 0; i < blob.Leafs.Length; i++)
            {
                leafStates.Add(new RecordingLeafState
                {
                    IsEntered = 0,
                    BufferIndex = i,
                    EnterCount = 0,
                    TickCount = 0,
                    ExitCount = 0,
                    AbortCount = 0,
                    StatusCursor = 0,
                    LastStatus = NodeStatus.None
                });
            }
        }
    }
}
