// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using Unity.Entities;

namespace VadimBurym.DodBehaviourTree.Dots
{
    [Serializable]
    [InternalBufferCapacity(0)]
    public struct LeafStateElement : IBufferElementData
    {
        public byte IsEntered;
        public ushort BufferIndex;
        public Entity StateEntity;
    }
}
