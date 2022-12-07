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
public partial class PlayerInputSystem : SystemBase
{
    private Entity _bulletPrefab;
    private EndInitializationEntityCommandBufferSystem _ecbSystem;

    protected override void OnCreate()
    {
        _ecbSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        if (_bulletPrefab == Entity.Null)
        {
            _bulletPrefab = this.GetSingleton<BulletPrefabReference>().Prefab;
            return;
        }

        float deltaTime = Time.DeltaTime;

        Entities
            .WithoutBurst()
            .ForEach((
                Entity playerEntity,
                ref PhysicsVelocity velocity,
                ref Rotation rotation, 
                ref BulletSpawnData bulletSpawnData,
                in Translation position,
                in PlayerAccelerationData accelerationData,
                in PlayerRotationData rotationData,
                in PlayerInputData inputData
                ) =>
            {
                EntityCommandBuffer ecb = _ecbSystem.CreateCommandBuffer();

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
                    ecb.AddComponent<ThrustingTag>(playerEntity);
                }
                else
                {
                    ecb.RemoveComponent<ThrustingTag>(playerEntity);
                }

                bulletSpawnData.ElapsedTimeSinceLast += deltaTime;
                bool canShoot = bulletSpawnData.ElapsedTimeSinceLast > (1 / bulletSpawnData.AmountPerSecond);

                if (inputData.IsShooting && canShoot)
                {
                    bulletSpawnData.ElapsedTimeSinceLast = 0;
                    SpawnBullet(in velocity, in rotation, in position, in bulletSpawnData);
                }

                if (inputData.IsJumpingToHyperspace)
                {
                    ecb.AddComponent<JumpToHyperspaceTag>(playerEntity);
                }
            })
            .Run();
    }

    private void SpawnBullet(
        in PhysicsVelocity playerVelocity, 
        in Rotation rotation, 
        in Translation position, 
        in BulletSpawnData spawnData)
    {
        EntityCommandBuffer commandBuffer = _ecbSystem.CreateCommandBuffer();
        Entity bulletEntity = commandBuffer.Instantiate(_bulletPrefab);

        var bulletPosition = new Translation 
        { 
            Value = position.Value + math.mul(rotation.Value, spawnData.SpawnOffset).xyz 
        };

        var bulletVelocity = new PhysicsVelocity
        {
            Linear = spawnData.BulletSpeed * math.mul(rotation.Value, new float3(0f, 1f, 0f)).xyz + playerVelocity.Linear
        };

        commandBuffer.SetComponent(bulletEntity, bulletPosition);
        commandBuffer.SetComponent(bulletEntity, rotation);
        commandBuffer.SetComponent(bulletEntity, bulletVelocity);

        _ecbSystem.AddJobHandleForProducer(this.Dependency);
    }
}
