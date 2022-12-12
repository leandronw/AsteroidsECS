using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * Makes UFOs shoot their bullets
 * */

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(UpdateWorldTimeSystem))]
public partial class UFOAttackSystem : SystemBase
{
    private EntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();
        float deltaTime = Time.DeltaTime;

        Entities
            .ForEach((
                Entity ufoEntity,
                ref UFORotatingWeaponData weaponData,
                in UFOTag ufoTag,
                in PhysicsVelocity velocity,
                in Translation position) =>
            {
                weaponData.ElapsedTimeSinceLastShot += deltaTime;
                bool canShoot = weaponData.ElapsedTimeSinceLastShot > (1 / weaponData.BulletsPerSecond);

                if (canShoot)
                {
                    ShootWeapon(in velocity, in position, ref weaponData, ref commandBuffer);
                    weaponData.ElapsedTimeSinceLastShot = 0;
                }

            }).Run();


        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }

    static private void ShootWeapon(
        in PhysicsVelocity ufoVelocity,
        in Translation position, 
        ref UFORotatingWeaponData weaponData,
        ref EntityCommandBuffer commandBuffer)
    {
        Entity bulletEntity = commandBuffer.Instantiate(weaponData.BulletPrefab);

        weaponData.LastShotRotation += weaponData.RotationPerShot;

        quaternion rotation = quaternion.RotateZ(weaponData.LastShotRotation);

        var bulletRotation = new Rotation
        {
            Value = rotation
        };

        var bulletPosition = new Translation 
        { 
            Value = position.Value
        };

        var bulletVelocity = new PhysicsVelocity
        {
            Linear = weaponData.BulletSpeed * math.mul(rotation, new float3(0f, 1f, 0f)).xyz + ufoVelocity.Linear
        };

        commandBuffer.AddComponent(bulletEntity, bulletPosition);
        commandBuffer.AddComponent(bulletEntity, bulletRotation);
        commandBuffer.AddComponent(bulletEntity, bulletVelocity);

        SfxPlayer.Instance.PlaySound(SoundId.UFO_SHOOT);

    }
}
