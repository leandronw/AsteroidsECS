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
 * Spawns new asteroids from all AsteroidSpawnRequest's
 */
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class AsteroidsSpawnSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem _entityCommandBufferSystem;

    private NativeArray<Entity> _asteroidPrefabs;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

        _asteroidPrefabs = new NativeArray<Entity>(3, Allocator.Persistent);  
    }

    protected override void OnStartRunning()
    {
        _asteroidPrefabs[(int)AsteroidSize.Big] = _entityCommandBufferSystem.GetSingleton<AsteroidBigPrefabReference>().Prefab;
        _asteroidPrefabs[(int)AsteroidSize.Medium] = _entityCommandBufferSystem.GetSingleton<AsteroidMediumPrefabReference>().Prefab;
        _asteroidPrefabs[(int)AsteroidSize.Small] = _entityCommandBufferSystem.GetSingleton<AsteroidSmallPrefabReference>().Prefab;
    }

    protected override void OnDestroy()
    {
        _asteroidPrefabs.Dispose();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();
        NativeArray<Entity> prefabs = _asteroidPrefabs;

        Entities
            .WithReadOnly(prefabs)
            .ForEach((
                Entity spawnEntity,
                in AsteroidsSpawnRequest spawnRequest) => 
            {
                for (int i = 0; i < spawnRequest.Amount; i++)
                {
                    Entity entity = commandBuffer.Instantiate(prefabs[(int)spawnRequest.Size]);

                    commandBuffer.SetComponent(
                        entity,
                        new Translation
                        {
                            Value = new float3(spawnRequest.Position.x, spawnRequest.Position.y, 0)
                        });

                    commandBuffer.SetComponent(
                        entity,
                        new PhysicsVelocity
                        {
                            Linear = spawnRequest.PreviousVelocity
                        });

                    commandBuffer.AddComponent<NeedsInitTag>(entity, default(NeedsInitTag));
                }

                commandBuffer.DestroyEntity(spawnEntity);

            }).Run();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}
