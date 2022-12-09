using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.Assertions.Must;

/*
 * Initializes all Asteroids with a NeedsInitTag component and removes that component
 */

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
public partial class AsteroidsInitializeSystem : SystemBase
{
    private EndInitializationEntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .ForEach((
                Entity asteroidEntity,
                int entityInQueryIndex,
                ref PhysicsVelocity velocity, 
                in AsteroidRotationData rotationData,
                in AsteroidSpeedData speedData,
                in NeedsInitTag tag) => 
            {
                Random random = Random.CreateFromIndex((uint)entityInQueryIndex);

                float randomSpeed = random.NextFloat(speedData.MinSpeed, speedData.MaxSpeed);
                float3 previousVelocity = velocity.Linear;
                float3 newVelocity;

                if (previousVelocity.x == 0 && previousVelocity.y == 0)
                {
                    float2 newVelocity2D = random.NextFloat2Direction() * randomSpeed;
                    newVelocity = new float3(newVelocity2D.x, newVelocity2D.y, 0f);
                }
                else
                {
                    float randomRotation = random.NextFloat(-0.7f, 0.7f);
                    newVelocity = math.rotate(quaternion.RotateZ(randomRotation), previousVelocity);
                    newVelocity = math.normalize(newVelocity) * randomSpeed;
                }

                velocity.Linear = newVelocity;

                float randomAngularSpeed = random.NextFloat(rotationData.MaxSpeed);
                velocity.Angular = new float3(0f, 0f, randomAngularSpeed);

                commandBuffer.RemoveComponent<NeedsInitTag>(entityInQueryIndex, asteroidEntity);


            }).ScheduleParallel();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}