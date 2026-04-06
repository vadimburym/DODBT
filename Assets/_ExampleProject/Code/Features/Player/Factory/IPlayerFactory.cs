using UnityEngine;

namespace _ExampleProject.Code.Features.Player.Factory
{
    public interface IPlayerFactory
    {
        int Create(Vector2 position);
    }
}