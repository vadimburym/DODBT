using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Transforms;

namespace DODBT.Example.Battle
{
    public static class EntityUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DetachEntityChild(
            Entity parent,
            Entity child,
            ComponentLookup<Parent> parentLookup,
            BufferLookup<LinkedEntityGroup> linkedLookup,
            ComponentLookup<LocalTransform> localTransformLookup,
            EntityCommandBuffer ecb)
        {
            var localTransform = localTransformLookup[parent];
            ecb.SetComponent(child, localTransform);
            
            if (linkedLookup.HasBuffer(parent))
            {
                var oldBuffer = linkedLookup[parent];
                var newBuffer = ecb.SetBuffer<LinkedEntityGroup>(parent);
                for (int i = 0; i < oldBuffer.Length; i++)
                {
                    var linked = oldBuffer[i].Value;
                    if (linked == child)
                        continue;
                    newBuffer.Add(oldBuffer[i]);
                }
            }
            if (parentLookup.HasComponent(child))
                ecb.RemoveComponent<Parent>(child);
        }
    }
}
