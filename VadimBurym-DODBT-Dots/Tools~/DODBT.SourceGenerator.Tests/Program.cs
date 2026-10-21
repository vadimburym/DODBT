using System;
using System.IO;
using System.Linq;
using DODBT.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal static class Program
{
    private const string Source = @"
using System;
using VadimBurym.DodBehaviourTree.Dots;
using VadimBurym.DodBehaviourTree.Dots.Generated;
namespace VadimBurym.DodBehaviourTree.Dots {
 public sealed class GenerateBtRunnerAttribute : Attribute { }
 public sealed class LeafCodeGenAttribute : Attribute { public LeafCodeGenAttribute(int id, Type context) {} }
 public enum NodeStatus { None, Failure, Success, Running }
}
namespace Sample {
 [GenerateBtRunner] public struct Context { public int Data; }
 [LeafCodeGen(7, typeof(Context))] public sealed class Idle {
  public static NodeStatus OnTick(ref RunnerState_Context state) => NodeStatus.Success;
  public static void OnEnter(ref RunnerState_Context state) {}
  public static void OnExit(ref RunnerState_Context state) {}
  public static void OnAbort(ref RunnerState_Context state) {}
 }
}";

    private static CSharpCompilation Compilation(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        return CSharpCompilation.Create("GeneratorTests", new[] { CSharpSyntaxTree.ParseText(source) }, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static GeneratorDriverRunResult Generate(string source) =>
        CSharpGeneratorDriver.Create(new BtGenerator()).RunGenerators(Compilation(source)).GetRunResult();

    private static void Require(bool value, string name)
    {
        if (!value) throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }

    public static void Main(string[] args)
    {
        ContextTemplateTests.Run();
        LightTemplateTests.Run();
        if (args.Contains("--context-template")) return;
        LeafTemplateTests.Run();
        var cold = Generate(Source);
        Require(cold.Diagnostics.Length == 0, "No generator diagnostics with unresolved generated runner types");
        Require(cold.GeneratedTrees.Length == 3, "Cold compilation generates state, runner and dispatch");
        Require(cold.GeneratedTrees.All(t => !t.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)), "Generated source has valid syntax");
        string table = cold.GeneratedTrees.Single(t => t.FilePath.Contains("LeafTables")).ToString();
        Require(table.Contains("case 7: global::Sample.Idle.OnAbort(ref state);"), "Every leaf, including the final ID, has abort dispatch");
        foreach (int id in new[] { 256, 65536, int.MaxValue })
        {
            var wide = Generate(Source.Replace("LeafCodeGen(7,", "LeafCodeGen(" + id + ","));
            Require(wide.Diagnostics.Length == 0 && wide.GeneratedTrees.Single(t => t.FilePath.Contains("LeafTables")).ToString().Contains("case " + id + ":"), "Int dispatch ID " + id);
        }
        Require(Generate(Source.Replace("LeafCodeGen(7,", "LeafCodeGen(-1,")).Diagnostics.Any(d => d.GetMessage().Contains("nonnegative")), "Negative IDs are rejected");
        Require(table.Contains("GeneratorTests:Sample.Context") && table.Contains("GeneratorTests:Sample.Idle") && table.Contains("SubsystemRegistration"), "Dispatch registers exact context and leaf identities before player scene loading");
        var changed = Generate(Source.Replace("class Idle", "class Replacement"));
        var changedTable = changed.GeneratedTrees.Single(t => t.FilePath.Contains("LeafTables")).ToString();
        Require(changedTable.Contains("GeneratorTests:Sample.Replacement") && !changedTable.Contains("GeneratorTests:Sample.Idle"), "Reusing an ID updates its generated identity");
        var repeat = Generate(Source);
        Require(cold.GeneratedTrees.Select(t => t.ToString()).SequenceEqual(repeat.GeneratedTrees.Select(t => t.ToString())), "Generation is deterministic");
        var unrelatedError = Generate(Source + " class Broken { MissingUserType field; }");
        Require(unrelatedError.GeneratedTrees.Length == 3, "Unrelated compile errors do not block generation");
        var duplicate = Generate(Source + " namespace Sample { [LeafCodeGen(7, typeof(Context))] public class Other {} }");
        Require(duplicate.Diagnostics.Any(d => d.GetMessage().Contains("Duplicate leaf ID 7")), "Duplicate IDs report a source diagnostic");
        Require(duplicate.Diagnostics.All(d => d.Location.IsInSource), "Diagnostics point at user declarations");
        var missingMethod = Generate(Source.Replace("public static void OnAbort", "private static void OnAbort"));
        Require(missingMethod.Diagnostics.Any(d => d.GetMessage().Contains("OnAbort")), "Inaccessible lifecycle callback is rejected");
        var managed = Generate(Source.Replace("public int Data;", "public string Data;"));
        Require(managed.Diagnostics.Any(d => d.GetMessage().Contains("unmanaged")), "Managed context is rejected before Burst compilation");
        var noOwner = Generate(Source.Replace("[GenerateBtRunner]", ""));
        Require(noOwner.Diagnostics.Any(d => d.GetMessage().Contains("Mark that context")), "Missing context marker is diagnosed");
        var internalOwner = Generate(Source.Replace("public struct Context", "internal struct Context").Replace("public static", "internal static"));
        Require(internalOwner.Diagnostics.Length == 0 && internalOwner.GeneratedTrees.All(t => t.ToString().Contains("internal ")),
            "Internal contexts generate internal API without exposing inaccessible types");
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BtGenerator()).RunGenerators(Compilation(Source));
        driver = driver.RunGeneratorsAndUpdateCompilation(Compilation("public struct Unrelated {}"), out var removed, out var removalDiagnostics);
        Require(driver.GetRunResult().GeneratedTrees.Length == 0, "Deleting a context and its users removes all its generated sources on the same driver");
        Require(removalDiagnostics.Length == 0 && !removed.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error),
            "Removing the context and all references leaves a valid compilation");
        driver = driver.RunGeneratorsAndUpdateCompilation(Compilation(Source.Replace("[GenerateBtRunner] public struct Context { public int Data; }", "")), out var dangling, out var danglingDiagnostics);
        Require(driver.GetRunResult().GeneratedTrees.Length == 0 && danglingDiagnostics.Any(d => d.Id == "DODBT001"),
            "Deleting only the context does not retain a stale runner and diagnoses orphan leaves");
        Require(dangling.GetDiagnostics().Any(d => d.Id == "CS0246"), "References to the deleted context still produce ordinary C# errors");
        driver = driver.RunGenerators(Compilation(Source));
        Require(driver.GetRunResult().GeneratedTrees.Length == 3 && driver.GetRunResult().Diagnostics.Length == 0,
            "Restoring the context regenerates its sources after a failed compilation");
    }
}
