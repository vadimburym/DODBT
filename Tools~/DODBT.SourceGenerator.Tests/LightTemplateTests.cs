using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using LightTemplate = VadimBurym.DodBehaviourTree.Light.Editor.LeafSourceTemplate;

internal static class LightTemplateTests
{
    internal static void Run()
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(p => MetadataReference.CreateFromFile(p)).Append(MetadataReference.CreateFromFile(typeof(LightTemplateTests).Assembly.Location)).ToArray();
        void Compile(string source)
        {
            var compilation = CSharpCompilation.Create("LightTemplateCheck", new[] { CSharpSyntaxTree.ParseText(source) }, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var output = new MemoryStream();
            var result = compilation.Emit(output);
            if (!result.Success) throw new Exception(source + "\n" + string.Join("\n", result.Diagnostics));
        }
        foreach (var pair in new[]
        {
            (typeof(LightFixtures.Context), typeof(LightFixtures.State)),
            (typeof(LightFixtures.Outer<int>.Context<string>), typeof(LightFixtures.Outer<int>.State<string>)),
            (typeof(LightFixtures.Context), typeof(LightFixturesOther.Context))
        })
        {
            foreach (var sample in new[] { ("NewLeaf", "Game.AI"), ("@class", "@event.@namespace"), ("NodeStatus", "LightFixtures") })
                Compile(LightTemplate.Render(sample.Item1, sample.Item2, pair.Item1, pair.Item2, typeof(LightTemplateTests).Assembly.GetTypes()));
        }
        foreach (string name in new[] { "NewContext", "NewLeafState", "@class", "BtContext", "BtContextAttribute", "BtLeafState", "BtLeafStateAttribute", "VadimBurym" })
            foreach (string space in new[] { "Game.AI", "@event", "" })
            {
                if (name == "VadimBurym" && space.Length == 0) continue; // Rejected by the creation form: this is the plugin namespace.
                Compile(LightTemplate.RenderContext(name, space));
                Compile(LightTemplate.RenderState(name, space));
            }
        Console.WriteLine("PASS Light templates compile: callbacks, namespaces, keywords, conflicting names, nested generic types and context/state pairs.");
    }
}

namespace LightFixtures
{
    public sealed class Context { }
    public struct State { }
    public sealed class Outer<T>
    {
        public sealed class Context<U> { }
        public struct State<U> { }
    }
}
namespace LightFixturesOther { public struct Context { } }
