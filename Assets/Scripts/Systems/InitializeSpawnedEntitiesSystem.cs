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
    private Random _randomGenerator;
    private EntityQuery _query;
    private EndInitializationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        _randomGenerator = new Random((uint)System.DateTime.Now.TimeOfDay.TotalSeconds + (uint)Time.ElapsedTime);
    }

    protected override void OnUpdate()
    {
        Random random = _randomGenerator;
        _query = GetEntityQuery(
            ComponentType.ReadOnly<NeedsInitTag>(),
            ComponentType.ReadOnly<AsteroidRotationData>(),
            ComponentType.ReadOnly<AsteroidSpeedData>(),
            ComponentType.ReadWrite<PhysicsVelocity>());

        Entities
            .WithStoreEntityQueryInField(ref _query)
            .ForEach((
                ref PhysicsVelocity velocity, 
                in AsteroidRotationData rotationData,
                in AsteroidSpeedData speedData
                ) => 
            {
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