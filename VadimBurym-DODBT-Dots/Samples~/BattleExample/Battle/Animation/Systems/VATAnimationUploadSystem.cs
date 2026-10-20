using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DODBT.Example.Battle.Animation
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class VATAnimationUploadSystem : SystemBase
    {
        private EntityQuery _query;
        private GraphicsBuffer _gpuBuffer;
        private int _gpuCapacity;
        private Material[] _materials = System.Array.Empty<Material>();
        private static readonly int BufferId = Shader.PropertyToID("_DODBTExampleVATAnimStateBuffer");
        
        protected override void OnCreate()
        {
            _query = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<VATAnimator>(),
                    ComponentType.ReadWrite<VATRendererUserValue>(),
                }
            });

            RequireForUpdate(_query);
            EnsureBufferCapacity(1);
        }

        internal void SetMaterials(Material[] materials)
        {
            _materials = materials;
            BindBuffer();
        }

        private void BindBuffer()
        {
            foreach (var material in _materials)
            {
                material.SetBuffer(BufferId, _gpuBuffer);
                material.EnableKeyword("DODBT_SAMPLE_ANIMATION");
            }
        }

        protected override void OnDestroy()
        {
            _gpuBuffer?.Dispose();
            _gpuBuffer = null;
            _gpuCapacity = 0;
            _materials = System.Array.Empty<Material>();
        }

        protected override void OnUpdate()
        {
            int entityCount = _query.CalculateEntityCount();
            if (entityCount <= 0) return;

            using var states = new NativeArray<VATAnimStateGPU>(
                entityCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var job = new BuildAnimStateBufferJob {
                Output = states,
            };

            Dependency = job.ScheduleParallel(_query, Dependency);
            Dependency.Complete();

            EnsureBufferCapacity(entityCount);
            _gpuBuffer.SetData(states);
        }

        private void EnsureBufferCapacity(int requiredCount)
        {
            if (_gpuBuffer != null && _gpuCapacity >= requiredCount)
                return;

            int newCapacity = math.max(1, math.ceilpow2(requiredCount));
            _gpuBuffer?.Dispose();
            _gpuBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, newCapacity, Marshal.SizeOf<VATAnimStateGPU>());
            _gpuCapacity = newCapacity;
            BindBuffer();
        }

        [BurstCompile]
        private partial struct BuildAnimStateBufferJob : IJobEntity
        {
            public NativeArray<VATAnimStateGPU> Output;

            private void Execute(
                [EntityIndexInQuery] int entityIndexInQuery,
                ref VATRendererUserValue rendererUserValue,
                in VATAnimator animator)
            {
                rendererUserValue.Value = (uint)entityIndexInQuery;
                Output[entityIndexInQuery] = new VATAnimStateGPU
                {
                    AnimIndex = animator.CurrentClipIndex,
                    AnimTime = animator.CurrentNormalizedTime,
                    PrevAnimIndex = animator.PreviousClipIndex,
                    PrevAnimTime = animator.PreviousNormalizedTime,
                    Blend = animator.Blend01,
                    Pad0 = 0f,
                    Pad1 = 0f,
                    Pad2 = 0f,
                };
            }
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct VATAnimStateGPU
    {
        public float AnimIndex;
        public float AnimTime;
        public float PrevAnimIndex;
        public float PrevAnimTime;
        public float Blend;
        public float Pad0;
        public float Pad1;
        public float Pad2;
    }
}
