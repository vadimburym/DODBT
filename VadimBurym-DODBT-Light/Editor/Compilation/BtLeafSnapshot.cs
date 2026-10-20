// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    using System;
    using UnityEngine;
    using VadimBurym.DodBehaviourTree.Light;

    // A native serialization host preserves Unity object references and nested managed references.
    internal sealed class BtLeafSnapshot : ScriptableObject
    {
        [SerializeReference] public ILeaf Leaf;

        internal static ILeaf Clone(ILeaf source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var host = CreateInstance<BtLeafSnapshot>();
            BtLeafSnapshot copy = null;
            try
            {
                host.Leaf = source;
                copy = Instantiate(host);
                if (copy.Leaf == null || ReferenceEquals(copy.Leaf, source))
                    throw new InvalidOperationException("Unity could not clone leaf " + source.GetType().FullName);
                return copy.Leaf;
            }
            finally
            {
                if (copy != null) DestroyImmediate(copy);
                DestroyImmediate(host);
            }
        }
    }
}
