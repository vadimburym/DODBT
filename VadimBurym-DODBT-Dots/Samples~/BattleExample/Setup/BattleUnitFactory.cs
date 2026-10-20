using DODBT.Example.Battle.Grid;
using DODBT.Example.BehaviourTree;
using DODBT.Example.Battle.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;
using UnityEngine;
using VadimBurym.DodBehaviourTree.Dots;
using DODBT.Example.Battle.Animation;
using Random = Unity.Mathematics.Random;

namespace DODBT.Example
{
    internal sealed class BattleUnitFactory
    {
        private readonly EntityManager _manager;
        private Random _random;

        internal BattleUnitFactory(EntityManager manager, uint seed)
        {
            _manager = manager;
            _random = new Random(seed == 0 ? 1 : seed);
        }

        internal void SpawnArmy(UnitDefinition unit, Material material, BlobAssetReference<VATAnimationLibraryBlob> animation,
            byte team, int count, int columns, float spacing, float3 origin)
        {
            var prototype = CreatePrototype(unit, material, animation);
            try
            {
                using var commands = new EntityCommandBuffer(Allocator.Temp);
                for (int i = 0; i < count; i++)
                {
                    var position = origin + new float3(i % columns * spacing, 0, i / columns * spacing);
                    var entity = commands.Instantiate(prototype);
                    commands.SetComponent(entity, LocalTransform.FromPosition(position));
                    commands.SetComponent(entity, new TargetPosition { Position = position });
                    commands.SetComponent(entity, new Team { Value = team });
                    commands.SetComponent(entity, new AiBrain
                    {
                        BlobId = team,
                        UpdateTime = _random.NextFloat(0, .2f),
                        RandomState = _random.NextUInt(1, uint.MaxValue)
                    });
                    commands.SetComponent(entity, new EyeSensor { ScanTimer = _random.NextFloat(0, unit.ScanInterval) });
                    unit.Tree.FillAgentStateBuffers(entity, commands);
                    var placement = commands.CreateEntity();
                    commands.AddComponent(placement, new MoveCommandRequest { Destination = position });
                    commands.AddBuffer<MoveCommandTarget>(placement).Add(new MoveCommandTarget { Value = entity });
#if UNITY_EDITOR
                    commands.SetName(entity, new FixedString64Bytes($"{unit.name} {i + 1}"));
#endif
                }
                commands.Playback(_manager);
            }
            finally
            {
                if (_manager.Exists(prototype)) _manager.DestroyEntity(prototype);
            }
        }

        private Entity CreatePrototype(UnitDefinition unit, Material material, BlobAssetReference<VATAnimationLibraryBlob> animation)
        {
            var root = _manager.CreateEntity(typeof(Prefab), typeof(LocalTransform), typeof(LocalToWorld));
            var renderer = _manager.CreateEntity(typeof(Prefab), typeof(LocalTransform), typeof(LocalToWorld), typeof(Parent));
            var linked = _manager.AddBuffer<LinkedEntityGroup>(root);
            linked.Add(new LinkedEntityGroup { Value = root });
            linked.Add(new LinkedEntityGroup { Value = renderer });
            _manager.SetComponentData(root, LocalTransform.Identity);
            _manager.SetComponentData(renderer, LocalTransform.Identity);
            _manager.SetComponentData(renderer, new Parent { Value = root });
            _manager.AddComponentData(root, new RendererEntityRef { Value = renderer });

            var renderArray = new RenderMeshArray(new[] { material }, new[] { unit.Mesh });
            var renderDescription = new RenderMeshDescription(ShadowCastingMode.On, true);
            RenderMeshUtility.AddComponents(renderer, _manager, renderDescription, renderArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
            var bounds = unit.AnimationLibrary.GetRenderBounds();
            _manager.SetComponentData(renderer, new RenderBounds
            {
                Value = new AABB { Center = bounds.center, Extents = bounds.extents }
            });
            int meshIndex = unit.AnimationLibrary.FindMeshIndex(unit.Mesh);
            _manager.AddComponentData(renderer, new VATLibraryBlobRef { Value = animation });
            _manager.AddComponentData(renderer, new VATAnimator
            {
                MeshIndex = meshIndex,
                CurrentClipIndex = unit.AnimationLibrary.FindFirstClipForMesh(meshIndex),
                PreviousClipIndex = -1,
                Blend01 = 1,
                DefaultTransitionDuration = .15f,
                Speed = 1,
                Loop = 1,
                Playing = 1
            });
            _manager.AddComponentData(renderer, new VATAnimationCommand
            {
                RequestedAnimationIndex = -1, TransitionDuration = -1
            });
            _manager.AddComponent<VATRendererUserValue>(renderer);
            _manager.AddComponent<CorpseTag>(renderer);
            _manager.SetComponentEnabled<CorpseTag>(renderer, false);

            _manager.AddComponentData(root, new MovementStats { Speed = unit.Speed, RotationSpeed = unit.RotationSpeed });
            _manager.AddComponent<TargetPosition>(root);
            _manager.AddComponentData(root, new Footprint { FootprintX = unit.FootprintX, FootprintY = unit.FootprintY });
            _manager.AddComponent<GridNavigationState>(root);
            _manager.AddComponent<Team>(root);
            _manager.AddComponent<IsMovingTag>(root);
            _manager.AddComponent<AiBrain>(root);
            _manager.AddComponent<EyeSensor>(root);
            _manager.AddComponent<CleanupTag>(root);
            _manager.SetComponentEnabled<CleanupTag>(root, false);
            _manager.AddComponent<SeeToDetectedTag>(root);
            _manager.SetComponentEnabled<SeeToDetectedTag>(root, false);
            _manager.AddComponentData(root, new Health { Max = unit.Health, Current = unit.Health });
            _manager.AddComponentData(root, new AttackStats
            {
                AttackInterval = unit.AttackInterval, AttackRangeCells = unit.AttackRangeCells, Damage = unit.Damage
            });
            _manager.AddComponentData(root, new EyeSensorStats
            {
                DetectRadius = unit.DetectRadius, ChaseRadius = unit.ChaseRadius,
                UpdateNearestInterval = unit.UpdateNearestInterval, ScanInterval = unit.ScanInterval
            });
            _manager.AddBuffer<NodeStateElement>(root);
            _manager.AddBuffer<LeafStateElement>(root);
            return root;
        }
    }
}
