// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Light.Editor;

using System;

namespace VadimBurym.DodBehaviourTree.Light.Tests
{
    [Serializable]
    internal sealed class WrongContextLeaf : ILeaf<WrongContext, TestLeafState>
    {
        private readonly NodeStatus _status;

        public WrongContextLeaf(NodeStatus status)
        {
            _status = status;
        }

        public NodeStatus OnTick(WrongContext context, ref TestLeafState state) => _status;
        public void OnEnter(WrongContext context, ref TestLeafState state) { }
        public void OnExit(WrongContext context, ref TestLeafState state, NodeStatus exitStatus) { }
        public void OnAbort(WrongContext context, ref TestLeafState state) { }
    }
}