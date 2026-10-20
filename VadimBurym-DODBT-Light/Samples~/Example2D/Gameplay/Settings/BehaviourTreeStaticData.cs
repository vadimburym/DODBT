using UnityEngine;
using VadimBurym.DodBehaviourTree.Light;

namespace DODBT.Light.Example.Gameplay.Settings
{
    [CreateAssetMenu(fileName = nameof(BehaviourTreeStaticData), menuName ="DODBT/Light Example/StaticData/New BehaviourTreeStaticData")]
    public sealed class BehaviourTreeStaticData : ScriptableObject
    {
        public BehaviourTreeAsset[] Assets;
    }
}
