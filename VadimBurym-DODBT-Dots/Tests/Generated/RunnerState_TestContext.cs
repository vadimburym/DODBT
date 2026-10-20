// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;

using Unity.Collections;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    internal ref struct RunnerState_TestContext
    {
        [ReadOnly] public LeafData LeafData;
        public RecordingLeafState LeafState;
        [ReadOnly] public TestContext Context;
    }
}
