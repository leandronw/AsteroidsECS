using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

/*
 * Initializes all Asteroids with a NeedsInitTag component and removes that component
 */

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
public partial class InitializeAsteroidsSystem : SystemBase
{
    private EntityQuery _query;
    private EndInitializationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();

        _query = GetEntityQuery(
            ComponentType.ReadOnly<AsteroidRotationData>(),
            ComponentType.ReadOnly<AsteroidSpeedData>(),
            ComponentType.ReadWrite<NeedsInitTag>(),
            ComponentType.ReadWrite<PhysicsVelocity>());
    }

    protected override void OnUpdate()
    { 
        Entities
            .WithStoreEntityQueryInField(ref _query)
            .ForEach((
                int entityInQueryIndex,
                ref PhysicsVelocity velocity, 
                in AsteroidRotationData rotationData,
                in AsteroidSpeedData speedData,
                in NeedsInitTag tag
                ) => 
            {
                Random random = Random.CreateFromIndex((uint)entityInQueryIndex);

                float randomSpeed = random.NextFloat(speedData.MinSpeed, speedData.MaxSpeed);
                float2 randomVelocity = random.NextFloat2Direction() * randomSpeed;
                velocity.Linear = new float3(randomVelocity.x, randomVelocity.y, 0f);

                float randomAngularSpeed = random.NextFloat(rotationData.MaxSpeed);
                velocity.Angular = new float3(0f, 0f, randomAngularSpeed);
                
            }).ScheduleParallel();

        EntityCommandBuffer commandBuffer = _ecbSystem.CreateCommandBuffer();
        commandBuffer.RemoveComponentForEntityQuery(_query, typeof(NeedsInitTag));

        _ecbSystem.AddJobHandleForProducer(this.Dependency);
    }
}