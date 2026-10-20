// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;

namespace VadimBurym.DodBehaviourTree.Dots
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class LeafCodeGenAttribute : Attribute
    {
        internal readonly int Id;
        internal Type ContextType { get; }

        public LeafCodeGenAttribute(int id)
        {
            Id = id;
        }

        public LeafCodeGenAttribute(int id, Type contextType)
        {
            Id = id;
            ContextType = contextType;
        }
    }
}
