using System;
using System.IO;
using System.Linq;
using DODBT.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VadimBurym.DodBehaviourTree.Dots.Editor;

internal static class ContextTemplateTests
{
    internal static void Run()
    {
        const string support = @"
namespace Unity.Entities { public struct EntityCommandBuffer { public struct ParallelWriter {} } public struct EntityStorageInfoLookup {} }
namespace VadimBurym.DodBehaviourTree.Dots { public sealed class GenerateBtRunnerAttribute : System.Attribute {} }";
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p)).ToArray();
        foreach (var sample in new[] { ("NewContext", "Game.AI"), ("NewContext", ""), ("@struct", "@event.@namespace") })
        {
            string source = ContextSourceTemplate.Render(sample.Item1, sample.Item2);
            var tree = CSharpSyntaxTree.ParseText(source);
            if (tree.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>().Single().Members.Count != 0)
                throw new Exception("The context must start empty; example fields must remain comments.");
            foreach (bool uncomment in new[] { false, true })
            {
                string text = uncomment ? source.Replace("// public ", "public ") : source;
                var compilation = CSharpCompilation.Create("ContextTemplateCheck", new[] { CSharpSyntaxTree.ParseText(text), CSharpSyntaxTree.ParseText(support) }, references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                using var output = new MemoryStream();
                var result = compilation.Emit(output);
                if (!result.Success) throw new Exception(text + "\n" + string.Join("\n", result.Diagnostics));
                var generated = CSharpGeneratorDriver.Create(new BtGenerator()).RunGenerators(compilation).GetRunResult();
                if (generated.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error) || generated.GeneratedTrees.Length != 3 ||
                    generated.GeneratedTrees.Any(t => t.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)))
                    throw new Exception("The context template failed BT source generation: " + string.Join("\n", generated.Diagnostics));
                Console.WriteLine("PASS Context template compiles and generates runner, state and tables: " + sample + ", example fields=" + uncomment);
            }
        }
    }
}
