// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

#if UNITY_EDITOR
using Unity.Collections;
using Unity.Entities;

namespace VadimBurym.DodBehaviourTree.Dots
{
    // Authoring identity only. No debug components or traces are included in players.
    public struct BtDebugTree : ISharedComponentData
    {
        public FixedString64Bytes GraphGuid;
        public FixedString64Bytes CompiledRevision;
    }

    public struct BtDebugTarget : IComponentData
    {
        public Entity Entity;
    }

    [InternalBufferCapacity(0)]
    public struct BtDebugNodeState : IBufferElementData
    {
        public NodeStatus Status;
    }

    public struct BtDebugSnapshot : IComponentData
    {
        public uint Tick;
        public double Time;
        public NodeStatus Status;
    }
}
#endif
