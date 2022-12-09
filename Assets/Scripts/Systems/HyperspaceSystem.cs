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
public partial class HyperspaceSystem : SystemBase
{
    public event Action<float2, float2> OnHyperspace; 

    private EntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();
        float2 randomPosition = GameArea.GetRandomPosition();

        Entities
           .WithNone<DestroyedTag>()
           .ForEach((
               Entity playerEntity,
               in PlayerTag playerTag,
               in JumpToHyperspaceTag hyperspaceTag,
               in Translation position) =>
           {
                float2 previousPosition = position.Value.xy;
                float2 newPosition = randomPosition;

                commandBuffer.RemoveComponent<JumpToHyperspaceTag>(playerEntity);
                commandBuffer.SetComponent<Translation>(
                    playerEntity,
                    new Translation
                    {
                        Value = new float3(newPosition.x, newPosition.y, 0f)
                    });

                Entity eventEntity = commandBuffer.CreateEntity();
                commandBuffer.AddComponent<HyperspaceEvent>(
                    eventEntity,
                    new HyperspaceEvent { 
                        PreviousPosition = previousPosition,
                        NewPosition = newPosition}
                    );


           }).Schedule();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);

        //
        // dispatch events
        //
        var eventsCommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();

        Entities
            .WithoutBurst()
            .ForEach((Entity eventEntity, ref HyperspaceEvent eventComponent) =>
            {
                OnHyperspace?.Invoke(eventComponent.PreviousPosition, eventComponent.NewPosition);
                eventsCommandBuffer.DestroyEntity(eventEntity);

            }).Run();
    }

    public struct HyperspaceEvent : IComponentData
    {
        public float2 PreviousPosition;
        public float2 NewPosition;
    }
}   

