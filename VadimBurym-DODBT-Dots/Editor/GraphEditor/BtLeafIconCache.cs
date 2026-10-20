// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    [InitializeOnLoad]
    internal static class BtLeafIconCache
    {
        // Cache misses as well as hits. Neither reflection nor AssetDatabase runs in node repaint code.
        private static readonly Dictionary<Type, Texture2D> Icons = new();
        private static readonly Dictionary<string, Texture2D> Assets = new(StringComparer.Ordinal);
        internal static event Action Changed;

        static BtLeafIconCache() => EditorApplication.projectChanged += Invalidate;

        internal static Texture2D Get(Type leafType)
        {
            if (leafType == null || !typeof(ILeaf).IsAssignableFrom(leafType)) return null;
            if (Icons.TryGetValue(leafType, out var icon)) return icon;
            var reference = leafType.GetCustomAttribute<LeafIconAttribute>(true)?.Asset?.Trim();
            icon = Resolve(reference);
            Icons.Add(leafType, icon);
            return icon;
        }

        internal static Texture2D Resolve(string reference)
        {
            if (string.IsNullOrEmpty(reference)) return null;
            if (Assets.TryGetValue(reference, out var icon)) return icon;
            string path = reference.Replace('\\', '/');
            if (Guid.TryParseExact(path, "N", out _)) path = AssetDatabase.GUIDToAssetPath(path);
            icon = !string.IsNullOrEmpty(path) &&
                (path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("Packages/", StringComparison.Ordinal))
                ? AssetDatabase.LoadAssetAtPath<Texture2D>(path) : null;
            Assets.Add(reference, icon);
            return icon;
        }

        internal static void Invalidate()
        {
            Icons.Clear();
            Assets.Clear();
            Changed?.Invoke();
        }
    }
}
