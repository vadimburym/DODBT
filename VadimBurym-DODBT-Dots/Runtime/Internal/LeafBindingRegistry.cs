// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Collections.Generic;

namespace VadimBurym.DodBehaviourTree.Dots
{
    [Serializable]
    internal struct LeafBinding
    {
        public int Id;
        public string TypeIdentity;
    }

    // Public for generated code in the consuming assembly, not for manual registration.
    // Used only before creating a shared blob, never by the runner.
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class LeafBindingRegistry
    {
        private static readonly Dictionary<string, Func<int, string>> Resolvers = new();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Register(string contextIdentity, Func<int, string> resolve)
        {
            if (string.IsNullOrEmpty(contextIdentity)) throw new ArgumentException(nameof(contextIdentity));
            Resolvers[contextIdentity] = resolve ?? throw new ArgumentNullException(nameof(resolve));
        }

        internal static string Identity(Type type) => type.Assembly.GetName().Name + ":" + type.FullName;

        internal static string Validate(string contextIdentity, LeafBinding[] bindings, LeafData[] leaves)
        {
            if (string.IsNullOrEmpty(contextIdentity) || bindings == null || leaves == null)
                return "Missing compiled leaf bindings.";
            if (!Resolvers.TryGetValue(contextIdentity, out var resolve))
                return "No generated leaf table is registered for " + contextIdentity + ".";

            var recorded = new HashSet<int>();
            foreach (var binding in bindings)
            {
                if (binding.Id < 0 || string.IsNullOrEmpty(binding.TypeIdentity) || !recorded.Add(binding.Id))
                    return "Invalid or duplicate compiled leaf binding: " + binding.Id + ".";
                string current = resolve(binding.Id);
                if (!string.Equals(current, binding.TypeIdentity, StringComparison.Ordinal))
                    return "Leaf ID " + binding.Id + " was compiled as " + binding.TypeIdentity +
                        " but now resolves to " + (current ?? "a missing leaf") + ".";
            }

            foreach (var leaf in leaves)
                if (!recorded.Contains(leaf.LeafId)) return "Missing compiled binding for leaf ID " + leaf.LeafId + ".";
            return null;
        }
    }
}
