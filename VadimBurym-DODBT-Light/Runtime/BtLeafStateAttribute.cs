// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;

namespace VadimBurym.DodBehaviourTree.Light
{
    /// <summary>Exposes a per-leaf state struct in editor selectors before any leaves exist.</summary>
    [AttributeUsage(AttributeTargets.Struct, Inherited = false)]
    public sealed class BtLeafStateAttribute : Attribute
    {
    }
}
