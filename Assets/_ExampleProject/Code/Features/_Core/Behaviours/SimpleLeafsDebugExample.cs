using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features._Core.Behaviours
{
    public sealed class SimpleLeafsDebugExample : MonoBehaviour
    {
        [SerializeField] private BtMonoDebug _btDebug;
        [SerializeField] private TextMesh _text;

        public void LateUpdate()
        {
            var text = "";
            var debug = _btDebug.RunningLeafs;
            for (int i = 0; i < debug.Count; i++)
            {
                text += debug[i] + "\n";
            }
            _text.text = text;
        }
    }
}