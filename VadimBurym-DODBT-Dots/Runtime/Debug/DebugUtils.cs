// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Collections.Generic;

namespace VadimBurym.DodBehaviourTree.Dots
{
    #if UNITY_EDITOR
    internal static class DebugUtils
    {
        private const string Suffix = "Leaf";

        private static readonly Dictionary<Type, string> _cache = new();

        internal static string GetLeafName(ILeaf leaf)
        {
            if (leaf == null)
                return string.Empty;
            var type = leaf.GetType();
            if (_cache.TryGetValue(type, out var cached))
                return cached;
            string name = type.Name;
            if (name.EndsWith(Suffix))
                name = name.Substring(0, name.Length - Suffix.Length);
            _cache[type] = name;
            return name;
        }
    }
    #endif
}
