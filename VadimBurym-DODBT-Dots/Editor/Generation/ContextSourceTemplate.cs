// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    internal static class ContextSourceTemplate
    {
        internal static bool Identifier(string value) => Regex.IsMatch(value ?? "", @"^@?[A-Za-z_][A-Za-z0-9_]*$");

        internal static string FullName(string name, string namespaceName) =>
            string.IsNullOrEmpty(namespaceName) ? name.TrimStart('@') :
                string.Join(".", namespaceName.Split('.').Select(p => p.TrimStart('@'))) + "." + name.TrimStart('@');

        internal static string Render(string name, string namespaceName)
        {
            var code = new StringBuilder();
            code.AppendLine("using Unity.Entities;").AppendLine();
            bool hasNamespace = !string.IsNullOrEmpty(namespaceName);
            if (hasNamespace)
                code.Append("namespace ").AppendLine(string.Join(".", namespaceName.Split('.').Select(LeafSourceTemplate.Escape))).AppendLine("{");
            string indent = hasNamespace ? "    " : "";
            void Line(string text) => code.AppendLine(indent + text);
            Line("// Provides the ECS data that this tree's leaves need.");
            Line("[VadimBurym.DodBehaviourTree.Dots.GenerateBtRunner]");
            Line("public struct " + LeafSourceTemplate.Escape(name));
            Line("{");
            Line("    // Populate the context in your tick system before passing it to the runner.");
            Line("    // Leaves access these fields through state.Context.");
            Line("    // Uncomment the fields you need:");
            Line("    // public EntityCommandBuffer.ParallelWriter Ecb;");
            Line("    // public EntityStorageInfoLookup EntityInfoLookup;");
            code.AppendLine();
            Line("    // Add ComponentLookup<T> fields for other components your leaves use.");
            Line("    // Refresh cached lookups before scheduling jobs; use read-only access where possible.");
            Line("    // Keep the context unmanaged and store persistent agent state in ECS components.");
            Line("}");
            if (hasNamespace) code.AppendLine("}");
            return code.ToString();
        }
    }
}
