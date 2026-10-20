// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public interface ISnapshotTestValue { }

    [Serializable]
    public sealed class SnapshotTestValue : ISnapshotTestValue
    {
        public int Number;
        [SerializeReference] public ISnapshotTestValue Next;
    }

    [Serializable, LeafCodeGen(1, typeof(AuditContext))]
    internal sealed class SnapshotTestLeaf : ILeaf
    {
        [SerializeField] private int _privateNumber = 11;
        [HideInInspector] public int HiddenNumber;
        public int PrivateNumber => _privateNumber;
        public int Number;
        public Texture2D Texture;
        [SerializeReference] public ISnapshotTestValue Value;
        [SerializeReference] public ISnapshotTestValue Alias;
        [SerializeReference] public List<ISnapshotTestValue> Values = new();
        [NonSerialized] public object RuntimeOnly;
        [NonSerialized] public int Ticks;
        public LeafData GetCompiledData() => new LeafData { LeafId = 1, Int0 = Number };
        public static NodeStatus OnTick(ref VadimBurym.DodBehaviourTree.Dots.Generated.RunnerState_AuditContext state) => NodeStatus.Success;
        public static void OnEnter(ref VadimBurym.DodBehaviourTree.Dots.Generated.RunnerState_AuditContext state) { }
        public static void OnExit(ref VadimBurym.DodBehaviourTree.Dots.Generated.RunnerState_AuditContext state) { }
        public static void OnAbort(ref VadimBurym.DodBehaviourTree.Dots.Generated.RunnerState_AuditContext state) { }
    }
}
