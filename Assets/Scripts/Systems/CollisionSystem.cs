using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ExportPhysicsWorld))]
[UpdateBefore(typeof(EndFixedStepSimulationEntityCommandBufferSystem))]
public partial class CollisionSystem : SystemBase
{
    private StepPhysicsWorld _stepPhysicsWorld;
    private EndFixedStepSimulationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        _ecbSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        this.RegisterPhysicsRuntimeSystemReadOnly();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = _ecbSystem.CreateCommandBuffer();
        Dependency = new TriggerListenerJob
        {
            ECB = ecb
        }
        .Schedule(_stepPhysicsWorld.Simulation, Dependency);

        _ecbSystem.AddJobHandleForProducer(Dependency);
     }

    [BurstCompile]
    struct TriggerListenerJob : ITriggerEventsJob
    {
        public EntityCommandBuffer ECB;

        public void Execute(TriggerEvent triggerEvent)
        {
            ECB.AddComponent<CollidedTag>(triggerEvent.EntityA);
            ECB.AddComponent<CollidedTag>(triggerEvent.EntityB);
        }
    }
}


