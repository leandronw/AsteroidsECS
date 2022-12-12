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
public partial class UFODestroySystem : SystemBase
{
    private EntityCommandBufferSystem _entityCommandBufferSystem;

    private Entity _vfxPrefab;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
  
    protected override void OnStartRunning()
    {
        _vfxPrefab = _entityCommandBufferSystem.GetSingleton<UFOVFXPrefabReference>().Prefab;
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();
        Entity vfxPrefab = _vfxPrefab;

        Entities
           .ForEach((
               Entity entity,
               int entityInQueryIndex,
               in DestroyedTag destroyed,
               in UFOTag ufoTag,
               in Translation position) =>
           {
               commandBuffer.DestroyEntity(entity);

               Entity vfxEntity = commandBuffer.Instantiate(vfxPrefab);
               commandBuffer.AddComponent<Translation>(
                   vfxEntity,
                   new Translation
                   {
                       Value = position.Value
                   });

           }).Schedule();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}
