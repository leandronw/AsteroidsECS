using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ExportPhysicsWorld))]
public partial class CollisionDetectionSystem : SystemBase
{
    private StepPhysicsWorld _stepPhysicsWorld;
    private EntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        this.RegisterPhysicsRuntimeSystemReadOnly();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();
        
        EntityQuery alreadyCollidedQuery = GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[] {
                ComponentType.ReadOnly<DestroyedTag>(),
                ComponentType.ReadOnly<PickedTag>(),
                ComponentType.ReadOnly<CollisionComponent>()}
        });

        NativeArray<Entity> alreadyCollidedEntities = alreadyCollidedQuery.ToEntityArray(Allocator.TempJob);

        Dependency = new TriggerListenerJob
        {
            CommandBuffer = commandBuffer,
            AlreadyCollidedEntities = alreadyCollidedEntities
        }
        .Schedule(_stepPhysicsWorld.Simulation, Dependency);

        _entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
     }

    [BurstCompile]
    struct TriggerListenerJob : ITriggerEventsJob
    {
        public EntityCommandBuffer CommandBuffer;
        [DeallocateOnJobCompletion] public NativeArray<Entity> AlreadyCollidedEntities;

        public void Execute(TriggerEvent triggerEvent)
        {
            bool ignoreOne = AlreadyCollidedEntities.Contains(triggerEvent.EntityA);
            bool ignoreOther = AlreadyCollidedEntities.Contains(triggerEvent.EntityB);
            if (ignoreOne || ignoreOther)
            {
                return;
            }

            CommandBuffer.AddComponent<CollisionComponent>(
                triggerEvent.EntityA, 
                new CollisionComponent { otherEntity = triggerEvent.EntityB  });

            CommandBuffer.AddComponent<CollisionComponent>(
                triggerEvent.EntityB,
                new CollisionComponent { otherEntity = triggerEvent.EntityA });
        }
    }
}


