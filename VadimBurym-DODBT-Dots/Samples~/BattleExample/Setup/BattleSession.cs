using System;
using System.Collections.Generic;
using DODBT.Example.Battle.Grid;
using DODBT.Example.BehaviourTree;
using DODBT.Example.BehaviourTree.Systems;
using DODBT.Example.Battle.Systems;
using DODBT.Example.Battle.Sensing;
using DODBT.Example.Setup;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VadimBurym.DodBehaviourTree.Dots;
using DODBT.Example.Battle.Animation;

namespace DODBT.Example
{
    // The scene owns its World; stop all jobs before releasing the shared blobs.
    public sealed class BattleSession : IDisposable
    {
        public World World { get; private set; }
        // AiBrain is not enableable. Counting its chunks needs no component reads or job completion.
        public int AgentCount => !_disposed && World != null && World.IsCreated
            ? _agents.CalculateEntityCountWithoutFiltering() : 0;
        private EntityQuery _agents;
        private NativeArray<BlobAssetReference<BehaviourTreeBlob>> _trees;
        private BlobAssetReference<BattlefieldGridBlob> _grid;
        private readonly List<BlobAssetReference<VATAnimationLibraryBlob>> _animations = new();
        private readonly Dictionary<Material, Material> _materials = new();
        private bool _disposed;

        public BattleSession(BattleExample settings)
        {
            try
            {
                World = BattleWorld.Create();
                var manager = World.EntityManager;
                _trees = new NativeArray<BlobAssetReference<BehaviourTreeBlob>>(2, Allocator.Persistent);
                _trees[0] = settings.Footman.Tree.CreateBlob();
                _trees[1] = settings.Orc.Tree == settings.Footman.Tree
                    ? _trees[0] : settings.Orc.Tree.CreateBlob();
                manager.AddComponentData(manager.CreateEntity(), new BehaviourTreeSingleton { Blobs = _trees });
                _grid = CreateGrid(settings.Grid);
                manager.AddComponentData(manager.CreateEntity(), new BattlefieldGridSingleton { Value = _grid });

                var footmanAnimation = CreateAnimations(settings.Footman.AnimationLibrary);
                _animations.Add(footmanAnimation);
                var orcAnimation = footmanAnimation;
                if (settings.Orc.AnimationLibrary != settings.Footman.AnimationLibrary)
                {
                    orcAnimation = CreateAnimations(settings.Orc.AnimationLibrary);
                    _animations.Add(orcAnimation);
                }
                RegisterBattleSystems();
                var footmanMaterial = GetMaterial(settings.Footman.Material);
                var orcMaterial = GetMaterial(settings.Orc.Material);
                World.GetExistingSystemManaged<VATAnimationUploadSystem>().SetMaterials(
                    new List<Material>(_materials.Values).ToArray());
                var factory = new BattleUnitFactory(manager, settings.Seed);
                factory.SpawnArmy(settings.Footman, footmanMaterial, footmanAnimation, 0, settings.UnitsPerArmy,
                    settings.Columns, settings.Spacing, settings.FirstArmyOrigin);
                factory.SpawnArmy(settings.Orc, orcMaterial, orcAnimation, 1, settings.UnitsPerArmy,
                    settings.Columns, settings.Spacing, settings.SecondArmyOrigin);
                _agents = manager.CreateEntityQuery(ComponentType.ReadOnly<AiBrain>());
                ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(World);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void RegisterBattleSystems()
        {
            var group = World.GetOrCreateSystemManaged<BattleSystemGroup>();
            Add<MovementSystem>(group);
            Add<MovementAnimationSystem>(group);
            Add<MoveCommandSystem>(group);
            Add<TakeDamageSystem>(group);
            Add<EntityCleanupSystem>(group);
            Add<CorpseSystem>(group);
            Add<GridMovingCellSyncSystem>(group);
            Add<GridOccupiedCleanupSystem>(group);
            Add<EyeSensorGridSystem>(group);
            Add<EyeSensorSystem>(group);
            Add<SeeToDetectedSystem>(group);
            Add<LeafStateWriteSystem>(group);
            Add<BehaviourTreeTickSystem>(group);
            Add<AttackStateSystem>(group);
            Add<ChaseStateSystem>(group);
            group.SortSystems();
        }

        private void Add<T>(ComponentSystemGroup group) where T : unmanaged, ISystem =>
            group.AddSystemToUpdateList(World.GetOrCreateSystem<T>());

        private Material GetMaterial(Material source)
        {
            if (_materials.TryGetValue(source, out var material)) return material;
            material = new Material(source) { name = source.name + " (DODBT Battle)", hideFlags = HideFlags.DontSave };
            _materials.Add(source, material);
            return material;
        }

        private static BlobAssetReference<BattlefieldGridBlob> CreateGrid(BattlefieldGrid source)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var grid = ref builder.ConstructRoot<BattlefieldGridBlob>();
            grid.Origin = source.Origin;
            grid.CellSize = source.CellSize;
            grid.Width = source.Width;
            grid.Height = source.Height;
            var walkable = builder.Allocate(ref grid.Walkable, checked(source.Width * source.Height));
            for (int i = 0; i < walkable.Length; i++)
                walkable[i] = source.FillWalkableByDefault ? (byte)1 : (byte)0;
            foreach (var rect in source.BlockedRects)
                for (int y = math.max(0, rect.Y); y < math.min(grid.Height, rect.Y + rect.Height); y++)
                    for (int x = math.max(0, rect.X); x < math.min(grid.Width, rect.X + rect.Width); x++)
                        walkable[y * grid.Width + x] = 0;
            return builder.CreateBlobAssetReference<BattlefieldGridBlob>(Allocator.Persistent);
        }

        private static BlobAssetReference<VATAnimationLibraryBlob> CreateAnimations(VATAnimationLibrary source)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var library = ref builder.ConstructRoot<VATAnimationLibraryBlob>();
            library.ClipsLength = source.ClipsLength;
            var clips = builder.Allocate(ref library.Clips, source.clips.Length);
            for (int i = 0; i < clips.Length; i++)
                clips[i] = new VATClipRuntimeData
                {
                    MeshIndex = source.clips[i].meshIndex,
                    Length = math.max(source.clips[i].length, 1e-5f)
                };
            return builder.CreateBlobAssetReference<VATAnimationLibraryBlob>(Allocator.Persistent);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (World != null && World.IsCreated)
            {
                World.EntityManager.CompleteAllTrackedJobs();
                ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop(World);
                World.Dispose();
            }
            if (_trees.IsCreated)
            {
                if (_trees[1].IsCreated && _trees[1] != _trees[0]) _trees[1].Dispose();
                if (_trees[0].IsCreated) _trees[0].Dispose();
                _trees.Dispose();
            }
            if (_grid.IsCreated) _grid.Dispose();
            foreach (var animation in _animations)
                if (animation.IsCreated) animation.Dispose();
            _animations.Clear();
            foreach (var material in _materials.Values)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
            }
            _materials.Clear();
        }
    }
}
