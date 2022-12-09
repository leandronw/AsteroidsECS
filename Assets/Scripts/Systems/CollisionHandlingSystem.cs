using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * Handles all collision events. 
 * Rise events when player dies, a powerup is picked or an asteroid is destroyed
 */
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
public partial class CollisionHandlingSystem : SystemBase
{
    // delegates
    public event Action<float2> OnPlayerDestroyed;
    public event Action<float2> OnPowerUpPicked;
    public event Action<float2, AsteroidSize> OnAsteroidDestroyed;

    private EntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityQuery allPowerupsQuery = GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[] {
                typeof(ShieldPowerUpTag),
                typeof(WeaponPowerUpTag)},
            None = new ComponentType[] {
                typeof(PickedTag)}
        });
        NativeArray<Entity> allPowerupEntities = allPowerupsQuery.ToEntityArray(Allocator.TempJob);

        EntityQuery allAsteroidsQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] {
                typeof(AsteroidSizeComponent)},
            None = new ComponentType[] {
                typeof(DestroyedTag)}
        });
        NativeArray<Entity> allAsteroidEntities = allAsteroidsQuery.ToEntityArray(Allocator.TempJob);

        EntityQuery shieldedQuery = GetEntityQuery(typeof(PlayerTag), typeof(ShieldData));
        NativeArray<Entity> shieldedPlayers = shieldedQuery.ToEntityArray(Allocator.TempJob);

        EntityCommandBuffer.ParallelWriter commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        
        JobHandle bulletsJobHandle = Entities
            .WithNone<DestroyedTag>()
            .ForEach((
                Entity bulletEntity,
                int entityInQueryIndex,
                in BulletTag bulletTag,
                in CollisionData collision
                ) =>
            {
                // bullets are just destroyed
                commandBuffer.DestroyEntity(entityInQueryIndex, bulletEntity);

            }).ScheduleParallel(this.Dependency);


        JobHandle asteroidsJobHandle = new HandleAsteroidJob()
        {
            CommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()

        }.ScheduleParallel(this.Dependency);

        JobHandle playersJobHandle = new HandlePlayerJob()
        {
            AllAsteroids = allAsteroidEntities,
            AllPowerups = allPowerupEntities,
            AllShieldedPlayers = shieldedPlayers,
            CommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()

        }.ScheduleParallel(this.Dependency);


        JobHandle intermediateDependencies = JobHandle.CombineDependencies(bulletsJobHandle, asteroidsJobHandle);
        this.Dependency = JobHandle.CombineDependencies(intermediateDependencies, playersJobHandle);
        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);


        //
        // dispatch events
        //
        var eventsCommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();

        Entities
            .WithoutBurst()
            .ForEach((Entity eventEntity, ref PlayerDestroyedEvent eventComponent) =>
            {
                OnPlayerDestroyed?.Invoke(eventComponent.Position);
                eventsCommandBuffer.DestroyEntity(eventEntity);

            }).Run();

        Entities
            .WithoutBurst()
            .ForEach((Entity eventEntity, ref PowerUpEvent eventComponent) =>
            {
                OnPowerUpPicked?.Invoke(eventComponent.Position);
                eventsCommandBuffer.DestroyEntity(eventEntity);

            }).Run();

        Entities
           .WithoutBurst()
           .ForEach((Entity eventEntity, ref AsteroidDestroyedEvent eventComponent) =>
           {
               OnAsteroidDestroyed?.Invoke(eventComponent.Position, eventComponent.Size);
               eventsCommandBuffer.DestroyEntity(eventEntity);

           }).Run();

    }


    [BurstCompile]
    [WithNone(typeof(DestroyedTag))]
    partial struct HandlePlayerJob : IJobEntity
    {
        [DeallocateOnJobCompletion] public NativeArray<Entity> AllPowerups;
        [DeallocateOnJobCompletion] public NativeArray<Entity> AllAsteroids;
        [DeallocateOnJobCompletion] public NativeArray<Entity> AllShieldedPlayers;

        [WriteOnly] public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(
            Entity playerEntity,
            [EntityInQueryIndex] int entityInQueryIndex,
            in PlayerTag playerTag,
            in CollisionData collision,
            in Translation position
            )
        {
            bool collidedWithPowerUp = AllPowerups.Contains(collision.otherEntity);
            if (collidedWithPowerUp)
            {
                CommandBuffer.AddComponent<PickedTag>(
                       entityInQueryIndex,
                       collision.otherEntity);
            }
            else
            {
                bool collidedWithAsteroid = AllAsteroids.Contains(collision.otherEntity);
                if (collidedWithAsteroid)
                {
                    bool isShielded = AllShieldedPlayers.Contains(playerEntity);
                    if (!isShielded)
                    {
                        CommandBuffer.AddComponent<DestroyedTag>(
                            entityInQueryIndex,
                            playerEntity);

                        Entity eventEntity = CommandBuffer.CreateEntity(entityInQueryIndex);
                        CommandBuffer.AddComponent<PlayerDestroyedEvent>(
                            entityInQueryIndex,
                            eventEntity,
                            new PlayerDestroyedEvent
                            {
                                Position = position.Value.xy
                            });
                    }
                }

            }

            CommandBuffer.RemoveComponent<CollisionData>(entityInQueryIndex, playerEntity);

        }
    }

    [BurstCompile]
    partial struct HandleAsteroidJob : IJobEntity
    {
        [WriteOnly] public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(
            Entity asteroidEntity,
            [EntityInQueryIndex] int entityInQueryIndex,
            in AsteroidSizeComponent asteroidSize,
            in CollisionData collision,
            in Translation position,
            in PhysicsVelocity velocity
            )
        {
            if (asteroidSize.Size != AsteroidSize.Small)
            {
                Entity spawnerEntity = CommandBuffer.CreateEntity(entityInQueryIndex);
                CommandBuffer.AddComponent<AsteroidSpawnRequest>(
                    entityInQueryIndex,
                    spawnerEntity,
                    new AsteroidSpawnRequest
                    {
                        Amount = 2,
                        Position = position.Value.xy,
                        PreviousVelocity = velocity.Linear,
                        Size = asteroidSize.Size == AsteroidSize.Big ? AsteroidSize.Medium : AsteroidSize.Small
                    });
            }

            CommandBuffer.DestroyEntity(entityInQueryIndex, asteroidEntity);

            Entity eventEntity = CommandBuffer.CreateEntity(entityInQueryIndex);
            CommandBuffer.AddComponent<AsteroidDestroyedEvent>(
                entityInQueryIndex,
                eventEntity,
                new AsteroidDestroyedEvent
                {
                    Size = asteroidSize.Size,
                    Position = position.Value.xy
                });
        }
    }

    public struct PlayerDestroyedEvent : IComponentData
    {
        public float2 Position;
    }

    public struct AsteroidDestroyedEvent : IComponentData
    {
        public AsteroidSize Size;
        public float2 Position;
    }

    public struct PowerUpEvent : IComponentData
    {
        public float2 Position;
    }
}


