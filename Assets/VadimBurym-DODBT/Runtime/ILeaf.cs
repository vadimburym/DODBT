// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

namespace VadimBurym.DodBehaviourTree
{
    public interface ILeaf {}
    public interface ILeaf<in TContext, TLeafState> : ILeaf
        where TContext : class where TLeafState : struct
    {
        NodeStatus OnTick(TContext context, ref TLeafState state);
        void OnEnter(TContext context, ref TLeafState state);
        void OnExit(TContext context, ref TLeafState state, NodeStatus exitStatus);
        void OnAbort(TContext context, ref TLeafState state);
    }
}