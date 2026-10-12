using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using VadimBurym.DodBehaviourTree;

internal sealed class BtBuildCompatibility : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var target = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
        bool enabled = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Contains("DODBT_DECORATORS");
        var roots = new HashSet<string>(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));
        foreach (var path in AssetDatabase.GetAllAssetPaths())
            if (path.Contains("/Resources/") && !path.Contains("/Editor/")) roots.Add(path);
        foreach (var asset in PlayerSettings.GetPreloadedAssets())
            if (asset != null) roots.Add(AssetDatabase.GetAssetPath(asset));
        var treePaths = new HashSet<string>(AssetDatabase.FindAssets("t:BehaviourTreeAsset").Select(AssetDatabase.GUIDToAssetPath));
        foreach (var path in AssetDatabase.GetDependencies(roots.ToArray(), true))
        {
            // Loading scene or DLL subassets here invokes unrelated importers during the build.
            if (!treePaths.Contains(path)) continue;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is not BehaviourTreeAsset tree) continue;
                string error = tree.CompatibilityError(enabled);
                if (!string.IsNullOrEmpty(error)) throw new BuildFailedException(path + ": " + error);
            }
        }
    }
}
