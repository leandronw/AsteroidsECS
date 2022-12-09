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

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
  
    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .ForEach((
                Entity entity,
                int entityInQueryIndex,
                in DestroyedTag destroyed,
                in AsteroidSizeComponent asteroidSize,
                in PhysicsVelocity velocity,
                in Translation position,
                in CollisionData collision) => 
            {
                if (asteroidSize.Size != AsteroidSize.Small)
                {
                    Entity spawnerEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                    commandBuffer.AddComponent<AsteroidSpawnRequest>(
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

                commandBuffer.DestroyEntity(entityInQueryIndex, entity);

            }).ScheduleParallel();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);

    }
}
