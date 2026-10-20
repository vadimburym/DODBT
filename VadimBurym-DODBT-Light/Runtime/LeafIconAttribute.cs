// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Diagnostics;

namespace VadimBurym.DodBehaviourTree.Light
{
    /// <summary>Optional editor node icon: a texture asset GUID (recommended), or an Assets/Packages path.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public sealed class LeafIconAttribute : Attribute
    {
        public string Asset { get; }
        public LeafIconAttribute(string asset) => Asset = asset;
    }
}
