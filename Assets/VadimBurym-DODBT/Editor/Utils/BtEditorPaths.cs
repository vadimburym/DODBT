// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System.IO;
using UnityEditor;

internal static class BtEditorPaths
{
    internal static string GetAssetsFolderPath()
    {
        return "Assets/VadimBurym-DODBT/CompiledAssets";
        //string editorFolder = GetEditorWindowFolderPath();
        //string parentUnityPath = Path.GetDirectoryName(editorFolder)?.Replace('\\', '/');
        //string parentPath = Path.GetDirectoryName(parentUnityPath)?.Replace('\\', '/');
        //if (string.IsNullOrEmpty(parentPath))
        //    return "Assets/Assets";
        //return $"{parentPath}/Assets";
    }
    
    internal static string GetEditorAssetsFolderPath()
    {
        return "Assets/VadimBurym-DODBT/GraphAssets";
        //string editorFolder = GetEditorWindowFolderPath();
        //string parentUnityPath = Path.GetDirectoryName(editorFolder)?.Replace('\\', '/');
        //if (string.IsNullOrEmpty(parentUnityPath))
        //    return "Assets/Editor/Assets";
        //return $"{parentUnityPath}/Assets";
    }
    
    internal static string GetEditorWindowFolderPath()
    {
        string[] guids = AssetDatabase.FindAssets("BtEditorWindow t:MonoScript");
        if (guids == null || guids.Length == 0)
            return "Assets";
        string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        return Path.GetDirectoryName(scriptPath)?.Replace('\\', '/') ?? "Assets";
    }
    
    internal static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;
        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolderExists(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}