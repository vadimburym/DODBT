// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;

using System;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    [Serializable]
    internal static class RecordingLeaf
    {
        public const int LeafId = 0;

        public static NodeStatus OnTick(ref RunnerState_TestContext state)
        {
            state.LeafState.TickCount++;
            var cursor = state.LeafState.StatusCursor;

            var statusIndex = cursor < state.LeafData.Bytes.Length
                ? cursor
                : state.LeafData.Bytes.Length - 1;

            var status = (NodeStatus)state.LeafData.Bytes[statusIndex];
            state.LeafState.StatusCursor = (byte)(cursor + 1);

            state.Context.Events.Add("tick:" + state.Context.GetLeafName(state.LeafState.BufferIndex) + ":" + status);
            state.LeafState.LastStatus = status;
            return status;
        }

        public static void OnEnter(ref RunnerState_TestContext state)
        {
            state.LeafState.EnterCount++;
            state.Context.Events.Add("enter:" + state.Context.GetLeafName(state.LeafState.BufferIndex));
        }

        public static void OnExit(ref RunnerState_TestContext state)
        {
            state.LeafState.ExitCount++;
            state.Context.Events.Add("exit:" + state.Context.GetLeafName(state.LeafState.BufferIndex));
        }

        public static void OnAbort(ref RunnerState_TestContext state)
        {
            state.LeafState.AbortCount++;
            state.Context.Events.Add("abort:" + state.Context.GetLeafName(state.LeafState.BufferIndex));
        }
    }
}
