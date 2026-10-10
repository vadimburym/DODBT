using System;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

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
