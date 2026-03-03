using System;
using _Project.Code.Core.Keys;
using _Project.Infrastructure;
using UnityEngine;

namespace _Project.Code.Infrastructure.Scene
{
    public sealed class TransformProvider : MonoBehaviour
    {
        [SerializeField] private TransformInfo[] _transforms;

        private void Awake()
        {
            ServiceLocator.Bind<TransformProvider>(this);
        }
        
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