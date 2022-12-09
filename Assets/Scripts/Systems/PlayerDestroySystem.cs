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
 * Handles the destruction of Player entities
 */
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class PlayerDestroySystem : SystemBase
{
    private EntityCommandBufferSystem _entityCommandBufferSystem;
    private EntityQuery _query;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] {
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadWrite<DestroyedTag>()}
        });
    }
  
    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();
        commandBuffer.DestroyEntitiesForEntityQuery(_query);
    }
}
