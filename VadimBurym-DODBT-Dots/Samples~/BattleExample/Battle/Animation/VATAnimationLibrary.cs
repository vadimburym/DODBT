using System;
using UnityEngine;

namespace DODBT.Example.Battle.Animation
{
    [CreateAssetMenu(fileName = "VATAnimationLibrary", menuName = "VAT DOTS/Animation Library")]
    public sealed class VATAnimationLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class MeshEntry
        {
            public string name;
            public Mesh sourceMesh;
            public int meshIndex;
            public int vertexCount;
        }

        [Serializable]
        public sealed class ClipEntry
        {
            public string name;
            public string clipName;
            public int clipIndex;
            public int meshIndex;
            public int rowStart;
            public int frameCount;
            public float length;
        }

        [Header("Textures")]
        public Texture2D positionTexture;
        public Texture2D normalTexture;
        public Texture2D metadataTexture;

        [Header("Encoding")]
        public Vector3 boundsMin;
        public Vector3 boundsExtent = Vector3.one;
        public int maxVertexCount;
        public int totalFrameRows;

        [Header("Lookup")] 
        public byte ClipsLength => (byte)(clips.Length / meshes.Length);
        public MeshEntry[] meshes = Array.Empty<MeshEntry>();
        public ClipEntry[] clips = Array.Empty<ClipEntry>();

        public Bounds GetRenderBounds()
        {
            for (int axis = 0; axis < 3; axis++)
                if (!float.IsFinite(boundsMin[axis]) || !float.IsFinite(boundsExtent[axis]) || boundsExtent[axis] <= 0)
                    throw new InvalidOperationException($"{name}: VAT encoding bounds must be finite with positive size.");
            // Every decoded vertex and every frame/clip interpolation lies in this box.
            // The small margin covers texture quantization and floating-point roundoff.
            return new Bounds(boundsMin + boundsExtent * .5f, boundsExtent + Vector3.one * .02f);
        }

        public int FindMeshIndex(Mesh mesh)
        {
            if (mesh == null || meshes == null)
                return -1;

            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] != null && meshes[i].sourceMesh == mesh)
                    return meshes[i].meshIndex;
            }

            return -1;
        }

        public ClipEntry GetClip(int clipIndex)
        {
            if (clips == null || clipIndex < 0 || clipIndex >= clips.Length)
                return null;

            return clips[clipIndex];
        }

        public string GetClipDisplayName(int clipIndex)
        {
            var clip = GetClip(clipIndex);
            return clip == null ? $"Clip {clipIndex}" : clip.name;
        }

        public bool ClipBelongsToMesh(int clipIndex, int meshIndex)
        {
            var clip = GetClip(clipIndex);
            return clip != null && clip.meshIndex == meshIndex;
        }

        public int FindFirstClipForMesh(int meshIndex)
        {
            if (clips == null)
                return -1;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].meshIndex == meshIndex)
                    return clips[i].clipIndex;
            }

            return -1;
        }

        public int[] GetClipIndicesForMesh(int meshIndex)
        {
            if (clips == null)
                return Array.Empty<int>();

            int count = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].meshIndex == meshIndex)
                    count++;
            }

            if (count == 0)
                return Array.Empty<int>();

            int[] result = new int[count];
            int write = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].meshIndex == meshIndex)
                    result[write++] = clips[i].clipIndex;
            }

            return result;
        }
    }
}
