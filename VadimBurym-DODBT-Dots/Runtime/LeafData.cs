// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using Unity.Collections;

namespace VadimBurym.DodBehaviourTree.Dots
{
    [Serializable]
    public struct LeafData
    {
        public int LeafId;
        public int Int0;
        public float Float0;
        public byte Byte0;
        public FixedList32Bytes<byte> Bytes;
    }
}
