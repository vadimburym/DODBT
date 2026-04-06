using System;
using UnityEngine;

namespace _ExampleProject.Code.Infrastructure.StaticData.Enemy
{
    [Serializable]
    public struct SpawnPosition
    {
        public Vector2 Position;
        public float DifficultyWeight;
    }
}