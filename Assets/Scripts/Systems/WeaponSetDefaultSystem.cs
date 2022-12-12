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
 * Assigns default weapon to players without weapon
 */
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
public partial class WeaponSystem : SystemBase
{

    private EntityCommandBufferSystem _entityCommandBufferSystem;

    private WeaponData _defaultWeapon;
    private WeaponEquipRequest _defaultWeaponEquipRequest;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();  
    }

    protected override void OnStartRunning()
    {
        EntityQuery defaultWeaponQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] {
                typeof(DefaultWeaponDataTag)}
        });

        Entity defaultWeaponEntity = defaultWeaponQuery.GetSingletonEntity();
        _defaultWeapon = World.EntityManager.GetComponentData<WeaponData>(defaultWeaponEntity);
        _defaultWeaponEquipRequest = World.EntityManager.GetComponentData<WeaponEquipRequest>(defaultWeaponEntity);
    }

    protected override void OnUpdate()
    {
        WeaponData defaultWeapon = _defaultWeapon;
        WeaponEquipRequest defaultWeaponEquipData = _defaultWeaponEquipRequest;

        EntityCommandBuffer.ParallelWriter commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
            .WithNone<WeaponData>()
            .ForEach((
                Entity playerEntity,
                int entityInQueryIndex,
                in PlayerTag playerTag) =>
            {
                commandBuffer.AddComponent<WeaponData>(
                    entityInQueryIndex,
                    playerEntity,
                    defaultWeapon);

                commandBuffer.AddComponent<WeaponEquipRequest>(
                   entityInQueryIndex,
                   playerEntity,
                   defaultWeaponEquipData);

            }).ScheduleParallel();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}

