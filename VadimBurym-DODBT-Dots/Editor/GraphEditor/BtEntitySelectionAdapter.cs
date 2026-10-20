// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using System;
    using System.Reflection;
    using Unity.Entities;
    using UnityEngine;

    // Entities 1.4 exposes selection through an internal editor proxy. Keep that dependency
    // here; an incompatible package only disables the shortcut, not the entity list debugger.
    internal static class BtEntitySelectionAdapter
    {
        private static readonly Type ProxyType = Type.GetType("Unity.Entities.Editor.EntitySelectionProxy, Unity.Entities.Editor");
        private static readonly PropertyInfo WorldProperty = ProxyType?.GetProperty("World");
        private static readonly PropertyInfo EntityProperty = ProxyType?.GetProperty("Entity");
        internal static bool IsSupported => WorldProperty?.PropertyType == typeof(World) && EntityProperty?.PropertyType == typeof(Entity);

        internal static bool TryRead(UnityEngine.Object selected, out World world, out Entity entity)
        {
            world = null;
            entity = Entity.Null;
            if (!IsSupported || selected == null || !ProxyType.IsInstanceOfType(selected)) return false;
            try
            {
                world = (World)WorldProperty.GetValue(selected);
                entity = (Entity)EntityProperty.GetValue(selected);
                return world != null && world.IsCreated && entity.Index >= 0 && world.EntityManager.Exists(entity);
            }
            catch (TargetInvocationException) { }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            world = null;
            entity = Entity.Null;
            return false;
        }
    }
}
