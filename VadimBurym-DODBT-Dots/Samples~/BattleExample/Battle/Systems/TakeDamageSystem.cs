using DODBT.Example;
using DODBT.Example.Battle.Components;
using DODBT.Example.Battle;
using DODBT.Example.Setup;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using DODBT.Example.Battle.Animation;

namespace DODBT.Example.Battle.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(BattleSystemGroup), OrderFirst = true)]
    public partial struct TakeDamageSystem : ISystem
    {
        private ComponentLookup<Health> _healthLookup;
        private ComponentLookup<RendererEntityRef> _rendererLookup;

        private BufferLookup<LinkedEntityGroup> _linkedLookup;
        private ComponentLookup<Parent> _parentLookup;
        private ComponentLookup<LocalTransform> _localLookup;

        public void OnCreate(ref SystemState state)
        {
            _healthLookup = SystemAPI.GetComponentLookup<Health>();
            _rendererLookup = SystemAPI.GetComponentLookup<RendererEntityRef>();
            _linkedLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>();
            _parentLookup =  SystemAPI.GetComponentLookup<Parent>();
            _localLookup = SystemAPI.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _healthLookup.Update(ref state);
            _rendererLookup.Update(ref state);
            _linkedLookup.Update(ref state);
            _parentLookup.Update(ref state);
            _localLookup.Update(ref state);

            using var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (requestData, request) in
                     SystemAPI.Query<RefRO<TakeDamageRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(request);
                var entity = requestData.ValueRO.Source;

                if (!_healthLookup.HasComponent(entity))
                    continue;

                var damage = requestData.ValueRO.Damage;
                var health = _healthLookup[entity];
                if (health.Current <= 0)
                    continue;
                health.Current = damage >= health.Current ? (ushort)0 : (ushort)(health.Current - damage);
                _healthLookup[entity] = health;
                if (health.Current <= 0)
                {
                    if (_rendererLookup.HasComponent(entity))
                    {
                        var renderer = _rendererLookup[entity].Value;
                        AnimatorUtils.PlayAnimation(
                            renderer: renderer,
                            animationId: AnimationId.Dead,
                            ecb: ecb,
                            oneShot: 1);

                        EntityUtils.DetachEntityChild(
                            entity,
                            renderer,
                            _parentLookup,
                            _linkedLookup,
                            _localLookup,
                            ecb);

                        ecb.SetComponent(renderer, new CorpseTag { Time = 0f });
                        ecb.SetComponentEnabled<CorpseTag>(renderer, true);
                    }
                    ecb.SetComponentEnabled<CleanupTag>(entity, true);
                }
            }
            ecb.Playback(state.EntityManager);
        }
    }
}
