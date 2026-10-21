using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DODBT.SourceGenerator
{
    [Generator]
    public sealed class BtGenerator : ISourceGenerator
    {
        private const string ContextAttribute = "VadimBurym.DodBehaviourTree.Dots.GenerateBtRunnerAttribute";
        private const string LeafAttribute = "VadimBurym.DodBehaviourTree.Dots.LeafCodeGenAttribute";
        private static readonly DiagnosticDescriptor Invalid = new DiagnosticDescriptor(
            "DODBT001", "Invalid behaviour tree declaration", "{0}", "DODBT", DiagnosticSeverity.Error, true);

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            var types = new List<INamedTypeSymbol>();
            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                var model = context.Compilation.GetSemanticModel(tree);
                foreach (var declaration in tree.GetRoot(context.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    if (declaration.AttributeLists.Count == 0) continue;
                    if (model.GetDeclaredSymbol(declaration, context.CancellationToken) is INamedTypeSymbol symbol &&
                        !types.Any(t => SymbolEqualityComparer.Default.Equals(t, symbol))) types.Add(symbol);
                }
            }

            var contexts = types.Where(t => Attribute(t, ContextAttribute) != null).OrderBy(t => t.ToDisplayString(), StringComparer.Ordinal).ToArray();
            var leaves = types.Where(t => Attribute(t, LeafAttribute) != null).OrderBy(t => t.ToDisplayString(), StringComparer.Ordinal).ToArray();
            foreach (var leaf in leaves)
            {
                var attribute = Attribute(leaf, LeafAttribute)!;
                if (attribute.ConstructorArguments.Length < 2 || !(attribute.ConstructorArguments[1].Value is INamedTypeSymbol owner) ||
                    !contexts.Any(c => SymbolEqualityComparer.Default.Equals(c, owner)))
                    Error(context, leaf, "Leaf '" + leaf.Name + "' must name a context in the same assembly: [LeafCodeGen(id, typeof(Context))]. Mark that context with [GenerateBtRunner].");
            }

            foreach (var owner in contexts)
            {
                string name = owner.Name;
                if (owner.TypeKind != TypeKind.Struct || owner.IsGenericType || !owner.IsUnmanagedType || !Accessible(owner))
                {
                    Error(context, owner, "A generated BT context must be an accessible, non-generic unmanaged struct.");
                    continue;
                }
                if (contexts.Count(c => c.Name == name) != 1)
                {
                    Error(context, owner, "BT contexts in one assembly must have distinct simple names: " + name);
                    continue;
                }
                var selected = leaves.Where(l => {
                    var a = Attribute(l, LeafAttribute)!;
                    return a.ConstructorArguments.Length >= 2 && SymbolEqualityComparer.Default.Equals(a.ConstructorArguments[1].Value as ISymbol, owner);
                }).ToArray();
                var entries = new SortedDictionary<int, INamedTypeSymbol>();
                foreach (var leaf in selected)
                {
                    var idValue = Attribute(leaf, LeafAttribute)!.ConstructorArguments[0].Value;
                    if (!(idValue is int id) || id < 0) { Error(context, leaf, "Leaf ID must be a nonnegative int constant."); continue; }
                    if (entries.ContainsKey(id)) { Error(context, leaf, "Duplicate leaf ID " + id + " for context " + name + ": " + entries[id].Name + " and " + leaf.Name); continue; }
                    if (!Accessible(leaf) || leaf.IsGenericType)
                    { Error(context, leaf, "Generated leaves must be accessible and non-generic."); continue; }
                    bool valid = true;
                    foreach (string callback in new[] { "OnEnter", "OnTick", "OnExit", "OnAbort" })
                    {
                        var methods = leaf.GetMembers(callback).OfType<IMethodSymbol>().Where(m => m.IsStatic && !m.IsGenericMethod &&
                            (m.DeclaredAccessibility == Accessibility.Public || m.DeclaredAccessibility == Accessibility.Internal) &&
                            m.Parameters.Length == 1 && m.Parameters[0].RefKind == RefKind.Ref &&
                            m.Parameters[0].Type.Name == "RunnerState_" + name &&
                            (m.Parameters[0].Type.TypeKind == TypeKind.Error || m.Parameters[0].Type.ContainingNamespace.ToDisplayString() == "VadimBurym.DodBehaviourTree.Dots.Generated") &&
                            (callback == "OnTick" ? m.ReturnType.ToDisplayString() == "VadimBurym.DodBehaviourTree.Dots.NodeStatus" : m.ReturnsVoid)).ToArray();
                        if (methods.Length != 1)
                        {
                            Error(context, leaf, "Expected one accessible static " + (callback == "OnTick" ? "NodeStatus" : "void") + " " + callback + "(ref RunnerState_" + name + " state) on " + leaf.Name + ".");
                            valid = false;
                        }
                    }
                    if (valid) entries.Add(id, leaf);
                }
                string qualifiedContext = owner.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string access = PubliclyVisible(owner) ? "public" : "internal";
                Add(context, "RunnerState_" + name, Template("RunnerState").Replace("$CONTEXT$", qualifiedContext).Replace("$NAME$", name).Replace("$ACCESS$", access));
                Add(context, "BTRunner_" + name, Template("BTRunner").Replace("$CONTEXT$", qualifiedContext).Replace("$NAME$", name).Replace("$ACCESS$", access));
                Add(context, "LeafTables_" + name, Tables(owner, entries));
            }
        }

        private static bool Accessible(INamedTypeSymbol symbol)
        {
            for (var current = symbol; current != null; current = current.ContainingType)
                if (current.DeclaredAccessibility != Accessibility.Public && current.DeclaredAccessibility != Accessibility.Internal) return false;
            return true;
        }

        private static bool PubliclyVisible(INamedTypeSymbol symbol)
        {
            for (var current = symbol; current != null; current = current.ContainingType)
                if (current.DeclaredAccessibility != Accessibility.Public) return false;
            return true;
        }

        private static AttributeData? Attribute(INamedTypeSymbol symbol, string name) =>
            symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == name);

        private static void Error(GeneratorExecutionContext context, INamedTypeSymbol symbol, string message) =>
            context.ReportDiagnostic(Diagnostic.Create(Invalid, symbol.Locations.FirstOrDefault(), message));

        private static string Template(string name)
        {
            using var stream = typeof(BtGenerator).Assembly.GetManifestResourceStream("DODBT.SourceGenerator.Templates." + name + ".txt")!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static void Add(GeneratorExecutionContext context, string name, string code) =>
            context.AddSource(name + ".g.cs", SourceText.From("// <auto-generated/>\n" + code, Encoding.UTF8));

        private static string Identity(INamedTypeSymbol type)
        {
            string name = type.MetadataName;
            for (var parent = type.ContainingType; parent != null; parent = parent.ContainingType)
                name = parent.MetadataName + "+" + name;
            var parts = new Stack<string>();
            for (var ns = type.ContainingNamespace; !ns.IsGlobalNamespace; ns = ns.ContainingNamespace)
                parts.Push(ns.Name);
            return type.ContainingAssembly.Identity.Name + ":" + (parts.Count == 0 ? "" : string.Join(".", parts) + ".") + name;
        }

        private static string Literal(string value) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, true);

        private static string Tables(INamedTypeSymbol owner, SortedDictionary<int, INamedTypeSymbol> leaves)
        {
            string name = owner.Name;
            var code = new StringBuilder("namespace VadimBurym.DodBehaviourTree.Dots.Generated\n{\n    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]\n    internal static class LeafTables_" + name + "\n    {\n");
            code.Append("        [global::UnityEngine.RuntimeInitializeOnLoadMethod(global::UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]\n")
                .Append("#if UNITY_EDITOR\n        [global::UnityEditor.InitializeOnLoadMethod]\n#endif\n")
                .Append("        private static void RegisterBindings() => global::VadimBurym.DodBehaviourTree.Dots.LeafBindingRegistry.Register(")
                .Append(Literal(Identity(owner))).Append(", ResolveLeafIdentity);\n\n")
                .Append("        private static string ResolveLeafIdentity(int id)\n        {\n            switch (id)\n            {\n");
            foreach (var entry in leaves)
                code.Append("                case ").Append(entry.Key).Append(": return ").Append(Literal(Identity(entry.Value))).Append(";\n");
            code.Append("                default: return null;\n            }\n        }\n\n");
            foreach (string method in new[] { "Enter", "Tick", "Exit", "Abort" })
            {
                bool tick = method == "Tick";
                code.Append("        internal static ").Append(tick ? "NodeStatus" : "void").Append(' ').Append(method)
                    .Append("Leaf(int id, ref RunnerState_").Append(name).Append(" state)\n        {\n            switch (id)\n            {\n");
                foreach (var entry in leaves)
                    code.Append("                case ").Append(entry.Key).Append(": ").Append(tick ? "return " : "")
                        .Append(entry.Value.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(".On").Append(method)
                        .Append("(ref state);").Append(tick ? "\n" : " break;\n");
                if (tick) code.Append("                default: return NodeStatus.None;\n");
                code.Append("            }\n        }\n");
            }
            return code.Append("    }\n}\n").ToString();
        }
    }
}
