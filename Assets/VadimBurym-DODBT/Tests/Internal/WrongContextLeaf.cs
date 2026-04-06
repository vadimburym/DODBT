// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;

namespace VadimBurym.DodBehaviourTree.Tests
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