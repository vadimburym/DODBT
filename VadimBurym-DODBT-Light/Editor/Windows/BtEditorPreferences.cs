// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using UnityEditor;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    internal static class BtEditorPreferences
    {
        private static readonly string Key = "DODBT.Light.NodeIcons." + Hash128.Compute(Application.dataPath);
        private static bool _useNodeIcons = EditorPrefs.GetBool(Key, false);
        internal static event Action Changed;

        internal static bool UseNodeIcons
        {
            get => _useNodeIcons;
            set
            {
                if (_useNodeIcons == value) return;
                _useNodeIcons = value;
                EditorPrefs.SetBool(Key, value);
                Changed?.Invoke();
            }
        }
    }
}
