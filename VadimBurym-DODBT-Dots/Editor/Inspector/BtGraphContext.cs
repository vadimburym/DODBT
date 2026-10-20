// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEngine;
    using VadimBurym.DodBehaviourTree.Dots;

    // Context selection is authoring metadata. The runtime still uses the compiled binding manifest.
    internal static class BtGraphContext
    {
        internal static event Action<BtGraphAsset> Changed;
        private static Type[] _available;
        private static readonly Dictionary<Type, Type> LeafContexts = new();
        private static readonly Dictionary<string, Type> ResolvedIdentities = new();

        internal static string Identity(Type type) => type.Assembly.GetName().Name + ":" + type.FullName;
        internal static string Label(Type type) => type.FullName + " (" + type.Assembly.GetName().Name + ")";
        internal static Type LeafContext(Type leaf)
        {
            if (leaf == null) return null;
            if (!LeafContexts.TryGetValue(leaf, out var context))
                LeafContexts[leaf] = context = leaf.GetCustomAttribute<LeafCodeGenAttribute>()?.ContextType;
            return context;
        }
        private static bool Valid(Type type) => type != null && type.IsValueType && !type.ContainsGenericParameters &&
            type.IsDefined(typeof(GenerateBtRunnerAttribute), false);

        internal static Type[] Available()
        {
            if (_available != null) return _available;
            var player = CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(a => a.name).ToHashSet();
            return _available = TypeCache.GetTypesWithAttribute<GenerateBtRunnerAttribute>()
                .Where(t => Valid(t) && player.Contains(t.Assembly.GetName().Name))
                .OrderBy(t => t.FullName, StringComparer.Ordinal).ThenBy(t => t.Assembly.GetName().Name, StringComparer.Ordinal).ToArray();
        }

        internal static Type Resolve(BtGraphAsset graph)
        {
            if (graph == null) return null;
            if (!string.IsNullOrEmpty(graph.ContextIdentity))
            {
                if (ResolvedIdentities.TryGetValue(graph.ContextIdentity, out var cached)) return cached;
                int separator = graph.ContextIdentity.IndexOf(':');
                if (separator < 1) return null;
                try
                {
                    var type = Type.GetType(graph.ContextIdentity.Substring(separator + 1) + ", " + graph.ContextIdentity.Substring(0, separator), false);
                    if (!Valid(type)) return null;
                    ResolvedIdentities[graph.ContextIdentity] = type;
                    return type;
                }
                catch (ArgumentException) { return null; }
                catch (TypeLoadException) { return null; }
            }
            // Legacy graphs may be inferred only when every assigned leaf agrees, including detached nodes.
            Type inferred = null;
            if (graph.LeafNodes == null || SerializationUtility.HasManagedReferencesWithMissingTypes(graph)) return null;
            foreach (var node in graph.LeafNodes)
            {
                if (node?.Leaf == null) continue;
                var type = LeafContext(node.Leaf.GetType());
                if (!Valid(type) || inferred != null && inferred != type) return null;
                inferred = type;
            }
            return inferred;
        }

        internal static void BindLegacy(BtGraphAsset graph)
        {
            if (graph == null || !string.IsNullOrEmpty(graph.ContextIdentity)) return;
            var type = Resolve(graph);
            if (type == null) return;
            graph.ContextIdentity = Identity(type);
            // Inferring existing intent does not invalidate an already compiled tree.
            EditorUtility.SetDirty(graph);
        }

        internal static bool AcceptsLeaf(BtGraphAsset graph, Type leaf)
        {
            var context = Resolve(graph);
            return context != null && LeafContext(leaf) == context;
        }

        internal static string SelectionError(BtGraphAsset graph) => string.IsNullOrEmpty(graph.ContextIdentity)
            ? "Select a graph context. All leaves in one tree must use the same generated context."
            : "The graph context is missing or no longer marked [GenerateBtRunner]: " + graph.ContextIdentity + ". Restore it or select another context.";

        internal static string Warning(BtGraphAsset graph)
        {
            if (graph == null) return "";
            var context = Resolve(graph);
            if (context == null) return SelectionError(graph);
            var incompatible = graph.LeafNodes?.Where(n => n?.Leaf != null && LeafContext(n.Leaf.GetType()) != context)
                .Select(n => graph.FindHeader(n.Guid)?.Title ?? n.Leaf.GetType().Name).ToArray() ?? Array.Empty<string>();
            return incompatible.Length == 0 ? "" : incompatible.Length + " leaf node(s) do not match context " + context.FullName +
                ": " + string.Join(", ", incompatible.Take(12)) + (incompatible.Length > 12 ? ", ..." : "") +
                ". Their data is preserved. Replace them or change the context; incompatible nodes connected to Root cannot compile.";
        }

        internal static void Set(BtGraphAsset graph, Type context)
        {
            if (graph == null || !Valid(context)) throw new ArgumentException("Choose a context marked [GenerateBtRunner].");
            string identity = Identity(context);
            if (graph.ContextIdentity == identity) return;
            Undo.RegisterCompleteObjectUndo(graph, "Change BT graph context");
            graph.ContextIdentity = identity;
            graph.MarkModified();
            Changed?.Invoke(graph);
        }

        internal static void DrawSelector(Type selected, Action<Type> select, string placeholder = "Select context...", string tooltip = null)
        {
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, new GUIContent("Context"));
            var content = new GUIContent(selected?.Name ?? placeholder, selected == null ? tooltip : Label(selected));
            if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard))
                new BtTypePicker(Available(), select, selected).Show(rect);
        }

        internal static void Draw(BtGraphAsset graph, bool showWarning = true)
        {
            var selected = Resolve(graph);
            using (new EditorGUI.DisabledScope(graph == null))
                DrawSelector(selected, context =>
                {
                    if (graph != null) Set(graph, context);
                }, string.IsNullOrEmpty(graph?.ContextIdentity) ? "Select context..." : "Missing context", graph?.ContextIdentity);
            if (showWarning && graph != null)
            {
                string warning = Warning(graph);
                if (warning.Length > 0) EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }
    }
}
