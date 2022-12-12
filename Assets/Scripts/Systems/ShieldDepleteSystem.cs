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
 * Handles removing shield when time is up
 */
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ShieldDepleteSystem : SystemBase
{

    private EntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
  
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();
        
        EntityQuery equippedShieldQuery = GetEntityQuery(typeof(ShieldEquippedTag));
        NativeArray<Entity> equippedShields = equippedShieldQuery.ToEntityArray(Allocator.TempJob);

        Entities
            .WithDisposeOnCompletion(equippedShields)
            .ForEach((
                Entity entity,  
                ref ShieldData shieldData,
                in PlayerTag playerTag) =>
            {
                shieldData.TimeRemaining -= deltaTime;

                if (shieldData.TimeRemaining <= 0)
                {
                    commandBuffer.RemoveComponent<ShieldData>(entity);
                    commandBuffer.DestroyEntity(equippedShields);

                    Entity eventEntity = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent<ShieldDepletedEvent>(eventEntity);

                    Entity soundEventEntity = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent<SfxEvent>(
                        soundEventEntity,
                        new SfxEvent
                        {
                            Sound = SoundId.SHIELD_DISABLED
                        });
                }

            }).Schedule();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);

    }

}   

