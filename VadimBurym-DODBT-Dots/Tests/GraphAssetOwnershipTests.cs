// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using VadimBurym.DodBehaviourTree.Dots.Editor;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class GraphAssetOwnershipTests
    {
        [UnityTest]
        public IEnumerator DeleteConfirmationKeepsItsOriginalTargetAfterSwitchingGraphs()
        {
            string folder = "Assets/__BTDeleteTarget_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring(7));
            BtEditorWindow window = null;
            BtConfirmDeleteWindow confirmation = null;
            try
            {
                BtGraphAsset Create(string name)
                {
                    var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
                    graph.RootNode = new BtRootNodeData { Guid = name + "-root" };
                    AssetDatabase.CreateAsset(graph, folder + "/" + name + ".asset");
                    BtGraphAssets.EnsureCompiledAsset(graph);
                    return graph;
                }
                var first = Create("First");
                var second = Create("Second");
                BtEditorWindow.OpenWithAsset(first);
                yield return null;
                window = Resources.FindObjectsOfTypeAll<BtEditorWindow>().Single();
                typeof(BtEditorWindow).GetMethod("OpenDeleteAssetWindow", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(window, null);
                confirmation = Resources.FindObjectsOfTypeAll<BtConfirmDeleteWindow>().Single();
                window.SetCurrentGraphAsset(second);
                var confirm = (Action)typeof(BtConfirmDeleteWindow).GetField("onYes", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(confirmation);
                confirm();
                Assert.That(AssetDatabase.LoadAssetAtPath<BtGraphAsset>(folder + "/First.asset"), Is.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<BtGraphAsset>(folder + "/Second.asset"), Is.SameAs(second));
                Assert.That(second.CompiledAsset, Is.Not.Null);
                Assert.That(typeof(BtEditorWindow).GetField("_currentAsset", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(window), Is.SameAs(second));
            }
            finally
            {
                if (confirmation != null) confirmation.Close();
                if (window != null) window.Close();
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void NativeDuplicateCannotDeleteOriginalCompiledAssetAndGetsItsOwnBeforeCompilation()
        {
            string folder = "Assets/__BTAssetOwnership_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring(7));
            try
            {
                var source = ScriptableObject.CreateInstance<BtGraphAsset>();
                AssetDatabase.CreateAsset(source, folder + "/Original.asset");
                var originalCompiled = BtGraphAssets.EnsureCompiledAsset(source);
                string originalPath = AssetDatabase.GetAssetPath(originalCompiled);
                AssetDatabase.SaveAssets();
                string duplicatePath = folder + "/Duplicate.asset";
                Assert.That(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), duplicatePath), Is.True);
                var duplicate = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(duplicatePath);
                Assert.That(duplicate.CompiledAsset, Is.SameAs(originalCompiled));
                Assert.That(BtGraphAssets.OwnsCompiledAsset(duplicate), Is.False);
                Assert.That(BtGraphAssets.TryDelete(duplicate, out var error), Is.True, error);
                Assert.That(AssetDatabase.LoadAssetAtPath<BehaviourTreeAsset>(originalPath), Is.SameAs(originalCompiled));

                Assert.That(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), duplicatePath), Is.True);
                duplicate = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(duplicatePath);
                var ownCompiled = BtGraphAssets.EnsureCompiledAsset(duplicate);
                Assert.That(ownCompiled, Is.Not.SameAs(originalCompiled));
                Assert.That(ownCompiled.GUID, Is.EqualTo(AssetDatabase.AssetPathToGUID(duplicatePath)));
                Assert.That(BtGraphAssets.OwnsCompiledAsset(duplicate), Is.True);
                Assert.That(BtGraphAssets.EnsureCompiledAsset(duplicate), Is.SameAs(ownCompiled));
                string ownPath = AssetDatabase.GetAssetPath(ownCompiled);
                Assert.That(ownPath, Is.EqualTo(folder + "/Duplicate_Compiled.asset"));
                Assert.That(AssetDatabase.MoveAsset(duplicatePath, folder + "/Moved.asset"), Is.Empty);
                Assert.That(BtGraphAssets.OwnsCompiledAsset(duplicate), Is.True);
                Assert.That(BtGraphAssets.TryDelete(duplicate, out error), Is.True, error);
                Assert.That(AssetDatabase.LoadAssetAtPath<BehaviourTreeAsset>(ownPath), Is.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<BehaviourTreeAsset>(originalPath), Is.SameAs(originalCompiled));
                Assert.That(BtGraphAssets.OwnsCompiledAsset(source), Is.True);
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }
    }
}
