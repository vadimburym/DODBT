// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;

using System.Runtime.CompilerServices;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    internal static class LeafTables_TestContext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NodeStatus TickLeaf(int leafId, ref RunnerState_TestContext state)
        {
            switch (leafId)
            {
                case RecordingLeaf.LeafId:
                    return RecordingLeaf.OnTick(ref state);
                default:
                    return NodeStatus.Failure;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnterLeaf(int leafId, ref RunnerState_TestContext state)
        {
            switch (leafId)
            {
                case RecordingLeaf.LeafId:
                    RecordingLeaf.OnEnter(ref state);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExitLeaf(int leafId, ref RunnerState_TestContext state)
        {
            switch (leafId)
            {
                case RecordingLeaf.LeafId:
                    RecordingLeaf.OnExit(ref state);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AbortLeaf(int leafId, ref RunnerState_TestContext state)
        {
            switch (leafId)
            {
                case RecordingLeaf.LeafId:
                    RecordingLeaf.OnAbort(ref state);
                    break;
            }
        }
    }
}
