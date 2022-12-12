using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

/*
 * Handles input for players
 * */

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(UpdateWorldTimeSystem))]
public partial class PlayerInputHandlingSystem : SystemBase
{
    private Entity _bulletPrefab;
    private EntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities
            .WithoutBurst()
            .ForEach((
                Entity playerEntity,
                ref PhysicsVelocity velocity,
                ref Rotation rotation, 
                ref WeaponData weaponData,
                in Translation position,
                in PlayerAccelerationData accelerationData,
                in PlayerRotationData rotationData,
                in PlayerInputData inputData) =>
            {
                EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();

                if (inputData.IsTurningLeft)
                {
                    rotation.Value = math.mul(rotation.Value, quaternion.RotateZ(rotationData.Speed * deltaTime));
                }
                else if (inputData.IsTurningRight)
                {
                    rotation.Value = math.mul(rotation.Value, quaternion.RotateZ(-rotationData.Speed * deltaTime));
                }

                if (inputData.IsThrusting)
                {
                    float3 currentVelocity = velocity.Linear;
                    float3 forwardVector = math.rotate(rotation.Value, math.up());
                    float currentSpeedForward = math.dot(currentVelocity, forwardVector);
                    if (currentSpeedForward < accelerationData.MaxSpeed)
                    {
                        velocity.Linear += forwardVector * accelerationData.Acceleration * deltaTime;
                    }
                    commandBuffer.AddComponent<ThrustingTag>(playerEntity);
                }
                else
                {
                    commandBuffer.RemoveComponent<ThrustingTag>(playerEntity);
                }

                weaponData.ElapsedTimeSinceLastShot += deltaTime;
                bool canShoot = weaponData.ElapsedTimeSinceLastShot > (1 / weaponData.BulletsPerSecond);

                if (inputData.IsShooting && canShoot)
                {
                    weaponData.ElapsedTimeSinceLastShot = 0;
                    ShootWeapon(in velocity, in rotation, in position, in weaponData, ref commandBuffer);
                }

                if (inputData.IsJumpingToHyperspace)
                {
                    commandBuffer.AddComponent<JumpToHyperspaceTag>(playerEntity);
                }
            })
            .Run();


        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }

    private void ShootWeapon(
        in PhysicsVelocity playerVelocity, 
        in Rotation rotation, 
        in Translation position, 
        in WeaponData weaponData,
        ref EntityCommandBuffer commandBuffer)
    {
        Entity bulletEntity = commandBuffer.Instantiate(weaponData.BulletPrefab);

        var bulletPosition = new Translation 
        { 
            Value = position.Value + math.mul(rotation.Value, weaponData.BulletSpawnOffset).xyz 
        };

        var bulletVelocity = new PhysicsVelocity
        {
            Linear = weaponData.BulletSpeed * math.mul(rotation.Value, new float3(0f, 1f, 0f)).xyz + playerVelocity.Linear
        };

        commandBuffer.SetComponent(bulletEntity, bulletPosition);
        commandBuffer.SetComponent(bulletEntity, rotation);
        commandBuffer.SetComponent(bulletEntity, bulletVelocity);

    }
}
