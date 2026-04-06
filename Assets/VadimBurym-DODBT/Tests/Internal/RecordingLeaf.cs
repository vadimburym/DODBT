// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Tests
{
    [Serializable]
    internal sealed class RecordingLeaf : ILeaf<TestContext, TestLeafState>
    {
        private readonly string _name;
        private readonly NodeStatus[] _statuses;
        private int _cursor;

        public RecordingLeaf(string name, params NodeStatus[] statuses)
        {
            if (statuses == null || statuses.Length == 0)
                throw new ArgumentException("At least one status is required.", nameof(statuses));

            _name = name;
            _statuses = statuses;
        }

        public int EnterCount { get; private set; }
        public int TickCount { get; private set; }
        public int ExitCount { get; private set; }
        public int AbortCount { get; private set; }

        public void OnEnter(TestContext context, ref TestLeafState state)
        {
            EnterCount++;
            state.EnterCount++;
            context.Events.Add("enter:" + _name);
        }

        public NodeStatus OnTick(TestContext context, ref TestLeafState state)
        {
            TickCount++;
            state.TickCount++;

            var index = Mathf.Min(_cursor, _statuses.Length - 1);
            var status = _statuses[index];
            _cursor++;

            context.Events.Add("tick:" + _name + ":" + status);
            return status;
        }

        public void OnExit(TestContext context, ref TestLeafState state, NodeStatus exitStatus)
        {
            ExitCount++;
            state.ExitCount++;
            state.LastExitStatus = exitStatus;
            context.Events.Add("exit:" + _name + ":" + exitStatus);
        }

        public void OnAbort(TestContext context, ref TestLeafState state)
        {
            AbortCount++;
            state.AbortCount++;
            context.Events.Add("abort:" + _name);
        }
    }
}