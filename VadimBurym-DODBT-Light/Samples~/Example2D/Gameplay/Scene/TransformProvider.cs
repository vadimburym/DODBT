using System;
using DODBT.Light.Example.Gameplay.Settings;
using DODBT.Light.Example.Services;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Scene
{
    public sealed class TransformProvider : MonoBehaviour
    {
        [SerializeField] private TransformInfo[] _transforms;

        
        public Transform GetTransform(TransformId name)
        {
            for (int i = 0; i < _transforms.Length; i++)
            {
                if (_transforms[i].Name == name)
                    return _transforms[i].Transform;
            }
            return null;
        }

        public bool TryGetTransform(TransformId name, out Transform transform)
        {
            for (int i = 0; i < _transforms.Length; i++)
            {
                if (_transforms[i].Name == name)
                {
                    transform = _transforms[i].Transform;
                    return true;
                }
            }
            transform = null;
            return false;
        }
        
        [Serializable]
        public struct TransformInfo
        {
            public TransformId Name;
            public Transform Transform;
        }
    }
}
