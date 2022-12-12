using System;
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
 * Creates the shield visualization when a shield power-up is picked
 */
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
public partial class ShieldEnableSystem : SystemBase
{
    private EntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();
        EntityQuery equippedShieldQuery = GetEntityQuery(typeof(ShieldEquippedTag));
        NativeArray<Entity> equippedShields = equippedShieldQuery.ToEntityArray(Allocator.TempJob);

        Entities
           .WithDisposeOnCompletion(equippedShields)
           .WithNone<DestroyedTag>()
           .ForEach((
               Entity playerEntity,
               in PlayerTag playerTag,
               in ShieldEnableRequest request,
               in ShieldData shieldData) =>
           {
               commandBuffer.RemoveComponent<ShieldEnableRequest>(playerEntity);

               // destroy any previous evisting shield
               commandBuffer.DestroyEntity(equippedShields);

               // create new shield
               Entity shieldEntity = commandBuffer.Instantiate(shieldData.ShieldPrefab);
               commandBuffer.AddComponent<ShieldEquippedTag>(shieldEntity);

               // make shield child of player
               commandBuffer.AddComponent(shieldEntity, new Parent { Value = playerEntity });
               commandBuffer.AddComponent(shieldEntity, new LocalToParent { });
               commandBuffer.AddComponent(shieldEntity, new LocalToWorld { });
               commandBuffer.AppendToBuffer<LinkedEntityGroup>(playerEntity, shieldEntity);

               // create events
               Entity eventEntity = commandBuffer.CreateEntity();
               commandBuffer.AddComponent<ShieldEnabledEvent>(
                   eventEntity,
                   new ShieldEnabledEvent 
                   { 
                       Duration = shieldData.TimeRemaining 
                   });

               Entity soundEventEntity = commandBuffer.CreateEntity();
               commandBuffer.AddComponent<SfxEvent>(
                   soundEventEntity,
                   new SfxEvent
                   {
                       Sound = SoundId.SHIELD_ENABLED
                   });

           }).Schedule();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);

    }

}   

