using UnityEngine;
using VadimBurym.DodBehaviourTree.Dots;
using DODBT.Example.Battle.Animation;

namespace DODBT.Example
{
    [CreateAssetMenu(menuName = "DODBT Example/Unit Definition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        public Mesh Mesh;
        public Material Material;
        public VATAnimationLibrary AnimationLibrary;
        public BehaviourTreeAsset Tree;
        [Min(0.01f)] public float Speed = 4;
        [Min(0.01f)] public float RotationSpeed = 5;
        [Min(1)] public ushort Health = 200;
        [Min(0.01f)] public float AttackInterval = 1.5f;
        [Min(1)] public byte AttackRangeCells = 2;
        [Min(1)] public ushort Damage = 10;
        [Min(0.01f)] public float DetectRadius = 10;
        [Min(0.01f)] public float ChaseRadius = 15;
        [Min(0.01f)] public float UpdateNearestInterval = 3;
        [Min(0.01f)] public float ScanInterval = .3f;
        [Min(1)] public byte FootprintX = 1;
        [Min(1)] public byte FootprintY = 1;

        public void Validate()
        {
            if (Mesh == null || Material == null || AnimationLibrary == null || Tree == null)
                throw new System.InvalidOperationException($"{name}: assign mesh, material, animation library and tree.");
            if (AnimationLibrary.FindMeshIndex(Mesh) < 0)
                throw new System.InvalidOperationException($"{name}: mesh is not present in its animation library.");
            AnimationLibrary.GetRenderBounds();
            if (Health == 0 || Damage == 0 || FootprintX == 0 || FootprintY == 0 ||
                AttackInterval <= 0 || ScanInterval <= 0 || UpdateNearestInterval <= 0)
                throw new System.InvalidOperationException($"{name}: health, damage, footprint and intervals must be positive.");
        }
    }
}
