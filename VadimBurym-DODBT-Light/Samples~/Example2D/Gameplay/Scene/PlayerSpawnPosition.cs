using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Scene
{
    public sealed class PlayerSpawnPosition : MonoBehaviour
    {
        public Vector2 SpawnPosition => transform.position;
    }
}
