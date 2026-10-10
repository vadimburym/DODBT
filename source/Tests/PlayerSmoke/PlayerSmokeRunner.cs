using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree.PlayerSmoke
{
    public sealed class SmokeContext { public int Ticks; }
    public struct SmokeState { public int Entries; }
    public interface ISmokeValue { int Number { get; } }

    [Serializable]
    public sealed class SmokeValue : ISmokeValue
    {
        public int StoredNumber;
        public int Number => StoredNumber;
        [SerializeReference] public ISmokeValue Next;
    }

    [Serializable]
    public sealed class SmokeLeaf : ILeaf<SmokeContext, SmokeState>
    {
        public int Number;
        public Texture2D Texture;
        [SerializeReference] public ISmokeValue Value;
        [SerializeReference] public ISmokeValue Alias;
        [SerializeReference] public List<ISmokeValue> Values = new();
        [NonSerialized] public object RuntimeOnly;
        public NodeStatus OnTick(SmokeContext context, ref SmokeState state) { context.Ticks++; return NodeStatus.Success; }
        public void OnEnter(SmokeContext context, ref SmokeState state) => state.Entries++;
        public void OnExit(SmokeContext context, ref SmokeState state, NodeStatus exitStatus) { }
        public void OnAbort(SmokeContext context, ref SmokeState state) { }
    }

    // Included only in explicitly requested verification players. Ordinary builds have no smoke assembly.
    public sealed class PlayerSmokeRunner : MonoBehaviour
    {
#if DODBT_PLAYER_SMOKE
        private bool _failed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var host = new GameObject("DODBT Player Verification");
            DontDestroyOnLoad(host);
            host.AddComponent<PlayerSmokeRunner>();
        }

        private void Awake() => Application.logMessageReceived += OnLog;
        private void OnDestroy() => Application.logMessageReceived -= OnLog;
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _failed = true;
        }

        private IEnumerator Start()
        {
            try { VerifyTree(); }
            catch (Exception error) { Debug.LogException(error); _failed = true; }
            // Also let the unchanged example bootstrap and game systems execute.
            yield return new WaitForSecondsRealtime(10);
            Debug.Log(_failed ? "DODBT_SMOKE_FAILED" : "DODBT_SMOKE_PASSED");
            Application.Quit(_failed ? 1 : 0);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void VerifyTree()
        {
            var asset = Resources.Load<BehaviourTreeAsset>("DODBT_SmokeTree");
            Require(asset != null, "Compiled tree was not loaded from the player data.");
            var tree = new BehaviourTree<SmokeContext, SmokeState>();
            tree.Construct(asset);
            Require(tree.Leafs.Length == 1, "Unexpected leaf count.");
            var leaf = tree.Leafs[0] as SmokeLeaf;
            Require(leaf != null && leaf.Number == 42 && leaf.Value.Number == 17, "Serialized leaf parameters were lost.");
            Require(ReferenceEquals(leaf.Value, leaf.Alias) && ReferenceEquals(leaf.Value, leaf.Values[0]), "Managed aliases were lost.");
            Require(ReferenceEquals(((SmokeValue)leaf.Value).Next, leaf.Value), "Managed cycle was lost.");
            Require(leaf.Texture != null && leaf.Texture.width == 2, "Unity asset reference was lost.");
            Require(leaf.RuntimeOnly == null, "Nonserialized runtime state leaked into the build.");
            var context = new SmokeContext();
            var state = new BtState<SmokeState>();
            tree.FillInitialState(state);
            for (int i = 0; i < 100; i++) tree.Tick(context, state);
            Require(context.Ticks == 100 && state.LeafStates[0].Entries == 100, "Compiled tree did not execute correctly.");
            tree.Abort(context, state);
            Debug.Log("DODBT_SERIALIZATION_AND_RUNTIME_PASSED");
        }
#endif
    }
}
