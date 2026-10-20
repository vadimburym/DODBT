// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;

using System;
using System.Collections.Generic;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    internal sealed class TestContext
    {
        private readonly string[] _leafNames;

        public TestContext(string[] leafNames)
        {
            _leafNames = leafNames ?? throw new ArgumentNullException(nameof(leafNames));
        }

        public List<string> Events { get; } = new List<string>();

        public byte GetLeafIndex(string name)
        {
            for (int i = 0; i < _leafNames.Length; i++)
                if (name == _leafNames[i]) return (byte)i;

            throw new ArgumentOutOfRangeException(nameof(name), name, "Leaf name is outside of registered test leaves.");
        }

        public string GetLeafName(ushort bufferIndex)
        {
            if (bufferIndex >= _leafNames.Length)
                throw new ArgumentOutOfRangeException(nameof(bufferIndex), bufferIndex, "Leaf buffer index is outside of registered test leaves.");

            return _leafNames[bufferIndex];
        }
    }
}
