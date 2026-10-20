using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DODBT.Example.Battle.Grid
{
    public struct BattlefieldGridUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CellDistanceChebyshev(int2 a, int2 b)
        {
            var d = math.abs(a - b);
            return math.max(d.x, d.y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 WorldToCell(ref BattlefieldGridBlob grid, float3 world)
        {
            var local = new float2(world.x - grid.Origin.x, world.z - grid.Origin.z);
            return (int2)math.floor(local / grid.CellSize);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 CellToWorldCenter(ref BattlefieldGridBlob grid, int2 cell, float y = 0f)
        {
            return new float3(
                grid.Origin.x + (cell.x + 0.5f) * grid.CellSize,
                y,
                grid.Origin.z + (cell.y + 0.5f) * grid.CellSize
            );
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 FootprintToWorldCenter(
            ref BattlefieldGridBlob grid,
            int2 originCell,
            int footprintX,
            int footprintY,
            float y = 0f)
        {
            float centerX = originCell.x + footprintX * 0.5f;
            float centerY = originCell.y + footprintY * 0.5f;
            return new float3(
                grid.Origin.x + centerX * grid.CellSize,
                y,
                grid.Origin.z + centerY * grid.CellSize
            );
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool InBounds(ref BattlefieldGridBlob grid, int2 cell)
        {
            return cell is { x: >= 0, y: >= 0 } && cell.x < grid.Width && cell.y < grid.Height;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Flatten(ref BattlefieldGridBlob grid, int2 cell)
        {
            return cell.y * grid.Width + cell.x;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWalkable(ref BattlefieldGridBlob grid, int2 cell)
        {
            return InBounds(ref grid, cell) && grid.Walkable[Flatten(ref grid, cell)] != 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAreaFree(
            ref BattlefieldGridBlob grid,
            NativeParallelHashMap<int2, Entity> occupiedMap,
            int2 originCell,
            int footprintX,
            int footprintY,
            Entity self)
        {
            for (int y = 0; y < footprintY; y++)
                for (int x = 0; x < footprintX; x++)
                {
                    var cell = originCell + new int2(x, y);
                    if (!IsWalkable(ref grid, cell))
                        return false;
                    if (occupiedMap.TryGetValue(cell, out var occupiedBy) && occupiedBy != self)
                        return false;
                }
            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OccupyAreaDirect(
            NativeParallelHashMap<int2, Entity> occupiedMap,
            Entity entity,
            int2 originCell,
            int footprintX,
            int footprintY)
        {
            for (int y = 0; y < footprintY; y++)
                for (int x = 0; x < footprintX; x++)
                    occupiedMap.TryAdd(originCell + new int2(x, y), entity);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseAreaDirect(
            NativeParallelHashMap<int2, Entity> occupiedMap,
            Entity entity,
            int2 originCell,
            int footprintX,
            int footprintY)
        {
            for (int y = 0; y < footprintY; y++)
                for (int x = 0; x < footprintX; x++)
                {
                    var cell = originCell + new int2(x, y);
                    if (occupiedMap.TryGetValue(cell, out var current) && current == entity)
                        occupiedMap.Remove(cell);
                }
        }
    }
}
