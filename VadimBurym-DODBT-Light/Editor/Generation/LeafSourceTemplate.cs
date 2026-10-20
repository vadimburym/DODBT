// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    internal static class LeafSourceTemplate
    {
        private const string Runtime = "VadimBurym.DodBehaviourTree.Light";
        private static readonly HashSet<string> Keywords = new((
            "abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while").Split(' '));

        internal static bool Identifier(string value) => Regex.IsMatch(value ?? "", @"^@?[A-Za-z_][A-Za-z0-9_]*$");
        internal static string Escape(string name) => Keywords.Contains(name.TrimStart('@')) ? "@" + name.TrimStart('@') : name.TrimStart('@');
        internal static string Namespace(string value) => string.Join(".", value.Split('.').Select(Escape));
        internal static string FullName(string name, string space) => string.IsNullOrEmpty(space) ? name.TrimStart('@') :
            string.Join(".", space.Split('.').Select(p => p.TrimStart('@'))) + "." + name.TrimStart('@');

        internal static string TypeName(Type type)
        {
            if (type.IsArray) return TypeName(type.GetElementType() ?? throw new ArgumentException("Missing array element type.")) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            var args = type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes;
            string name = (type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName ?? throw new ArgumentException("A named type is required.");
            int argument = 0;
            var parts = name.Replace('+', '.').Split('.').Select(part =>
            {
                int tick = part.IndexOf('`');
                if (tick < 0) return Escape(part);
                int count = int.Parse(part.Substring(tick + 1));
                string value = Escape(part.Substring(0, tick)) + "<" +
                    string.Join(", ", args.Skip(argument).Take(count).Select(TypeName)) + ">";
                argument += count;
                return value;
            });
            return "global::" + string.Join(".", parts);
        }

        internal static string Render(string name, string space, Type context, Type state, IEnumerable<Type> knownTypes)
        {
            name = name.TrimStart('@');
            space = string.Join(".", space.Split('.').Select(p => p.TrimStart('@')));
            var imports = new[] { "System", context.Namespace, state.Namespace, Runtime }
                .OfType<string>().Where(s => !string.IsNullOrEmpty(s) && s != space).Distinct().ToArray();
            var visible = knownTypes.Where(t => imports.Contains(t.Namespace ?? "") || string.IsNullOrEmpty(t.Namespace) ||
                space == t.Namespace || space.StartsWith(t.Namespace + ".", StringComparison.Ordinal)).ToArray();
            var used = visible.Select(t => t.Name).Append(name).ToHashSet();
            var aliases = new List<string>();
            string Reference(Type type)
            {
                bool conflict = type.IsNested || type.IsGenericType || type.Name == name || visible.Any(t => t.Name == type.Name && t != type);
                if (!conflict) return Escape(type.Name);
                string alias = "Bt" + type.Name.Split('`')[0];
                while (!used.Add(alias)) alias = "Bt" + alias;
                aliases.Add("using " + alias + " = " + TypeName(type) + ";");
                return alias;
            }
            string serializable = Reference(typeof(SerializableAttribute));
            if (serializable == "SerializableAttribute" && !used.Contains("Serializable")) serializable = "Serializable";
            string status = Reference(typeof(NodeStatus));
            string contextName = Reference(context);
            string stateName = context == state ? contextName : Reference(state);
            string leaf = visible.Any(t => t.Name == "ILeaf`2" && t != typeof(ILeaf<,>))
                ? "global::" + Runtime + ".ILeaf" : "ILeaf";
            var code = new StringBuilder();
            foreach (var import in imports) code.Append("using ").Append(used.Contains(import.Split('.')[0]) ? "global::" : "")
                .Append(Namespace(import)).AppendLine(";");
            foreach (var alias in aliases) code.AppendLine(alias);
            code.AppendLine();
            if (space.Length > 0) code.Append("namespace ").AppendLine(Namespace(space)).AppendLine("{");
            string indent = space.Length > 0 ? "    " : "";
            void Line(string text = "") => code.AppendLine(text.Length == 0 ? "" : indent + text);
            Line("[" + serializable + "]");
            Line((context.IsVisible && state.IsVisible ? "public" : "internal") + " sealed class " + Escape(name) +
                " : " + leaf + "<" + contextName + ", " + stateName + ">");
            Line("{");
            string parameters = contextName + " context, ref " + stateName + " state";
            Line("    public " + status + " OnTick(" + parameters + ")");
            Line("    {");
            Line("        return " + status + ".Failure;");
            Line("    }");
            foreach (string method in new[] { "OnEnter", "OnExit", "OnAbort" })
            {
                Line();
                Line("    public void " + method + "(" + parameters + (method == "OnExit" ? ", " + status + " exitStatus" : "") + ")");
                Line("    {");
                Line();
                Line("    }");
            }
            Line("}");
            if (space.Length > 0) code.AppendLine("}");
            return code.ToString();
        }

        internal static string RenderContext(string name, string space) => RenderDataType(name, space, false);
        internal static string RenderState(string name, string space) => RenderDataType(name, space, true);

        private static string RenderDataType(string name, string space, bool state)
        {
            var code = new StringBuilder();
            code.Append("using global::").Append(Runtime).AppendLine(";").AppendLine();
            bool nested = !string.IsNullOrEmpty(space);
            if (nested) code.Append("namespace ").AppendLine(Namespace(space)).AppendLine("{");
            string indent = nested ? "    " : "";
            void Line(string text = "") => code.AppendLine(text.Length == 0 ? "" : indent + text);
            string attribute = state ? "BtLeafState" : "BtContext";
            bool conflict = name.TrimStart('@') == attribute || name.TrimStart('@') == attribute + "Attribute";
            Line("// Makes this type available in editor selectors before any leaves exist.");
            Line("[" + (conflict ? "global::" + Runtime + "." + attribute + "Attribute" : attribute) + "]");
            Line("public " + (state ? "struct " : "sealed class ") + Escape(name));
            Line("{");
            if (state)
            {
                Line("    // Each leaf has a separate state for each agent.");
                Line("    // Add counters, timers or progress that must survive between ticks.");
                Line("    // Shared leaf objects must not store per-agent state.");
            }
            else
            {
                Line("    // Populate this object before passing it to BehaviourTree.Tick.");
                Line("    // Add your agent, movement, sensing or gameplay service references here.");
            }
            Line("}");
            if (nested) code.AppendLine("}");
            // Use a normal import unless the new type shadows the namespace root.
            return code.ToString().Replace("using global::" + Runtime,
                "using " + (name.TrimStart('@') == "VadimBurym" ? "global::" : "") + Runtime);
        }
    }
}
