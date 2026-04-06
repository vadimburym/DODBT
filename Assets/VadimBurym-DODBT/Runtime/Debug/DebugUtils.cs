// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace VadimBurym.DodBehaviourTree
{
    #if UNITY_EDITOR
    internal static class DebugUtils
    {
        private const string Suffix = "Leaf";

        private static readonly Dictionary<Type, string> _cache = new();

        public static string GetLeafName(ILeaf leaf)
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