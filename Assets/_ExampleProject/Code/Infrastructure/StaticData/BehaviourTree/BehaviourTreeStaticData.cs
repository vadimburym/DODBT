using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Infrastructure.StaticData.BehaviourTree
{
    [CreateAssetMenu(fileName = nameof(BehaviourTreeStaticData), menuName ="_Project/StaticData/New BehaviourTreeStaticData")]
    public sealed class BehaviourTreeStaticData : ScriptableObject
    {
        public BehaviourTreeAsset[] Assets;
    }
}