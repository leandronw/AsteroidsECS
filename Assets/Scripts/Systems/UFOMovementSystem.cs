using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

/*
 * Handles movement for UFOs
 * */
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class UFOMovementSystem : SystemBase
{
    private EntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();
        float deltaTime = Time.DeltaTime;

        EntityQuery allAsteroidsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] {
                typeof(AsteroidSizeComponent),
                typeof(Translation)},
            None = new ComponentType[] {
                typeof(DestroyedTag)}
        });
        NativeArray<Translation> allAsteroidPositions = allAsteroidsQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        JobHandle jobHandle = new HandleUFOMovementJob()
        {
            CommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
            DeltaTime = deltaTime,
            AllAsteroidPositions = allAsteroidPositions

        }.ScheduleParallel(this.Dependency);

        _entityCommandBufferSystem.AddJobHandleForProducer(jobHandle);
    }

    [BurstCompile]
    [WithNone(typeof(DestroyedTag))]
    partial struct HandleUFOMovementJob : IJobEntity
    {
        [WriteOnly] public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly] public float DeltaTime;

        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Translation> AllAsteroidPositions;

        public void Execute(
            Entity bulletEntity,
            [EntityInQueryIndex] int entityInQueryIndex,
            ref PhysicsVelocity velocity,
            ref UFOBrainComponent ufoBrain,
            in UFOTag ufoTag,
            in Translation ufoPosition,
            in UFOSpeedData ufoSpeedData
            )
        {
            bool goingLeftToRight = velocity.Linear.x > 0f;

            ufoBrain.ElapsedTimeSinceLastChange += DeltaTime;

            bool canChangeDirection = ufoBrain.ElapsedTimeSinceLastChange >= ufoBrain.MinTimeSinceLastChange;
            if (canChangeDirection)
            {
                float2 newVelocity = CalculateNewVelocity(
                    goingLeftToRight,
                    ufoPosition.Value.xy,
                    velocity.Linear.xy,
                    ufoSpeedData.Speed,
                    ufoBrain.MinDistanceToCheckObstacle);
                velocity.Linear = new float3(newVelocity.x, newVelocity.y, 0f);
                ufoBrain.ElapsedTimeSinceLastChange = 0;
            }
        }

        // very simplistic and inaccurate obstacle avoidance algorithm
        private float2 CalculateNewVelocity(
            bool goingLeftToRight, 
            float2 ufoPosition, 
            float2 previousVelocity,
            float speed,
            float minDistanceToInclude)
        {
            float2 newDirection = float2.zero;

            for (int i = 0; i < AllAsteroidPositions.Length; i++)
            {
                float2 asteroidPosition = AllAsteroidPositions[i].Value.xy;

                float2 difference = ufoPosition - asteroidPosition;

                float distance = math.length(difference);
                if (distance > minDistanceToInclude)
                {
                    // skip asteroids that are too far away
                    continue;
                }

                float2 oppositeForce = math.normalize(difference) / distance;
                float newX = oppositeForce.x;
                float newY = oppositeForce.y;

                if (oppositeForce.x < 0f && goingLeftToRight)
                {
                    if (oppositeForce.y > 0f)
                    {
                        newX = oppositeForce.y;
                        newY = -oppositeForce.x;
                    } 
                    else
                    {
                        newX = -oppositeForce.y;
                        newY = oppositeForce.x;
                    } 

                }
                else if (oppositeForce.x > 0f && !goingLeftToRight)
                {
                    if (oppositeForce.y > 0f)
                    {
                        newX = -oppositeForce.y;
                        newY = oppositeForce.x;
                    }
                    else
                    {
                        newX = oppositeForce.y;
                        newY = -oppositeForce.x;
                    }
                }

                oppositeForce.x = newX;
                oppositeForce.y = newY;

                newDirection += oppositeForce;
            }

            if (newDirection.x != 0 && newDirection.y != 0)
                return math.normalize(newDirection) * speed;

            else
                return previousVelocity;
        }
    }

}

