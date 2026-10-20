// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;

namespace VadimBurym.DodBehaviourTree.Light
{
    /// <summary>Exposes a context in editor selectors before any leaves exist. An optional default state is an authoring suggestion, not a runtime restriction.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class BtContextAttribute : Attribute
    {
        public Type LeafStateType { get; } = null!;

        public BtContextAttribute() { }

        public BtContextAttribute(Type leafStateType) => LeafStateType = leafStateType;
    }
}
