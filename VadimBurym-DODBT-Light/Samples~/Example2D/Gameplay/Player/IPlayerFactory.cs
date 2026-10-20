using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Player
{
    public interface IPlayerFactory
    {
        int Create(Vector2 position);
    }
}
