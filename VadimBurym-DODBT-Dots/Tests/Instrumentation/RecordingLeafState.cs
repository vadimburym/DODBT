// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;

using System;
using Unity.Entities;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    [Serializable]
    [InternalBufferCapacity(0)]
    public struct RecordingLeafState : IBufferElementData
    {
        public byte IsEntered;
        public ushort BufferIndex;
        public byte StatusCursor;
        public byte EnterCount;
        public byte ExitCount;
        public byte TickCount;
        public byte AbortCount;
        public NodeStatus LastStatus;
    }
}
