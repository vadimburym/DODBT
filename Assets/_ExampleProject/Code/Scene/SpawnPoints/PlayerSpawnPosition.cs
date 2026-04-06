using UnityEngine;

namespace _ExampleProject.Code.Scene.SpawnPoints
{
    public sealed class PlayerSpawnPosition : MonoBehaviour
    {
        public Vector2 SpawnPosition => transform.position;
    }
}