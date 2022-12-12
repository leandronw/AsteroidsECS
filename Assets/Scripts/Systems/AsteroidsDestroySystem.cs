using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.PlayerLoop;

/*
 * Handles the destruction of asteroids and the spawning of smaller ones
 */
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class AsteroidsDestroySystem : SystemBase
{
    private EntityCommandBufferSystem _entityCommandBufferSystem;

    private NativeArray<Entity> _vfxPrefabs;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        _vfxPrefabs = new NativeArray<Entity>(3, Allocator.Persistent);
    }

    protected override void OnStartRunning()
    {
        _vfxPrefabs[(int)AsteroidSize.Big] = _entityCommandBufferSystem.GetSingleton<AsteroidBigVFXPrefabReference>().Prefab;
        _vfxPrefabs[(int)AsteroidSize.Medium] = _entityCommandBufferSystem.GetSingleton<AsteroidMediumVFXPrefabReference>().Prefab;
        _vfxPrefabs[(int)AsteroidSize.Small] = _entityCommandBufferSystem.GetSingleton<AsteroidSmallVFXPrefabReference>().Prefab;
    }

    protected override void OnDestroy()
    {
        _vfxPrefabs.Dispose();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        NativeArray<Entity> vfxPrefabs = _vfxPrefabs;

        Entities
            .WithReadOnly(vfxPrefabs)
            .ForEach((
                Entity entity,
                int entityInQueryIndex,
                in DestroyedTag destroyed,
                in AsteroidSizeComponent asteroidSize,
                in PhysicsVelocity velocity,
                in Translation position) => 
            {
                if (asteroidSize.Size != AsteroidSize.Small)
                {
                    Entity spawnerEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                    commandBuffer.AddComponent<AsteroidsSpawnRequest>(
                        entityInQueryIndex,
                        spawnerEntity,
                        new AsteroidsSpawnRequest
                        {
                            Amount = 2,
                            Position = position.Value.xy,
                            PreviousVelocity = velocity.Linear,
                            Size = asteroidSize.Size == AsteroidSize.Big ? AsteroidSize.Medium : AsteroidSize.Small
                        });
                }

                commandBuffer.DestroyEntity(entityInQueryIndex, entity);

                Entity vfxEntity = commandBuffer.Instantiate(entityInQueryIndex, vfxPrefabs[(int)asteroidSize.Size]);
                commandBuffer.AddComponent<Translation>(
                    entityInQueryIndex,
                    vfxEntity,
                    new Translation
                    {
                        Value = position.Value
                    });

            }).ScheduleParallel();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);

    }
}
