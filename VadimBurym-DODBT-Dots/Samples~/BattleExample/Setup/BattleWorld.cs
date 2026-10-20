using System;
using System.Collections.Generic;
using DODBT.Example.Battle.Animation;
using DODBT.Example.Setup;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace DODBT.Example
{
    internal static class BattleWorld
    {
        internal static World Create()
        {
            var world = new World("DODBT Battle", WorldFlags.Game);
            try
            {
                // Use Unity's default filtering and ordering, but never import another game's systems.
                var systems = new List<Type>();
                foreach (var type in DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default))
                {
                    var assembly = type.Assembly;
                    if (assembly == typeof(World).Assembly ||
                        assembly == typeof(LocalToWorldSystem).Assembly ||
                        assembly == typeof(EntitiesGraphicsSystem).Assembly ||
                        type == typeof(UpdateWorldTimeSystem))
                        systems.Add(type);
                }
                systems.Add(typeof(BattleSystemGroup));
                systems.Add(typeof(VATAnimationSystem));
                systems.Add(typeof(VATAnimationUploadSystem));
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                return world;
            }
            catch
            {
                world.Dispose();
                throw;
            }
        }
    }
}
