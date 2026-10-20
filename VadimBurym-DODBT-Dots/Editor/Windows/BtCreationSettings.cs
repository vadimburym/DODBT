// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    // Local authoring preferences, separated by project and creation window. No user assets are written here.
    internal sealed class BtCreationSettings
    {
        private readonly string _prefix;

        internal BtCreationSettings(string window) => _prefix = "DODBT.Dots.Create." + Hash128.Compute(Application.dataPath) + "." + window + ".";

        internal string Folder
        {
            get
            {
                string moved = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(_prefix + "FolderGuid", ""));
                string saved = string.IsNullOrEmpty(moved) ? EditorPrefs.GetString(_prefix + "Folder", "Assets") : moved;
                return TryAssetFolder(saved, out var folder) ? folder : "Assets";
            }
            set
            {
                EditorPrefs.SetString(_prefix + "Folder", value);
                EditorPrefs.SetString(_prefix + "FolderGuid", AssetDatabase.AssetPathToGUID(value));
            }
        }

        internal string Namespace
        {
            get => EditorPrefs.GetString(_prefix + "Namespace", "Game.AI");
            set => EditorPrefs.SetString(_prefix + "Namespace", value);
        }

        internal Type Context
        {
            get
            {
                var contexts = BtGraphContext.Available();
                string identity = EditorPrefs.GetString(_prefix + "Context", "");
                return contexts.FirstOrDefault(type => BtGraphContext.Identity(type) == identity) ??
                    (identity.Length == 0 && contexts.Length == 1 ? contexts[0] : null);
            }
            set => EditorPrefs.SetString(_prefix + "Context", BtGraphContext.Identity(value));
        }

        internal static bool TryAssetFolder(string path, out string relative)
        {
            relative = null;
            if (string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                string assets = Path.GetFullPath(Application.dataPath).Replace('\\', '/').TrimEnd('/');
                string absolute = Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
                var comparison = Application.platform == RuntimePlatform.WindowsEditor ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!absolute.Equals(assets, comparison) && !absolute.StartsWith(assets + "/", comparison)) return false;
                relative = "Assets" + absolute.Substring(assets.Length);
                return AssetDatabase.IsValidFolder(relative);
            }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
            catch (PathTooLongException) { return false; }
        }

        internal static string GraphNameError(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Enter a graph name.";
            if (name == "." || name == ".." || name.EndsWith(".", StringComparison.Ordinal) ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.IndexOfAny("<>:\"/\\|?*".ToCharArray()) >= 0)
                return "Use a file name without slashes or special characters.";
            string stem = name.Split('.')[0].ToUpperInvariant();
            if (stem == "CON" || stem == "PRN" || stem == "AUX" || stem == "NUL" ||
                stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) &&
                stem[3] >= '1' && stem[3] <= '9')
                return "This file name is reserved. Choose another name.";
            return null;
        }
    }
}
