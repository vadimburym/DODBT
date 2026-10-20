// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    internal static class LeafSourceTemplate
    {
        private const string RuntimeNamespace = "VadimBurym.DodBehaviourTree.Dots";
        private const string GeneratedNamespace = RuntimeNamespace + ".Generated";
        private static readonly HashSet<string> Keywords = new HashSet<string>((
            "abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while").Split(' '));

        internal static int FirstAvailableId(IEnumerable<int> used)
        {
            var occupied = new HashSet<int>(used);
            for (int id = 0; ; id++)
            {
                if (!occupied.Contains(id)) return id;
                if (id == int.MaxValue) throw new InvalidOperationException("No unused nonnegative int leaf IDs remain.");
            }
        }

        internal static string Escape(string name)
        {
            name = name.TrimStart('@');
            return Keywords.Contains(name) ? "@" + name : name;
        }

        internal static string Render(string className, string namespaceName, Type context, int id, IEnumerable<Type> knownTypes)
        {
            className = className.TrimStart('@');
            namespaceName = string.Join(".", namespaceName.Split('.').Select(p => p.TrimStart('@')));
            var imports = new List<string> { "System" };
            if (!string.IsNullOrEmpty(context.Namespace) && context.Namespace != namespaceName) imports.Add(context.Namespace);
            imports.Add(RuntimeNamespace);
            imports.Add(GeneratedNamespace);
            imports = imports.Distinct().ToList();
            var visible = knownTypes.Where(t => imports.Contains(t.Namespace ?? "") ||
                string.IsNullOrEmpty(t.Namespace) || namespaceName == t.Namespace || namespaceName.StartsWith(t.Namespace + ".", StringComparison.Ordinal)).ToArray();
            var names = new HashSet<string>(visible.Select(t => t.Name)) { className };
            var aliases = new List<string>();
            var referencedNames = new[] { "System.SerializableAttribute", RuntimeNamespace + ".LeafCodeGenAttribute",
                RuntimeNamespace + ".ILeaf", RuntimeNamespace + ".LeafData", RuntimeNamespace + ".NodeStatus",
                context.FullName ?? throw new ArgumentException("The context must have a full name."), GeneratedNamespace + ".RunnerState_" + context.Name };

            string Qualified(string name) => string.Join(".", name.Replace('+', '.').Split('.').Select(Escape));
            string Reference(string fullName)
            {
                string simple = fullName.Substring(Math.Max(fullName.LastIndexOf('.'), fullName.LastIndexOf('+')) + 1);
                bool conflict = simple == className || fullName.Contains("+") || visible.Any(t => t.Name == simple && t.FullName != fullName) ||
                    referencedNames.Any(n => n != fullName && n.EndsWith("." + simple, StringComparison.Ordinal));
                if (!conflict) return Escape(simple);
                string alias = "Bt" + simple;
                while (!names.Add(alias)) alias = "Bt" + alias;
                aliases.Add("using " + alias + " = global::" + Qualified(fullName) + ";");
                return alias;
            }

            string serializable = Reference("System.SerializableAttribute");
            if (serializable == "SerializableAttribute" && !names.Contains("Serializable")) serializable = "Serializable";
            string attribute = Reference(RuntimeNamespace + ".LeafCodeGenAttribute");
            if (attribute == "LeafCodeGenAttribute" && !names.Contains("LeafCodeGen")) attribute = "LeafCodeGen";
            string leaf = Reference(RuntimeNamespace + ".ILeaf");
            string data = Reference(RuntimeNamespace + ".LeafData");
            string status = Reference(RuntimeNamespace + ".NodeStatus");
            string contextName = Reference(context.FullName ?? throw new ArgumentException(nameof(context)));
            string state = Reference(GeneratedNamespace + ".RunnerState_" + context.Name);
            var code = new StringBuilder();
            foreach (string import in imports)
            {
                string first = import.Split('.')[0];
                code.Append("using ");
                if (names.Contains(first)) code.Append("global::");
                code.Append(Qualified(import)).AppendLine(";");
            }
            foreach (string alias in aliases) code.AppendLine(alias);
            code.AppendLine();
            bool hasNamespace = namespaceName.Length > 0;
            if (hasNamespace) code.Append("namespace ").AppendLine(Qualified(namespaceName)).AppendLine("{");
            string indent = hasNamespace ? "    " : "";
            string access = context.IsVisible ? "public" : "internal";
            void Line(string text = "") => code.AppendLine(text.Length == 0 ? "" : indent + text);
            Line("[" + serializable + "]");
            Line("[" + attribute + "(" + id + ", typeof(" + contextName + "))]");
            Line("public sealed class " + Escape(className) + " : " + leaf);
            Line("{");
            Line("    public " + data + " GetCompiledData() => new " + data);
            Line("    {");
            Line("        LeafId = " + id);
            Line("    };");
            Line();
            Line("    " + access + " static " + status + " OnTick(ref " + state + " state)");
            Line("    {");
            Line("        return " + status + ".Failure;");
            Line("    }");
            foreach (string method in new[] { "OnEnter", "OnExit", "OnAbort" })
            {
                Line();
                Line("    " + access + " static void " + method + "(ref " + state + " state)");
                Line("    {");
                Line();
                Line("    }");
            }
            Line("}");
            if (hasNamespace) code.AppendLine("}");
            return code.ToString();
        }
    }
}
