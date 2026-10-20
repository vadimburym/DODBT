using UnityEngine;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.BehaviourTree
{
    public sealed class SimpleLeafsDebugExample : MonoBehaviour
    {
#if UNITY_EDITOR
        private static readonly Unity.Profiling.ProfilerMarker TextMarker = new("DODBT.Light.ExampleLabels");
        [SerializeField] private BtMonoDebug _btDebug;
        [SerializeField] private TextMesh _text;
        private readonly System.Collections.Generic.List<string> _lastLeaves = new();
        private readonly System.Text.StringBuilder _builder = new();
        private bool _hasText;

        public void LateUpdate()
        {
            using var profile = TextMarker.Auto();
            if (_btDebug == null || _text == null) return;
            var debug = _btDebug.RunningLeafs;
            if (debug == null) return;
            bool changed = !_hasText || debug.Count != _lastLeaves.Count;
            if (!changed)
                for (int i = 0; i < debug.Count; i++)
                    if (debug[i] != _lastLeaves[i]) { changed = true; break; }
            if (!changed) return;
            _lastLeaves.Clear();
            _builder.Clear();
            for (int i = 0; i < debug.Count; i++)
            {
                _lastLeaves.Add(debug[i]);
                _builder.Append(debug[i]).Append('\n');
            }
            _text.text = _builder.ToString();
            _hasText = true;
        }
#endif
    }
}
