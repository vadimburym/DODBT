using DODBT.Example.Battle.Grid;
using DODBT.Example.Battle.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DODBT.Example.Battle.Sensing
{
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateAfter(typeof(EyeSensorGridSystem))]
    public partial struct EyeSensorSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<Team> _teamLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EyeSensorGridSingleton>();

            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _teamLookup = state.GetComponentLookup<Team>(true);
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            _teamLookup.Update(ref state);

            var map = SystemAPI.GetSingleton<EyeSensorGridSingleton>();

            state.Dependency = new SensorJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                CellSize = map.CellSize,
                Command0Grid = map.Command0Grid,
                Command1Grid = map.Command1Grid,
                TransformLookup = _transformLookup,
                TeamLookup = _teamLookup
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct SensorJob : IJobEntity
        {
            public float DeltaTime;
            public float CellSize;

            [ReadOnly] public NativeParallelMultiHashMap<int2, Entity> Command0Grid;
            [ReadOnly] public NativeParallelMultiHashMap<int2, Entity> Command1Grid;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<Team> TeamLookup;
            
            private void Execute(
                Entity self,
                ref EyeSensor sensorState,
                in EyeSensorStats sensorSettings,
                in LocalTransform selfTransform,
                in Team selfTeam)
            {
                float detectRadius = sensorSettings.DetectRadius;
                float chaseRadius = sensorSettings.ChaseRadius;
                float detectRadiusSq = detectRadius * detectRadius;
                float chaseRadiusSq = chaseRadius * chaseRadius;

                bool needFullSearch = false;

                if (sensorState.IsDetected == 0 || sensorState.DetectedEntity == Entity.Null)
                {
                    sensorState.ScanTimer -= DeltaTime;
                    if (sensorState.ScanTimer > 0f)
                        return;

                    needFullSearch = true;
                    sensorState.ScanTimer = sensorSettings.ScanInterval;
                }
                else
                {
                    Entity current = sensorState.DetectedEntity;

                    if (!TransformLookup.HasComponent(current) ||
                        !TeamLookup.HasComponent(current) ||
                        TeamLookup[current].Value == selfTeam.Value)
                    {
                        sensorState.IsDetected = 0;
                        sensorState.DetectedEntity = Entity.Null;
                        sensorState.UpdateNearestTimer = 0f;
                        needFullSearch = true;
                    }
                    else
                    {
                        float3 posA3 = selfTransform.Position;
                        float3 posB3 = TransformLookup[current].Position;
                        float2 posA = new float2(posA3.x, posA3.z);
                        float2 posB = new float2(posB3.x, posB3.z);
                        float currentDistSq = math.lengthsq(posA - posB);

                        if (currentDistSq > chaseRadiusSq)
                        {
                            sensorState.IsDetected = 0;
                            sensorState.DetectedEntity = Entity.Null;
                            sensorState.UpdateNearestTimer = 0f;
                            needFullSearch = true;
                        }
                        else
                        {
                            sensorState.UpdateNearestTimer -= DeltaTime;
                            if (sensorState.UpdateNearestTimer <= 0f)
                                needFullSearch = true;
                        }
                    }
                }

                if (!needFullSearch)
                    return;

                var selfPos3 = selfTransform.Position;
                var selfPosXZ = new float2(selfPos3.x, selfPos3.z);

                var bestEntity = Entity.Null;
                float bestDistSq = detectRadiusSq;

                var selfBucket = (int2)math.floor(selfPosXZ / CellSize);
                int maxRing = (int)math.ceil(detectRadius / CellSize);

                var enemyGrid = selfTeam.Value == 0 ? Command1Grid : Command0Grid;
                
                for (int ring = 0; ring <= maxRing; ring++)
                {
                    if (ring == 0)
                    {
                        ProcessBucket(
                            enemyGrid,
                            self,
                            selfTeam.Value,
                            selfPosXZ,
                            selfBucket,
                            ref bestEntity,
                            ref bestDistSq);
                    }
                    else
                    {
                        int minX = selfBucket.x - ring;
                        int maxX = selfBucket.x + ring;
                        int minY = selfBucket.y - ring;
                        int maxY = selfBucket.y + ring;

                        for (int x = minX; x <= maxX; x++)
                        {
                            ProcessBucket(
                                enemyGrid,
                                self,
                                selfTeam.Value,
                                selfPosXZ,
                                new int2(x, minY),
                                ref bestEntity,
                                ref bestDistSq);

                            ProcessBucket(
                                enemyGrid,
                                self,
                                selfTeam.Value,
                                selfPosXZ,
                                new int2(x, maxY),
                                ref bestEntity,
                                ref bestDistSq);
                        }
                        
                        for (int y = minY + 1; y <= maxY - 1; y++)
                        {
                            ProcessBucket(
                                enemyGrid,
                                self,
                                selfTeam.Value,
                                selfPosXZ,
                                new int2(minX, y),
                                ref bestEntity,
                                ref bestDistSq);

                            ProcessBucket(
                                enemyGrid,
                                self,
                                selfTeam.Value,
                                selfPosXZ,
                                new int2(maxX, y),
                                ref bestEntity,
                                ref bestDistSq);
                        }
                    }

                    if (bestEntity != Entity.Null && ring < maxRing)
                    {
                        int nextRing = ring + 1;

                        float nextRingMinDist = math.max(0f, (nextRing - 1) * CellSize);
                        float nextRingMinDistSq = nextRingMinDist * nextRingMinDist;

                        if (nextRingMinDistSq >= bestDistSq)
                            break;
                    }
                }

                if (bestEntity != Entity.Null)
                {
                    sensorState.IsDetected = 1;
                    sensorState.DetectedEntity = bestEntity;
                    sensorState.UpdateNearestTimer = sensorSettings.UpdateNearestInterval;
                }
                else
                {
                    sensorState.IsDetected = 0;
                    sensorState.DetectedEntity = Entity.Null;
                    sensorState.UpdateNearestTimer = sensorSettings.UpdateNearestInterval;
                }
            }

            [BurstCompile]
            private void ProcessBucket(
                NativeParallelMultiHashMap<int2, Entity> grid,
                Entity self,
                byte selfTeamValue,
                float2 selfPosXZ,
                int2 bucket,
                ref Entity bestEntity,
                ref float bestDistSq)
            {
                float bucketMinX = bucket.x * CellSize;
                float bucketMinY = bucket.y * CellSize;
                float bucketMaxX = bucketMinX + CellSize;
                float bucketMaxY = bucketMinY + CellSize;

                float closestX = math.clamp(selfPosXZ.x, bucketMinX, bucketMaxX);
                float closestY = math.clamp(selfPosXZ.y, bucketMinY, bucketMaxY);

                float2 closestPoint = new float2(closestX, closestY);
                float bucketMinDistSq = math.lengthsq(selfPosXZ - closestPoint);

                if (bucketMinDistSq >= bestDistSq)
                    return;

                NativeParallelMultiHashMapIterator<int2> it;
                Entity candidate;

                if (!grid.TryGetFirstValue(bucket, out candidate, out it))
                    return;

                do
                {
                    if (candidate == self)
                        continue;

                    if (!TransformLookup.HasComponent(candidate) || !TeamLookup.HasComponent(candidate))
                        continue;

                    if (TeamLookup[candidate].Value == selfTeamValue)
                        continue;

                    float3 candidatePos3 = TransformLookup[candidate].Position;
                    float2 candidatePosXZ = new float2(candidatePos3.x, candidatePos3.z);
                    float distSq = math.lengthsq(selfPosXZ - candidatePosXZ);

                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestEntity = candidate;
                    }
                }
                while (grid.TryGetNextValue(out candidate, ref it));
            }
        }
    }
}
