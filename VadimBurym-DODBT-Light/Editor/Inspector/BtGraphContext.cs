// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    // Authoring metadata only. The runtime continues to use ILeaf<TContext, TLeafState>.
    internal static class BtGraphContext
    {
        internal static event Action<BtGraphAsset> Changed;
        private static Type[] _available, _states;
        private static HashSet<string> _playerAssemblies;
        private static readonly Dictionary<Type, (Type Context, Type State)[]> Contracts = new();
        private static readonly Dictionary<string, Type> ResolvedIdentities = new();

        internal static string Identity(Type type) => type == null ? "" : type.Assembly.GetName().Name + ":" + type.FullName;
        internal static string Label(Type type) => type?.FullName ?? "None";
        internal static bool ValidContext(Type type) => type != null && type.IsClass && !type.ContainsGenericParameters;
        internal static bool ValidState(Type type) => type != null && type.IsValueType && !type.IsEnum &&
            !type.IsByRefLike && !type.ContainsGenericParameters && Nullable.GetUnderlyingType(type) == null;

        internal static (Type Context, Type State)[] LeafContracts(Type leaf)
        {
            if (leaf == null) return Array.Empty<(Type, Type)>();
            if (!Contracts.TryGetValue(leaf, out var pairs))
                Contracts[leaf] = pairs = leaf.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILeaf<,>))
                    .Select(i => (Context: i.GetGenericArguments()[0], State: i.GetGenericArguments()[1]))
                    .Where(p => ValidContext(p.Context) && ValidState(p.State)).Distinct().ToArray();
            return pairs;
        }

        private static IEnumerable<Type> RuntimeLeaves() => BtTypePicker.GetTypes(typeof(ILeaf));

        private static bool IsRuntimeType(Type type)
        {
            _playerAssemblies ??= CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(a => a.name).ToHashSet();
            return _playerAssemblies.Contains(type.Assembly.GetName().Name);
        }

        internal static Type[] Available() => _available ??= RuntimeLeaves().SelectMany(LeafContracts).Select(p => p.Context)
            .Concat(TypeCache.GetTypesWithAttribute<BtContextAttribute>().Where(IsRuntimeType))
            .Where(ValidContext).Distinct().OrderBy(t => t.FullName, StringComparer.Ordinal).ToArray();

        // State types are independent of contexts. This also permits creating the first leaf for a new pair.
        internal static Type[] States() => _states ??= RuntimeLeaves().SelectMany(LeafContracts).Select(p => p.State)
            .Concat(TypeCache.GetTypesWithAttribute<BtLeafStateAttribute>().Where(IsRuntimeType))
            .Concat(Available().Select(t => t.GetCustomAttribute<BtContextAttribute>()?.LeafStateType))
            .Where(ValidState).Distinct().OrderBy(t => t.FullName, StringComparer.Ordinal).ToArray();

        private static Type Find(string identity)
        {
            if (string.IsNullOrEmpty(identity)) return null;
            if (ResolvedIdentities.TryGetValue(identity, out var cached)) return cached;
            int separator = identity.IndexOf(':');
            if (separator < 1) return null;
            try
            {
                var type = Type.GetType(identity.Substring(separator + 1) + ", " + identity.Substring(0, separator), false);
                if (type != null) ResolvedIdentities[identity] = type;
                return type;
            }
            catch (ArgumentException) { return null; }
            catch (TypeLoadException) { return null; }
        }

        private static (Type Context, Type State) Infer(BtGraphAsset graph)
        {
            if (graph == null || graph.LeafNodes == null || SerializationUtility.HasManagedReferencesWithMissingTypes(graph)) return default;
            HashSet<(Type Context, Type State)> candidates = null;
            foreach (var node in graph.LeafNodes)
            {
                if (node?.Leaf == null) continue;
                var pairs = LeafContracts(node.Leaf.GetType());
                if (candidates == null) candidates = new HashSet<(Type, Type)>(pairs);
                else candidates.IntersectWith(pairs);
            }
            return candidates?.Count == 1 ? candidates.Single() : default;
        }

        internal static Type Resolve(BtGraphAsset graph)
        {
            if (graph == null) return null;
            var type = string.IsNullOrEmpty(graph.ContextIdentity) && string.IsNullOrEmpty(graph.LeafStateIdentity)
                ? Infer(graph).Context : Find(graph.ContextIdentity);
            return ValidContext(type) ? type : null;
        }

        internal static Type ResolveState(BtGraphAsset graph)
        {
            if (graph == null) return null;
            var type = string.IsNullOrEmpty(graph.ContextIdentity) && string.IsNullOrEmpty(graph.LeafStateIdentity)
                ? Infer(graph).State : Find(graph.LeafStateIdentity);
            return ValidState(type) ? type : null;
        }

        internal static void BindLegacy(BtGraphAsset graph)
        {
            if (graph == null || !string.IsNullOrEmpty(graph.ContextIdentity) || !string.IsNullOrEmpty(graph.LeafStateIdentity)) return;
            var pair = Infer(graph);
            if (pair.Context == null) return;
            graph.ContextIdentity = Identity(pair.Context);
            graph.LeafStateIdentity = Identity(pair.State);
            EditorUtility.SetDirty(graph);
        }

        internal static bool AcceptsLeaf(BtGraphAsset graph, Type leaf)
        {
            var context = Resolve(graph);
            var state = ResolveState(graph);
            return context != null && state != null && LeafContracts(leaf).Contains((context, state));
        }

        internal static string SelectionError(BtGraphAsset graph) =>
            string.IsNullOrEmpty(graph?.ContextIdentity) && string.IsNullOrEmpty(graph?.LeafStateIdentity)
                ? "Choose a context and leaf state for this graph. All leaves must support the same pair."
                : "The graph context or leaf state is missing. Restore the type or choose another pair. Saved leaf data is preserved.";

        internal static string Warning(BtGraphAsset graph)
        {
            if (graph == null) return "";
            if (Resolve(graph) == null || ResolveState(graph) == null) return SelectionError(graph);
            var incompatible = graph.LeafNodes.Where(n => n?.Leaf != null && !AcceptsLeaf(graph, n.Leaf.GetType()))
                .Select(n => graph.FindHeader(n.Guid)?.Title ?? n.Leaf.GetType().Name).ToArray();
            return incompatible.Length == 0 ? "" : incompatible.Length + " leaf node(s) do not match context/state: " +
                string.Join(", ", incompatible.Take(12)) + ". Their data is preserved; incompatible nodes connected to Root cannot compile.";
        }

        internal static void Set(BtGraphAsset graph, Type context, Type state)
        {
            if (graph == null || !ValidContext(context) || !ValidState(state)) throw new ArgumentException("Choose a class context and a struct leaf state.");
            if (graph.ContextIdentity == Identity(context) && graph.LeafStateIdentity == Identity(state)) return;
            Undo.RegisterCompleteObjectUndo(graph, "Change BT context and leaf state");
            graph.ContextIdentity = Identity(context);
            graph.LeafStateIdentity = Identity(state);
            graph.MarkModified();
            Changed?.Invoke(graph);
        }

        internal static void DrawContext(BtGraphAsset graph)
        {
            using (new EditorGUI.DisabledScope(graph == null))
                DrawSelector("Context", Resolve(graph), Available(), chosen =>
                {
                    var state = ResolveState(graph);
                    if (state != null) Set(graph, chosen, state);
                    else BtContextPairWindow.Open(graph, chosen);
                });
        }

        internal static void DrawState(BtGraphAsset graph)
        {
            using (new EditorGUI.DisabledScope(graph == null || Resolve(graph) == null))
                DrawSelector("LeafState", ResolveState(graph), States(), chosen => Set(graph, Resolve(graph), chosen));
        }

        internal static void Draw(BtGraphAsset graph, bool showWarning = true)
        {
            DrawContext(graph);
            DrawState(graph);
            if (!showWarning || graph == null) return;
            string warning = Warning(graph);
            if (warning.Length > 0) EditorGUILayout.HelpBox(warning, MessageType.Warning);
        }

        internal static void DrawSelector(string label, Type selected, Type[] types, Action<Type> select)
        {
            var rect = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), new GUIContent(label));
            if (EditorGUI.DropdownButton(rect, new GUIContent(selected?.Name ?? "Select...", Label(selected)), FocusType.Keyboard))
                new BtTypePicker(types, select, selected, "Select " + label).Show(rect);
        }
    }

    // Choosing a pair is atomic: no half-selected graph is saved while a popup is open.
    internal sealed class BtContextPairWindow : EditorWindow
    {
        private BtGraphAsset _graph;
        private Type _context;
        private Type _state;
        internal static void Open(BtGraphAsset graph, Type context)
        {
            var window = CreateInstance<BtContextPairWindow>();
            window._graph = graph;
            window._context = context;
            window._state = ResolveInitialState(graph, context);
            window.titleContent = new GUIContent("Graph Context");
            BtCreationWindowUi.ShowCentered(window, 2);
        }
        private static Type ResolveInitialState(BtGraphAsset graph, Type context) =>
            BtGraphContext.ResolveState(graph) ?? context.GetCustomAttribute<BtContextAttribute>()?.LeafStateType;

        private void OnGUI()
        {
            if (_graph == null) { Close(); return; }
            BtCreationWindowUi.Begin(this, "Graph Context", "Choose the context and per-leaf state together.", 2);
            BtCreationWindowUi.Context(this, 0, _context, type => _context = type);
            BtCreationWindowUi.State(this, 1, _state, type => _state = type);
            if (BtCreationWindowUi.Create(this, _state == null ? "Choose a leaf state." : null, "Saved leaf data will be preserved."))
            {
                BtGraphContext.Set(_graph, _context, _state);
                Close();
            }
        }
    }
}
