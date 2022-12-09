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
 * Creates a shield representation as player's child when a new shield is picked
 */
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
public partial class ShieldEnableSystem : SystemBase
{
    public event Action<float> OnShieldEnabled; 

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
               commandBuffer.DestroyEntity(equippedShields);

               commandBuffer.RemoveComponent<ShieldEnableRequest>(playerEntity);

               Entity shieldEntity = commandBuffer.Instantiate(shieldData.ShieldPrefab);
               commandBuffer.AddComponent<ShieldEquippedTag>(shieldEntity);

               // make shield child of player
               commandBuffer.AddComponent(shieldEntity, new Parent { Value = playerEntity });
               commandBuffer.AddComponent(shieldEntity, new LocalToParent { });
               commandBuffer.AddComponent(shieldEntity, new LocalToWorld { });

               commandBuffer.AppendToBuffer<LinkedEntityGroup>(playerEntity, shieldEntity);
               /*
               if (childrenBuffer.IsEmpty)
               {
                   childrenBuffer.Add(playerEntity);
               }

               childrenBuffer.Add(shieldEntity);*/

               Entity eventEntity = commandBuffer.CreateEntity();
               commandBuffer.AddComponent<ShieldEnabledEvent>(
                   eventEntity,
                   new ShieldEnabledEvent { Duration = shieldData.TimeRemaining });


           }).Schedule();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);

        //
        // dispatch events
        //
        var eventsCommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();

        Entities
            .WithoutBurst()
            .ForEach((Entity eventEntity, ref ShieldEnabledEvent eventComponent) =>
            {
                OnShieldEnabled?.Invoke(eventComponent.Duration);
                eventsCommandBuffer.DestroyEntity(eventEntity);

            }).Run();
    }

    public struct ShieldEnabledEvent : IComponentData
    {
        public float Duration;
    }

    public struct ShieldDepletedEvent : IComponentData
    {
        public Entity Entity;
    }

}   

