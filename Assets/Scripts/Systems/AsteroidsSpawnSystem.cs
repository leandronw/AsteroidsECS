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
 * Gathers all AsteroidSpawnRequestData components and spawn the required entities 
 */
[UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]

public partial class AsteroidsSpawnSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem _entityCommandBufferSystem;
    private EntityQuery _query;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        _query = GetEntityQuery(ComponentType.ReadWrite<AsteroidSpawnRequestData>());
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();

        Entities
            .WithStoreEntityQueryInField(ref _query)
            .ForEach((in AsteroidSpawnRequestData spawnData) => {
                for (int i = 0; i < spawnData.Amount; i++)
                {
                    Entity entity = commandBuffer.Instantiate(spawnData.Prefab);

                    commandBuffer.SetComponent(
                        entity,
                        new Translation
                        {
                            Value = new float3(spawnData.Position.x, spawnData.Position.y, 0)
                        });

                    commandBuffer.AddComponent<NeedsInitTag>(entity, default(NeedsInitTag));
                }
            }).Run();

        commandBuffer.DestroyEntitiesForEntityQuery(_query);
        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}
