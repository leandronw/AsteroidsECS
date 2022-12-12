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
 * Handles picked weapon PowerUps
 */
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class WeaponPickedpSystem : SystemBase
{

    private EntityCommandBufferSystem _entityCommandBufferSystem;
 
    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();

        Entities
            .ForEach((
                Entity powerUpEntity,
                in WeaponPowerUpTag powerupTag,
                in PickedTag pickedTag,
                in WeaponData weaponData,
                in WeaponEquipRequest weaponEquipRequest,
                in CollisionComponent collision) =>
            {
                Entity playerEntity = collision.otherEntity;
                commandBuffer.AddComponent<WeaponData>(
                    playerEntity,
                    weaponData);

                commandBuffer.AddComponent<WeaponEquipRequest>(
                    playerEntity,
                    weaponEquipRequest);

                commandBuffer.DestroyEntity(powerUpEntity);

            }).Schedule();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}
