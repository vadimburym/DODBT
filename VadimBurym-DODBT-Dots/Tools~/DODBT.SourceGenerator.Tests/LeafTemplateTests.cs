using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VadimBurym.DodBehaviourTree.Dots.Editor;

namespace TemplateSamples
{
    public struct Context { }
    public struct NodeStatus { }
    public class Outer { internal struct Nested { } }
}
namespace @event { public struct @struct { } }

internal static class LeafTemplateTests
{
    public static void Run()
    {
        if (LeafSourceTemplate.FirstAvailableId(new[] { 0, 2, int.MaxValue }) != 1 ||
            LeafSourceTemplate.FirstAvailableId(Enumerable.Range(0, 300)) != 300)
            throw new Exception("ID allocation did not reuse a gap or stopped at 256.");
        Console.WriteLine("PASS Lowest free int ID is reused without byte limits");
        foreach (var sample in new[] {
            (typeof(TemplateSamples.Context), "NewLeaf", "Game.AI"),
            (typeof(TemplateSamples.Context), "Context", "Game.AI"),
            (typeof(TemplateSamples.NodeStatus), "NewLeaf", "Game.AI"),
            (typeof(TemplateSamples.Outer.Nested), "NewLeaf", "Game.AI"),
            (typeof(@event.@struct), "@event", "@class.@namespace"),
            (typeof(TemplateSamples.Context), "NewLeaf", "") })
        {
            string text = LeafSourceTemplate.Render(sample.Item2, sample.Item3, sample.Item1, 65536,
                new[] { typeof(TemplateSamples.Context), typeof(TemplateSamples.NodeStatus), typeof(TemplateSamples.Outer), typeof(TemplateSamples.Outer.Nested), typeof(@event.@struct) });
            string support = @"
namespace TemplateSamples { public struct Context {} public struct NodeStatus {} public class Outer { internal struct Nested {} } }
namespace @event { public struct @struct {} }
namespace VadimBurym.DodBehaviourTree.Dots {
 public class LeafCodeGenAttribute : System.Attribute { public LeafCodeGenAttribute(int id, System.Type context) {} }
 public interface ILeaf { LeafData GetCompiledData(); }
 public struct LeafData { public int LeafId; }
 public enum NodeStatus { Failure }
}
namespace VadimBurym.DodBehaviourTree.Dots.Generated {
 public struct RunnerState_Context {} public struct RunnerState_NodeStatus {} internal struct RunnerState_Nested {} public struct RunnerState_struct {}
}";
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p));
            var compilation = CSharpCompilation.Create("TemplateCheck", new[] { CSharpSyntaxTree.ParseText(text), CSharpSyntaxTree.ParseText(support) }, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var output = new MemoryStream();
            var result = compilation.Emit(output);
            if (!result.Success) throw new Exception(text + "\n" + string.Join("\n", result.Diagnostics));
            Console.WriteLine("PASS Template compiles: " + sample);
        }
        string normal = LeafSourceTemplate.Render("NewLeaf", "Game.AI", typeof(TemplateSamples.Context), 0, Array.Empty<Type>());
        if (normal.Contains("@") || normal.Contains("global::") || !normal.Contains("ref RunnerState_Context state") || !normal.Contains("using TemplateSamples;"))
            throw new Exception("Ordinary template should use short readable names.");
        Console.WriteLine("PASS Normal template uses readable names and multiline method bodies");
    }
}
