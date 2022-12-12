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
 * Creates a weapon representation as player's child when a new weapon is picked
 */
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
public partial class WeaponEquipSystem : SystemBase
{
    public event Action OnWeaponEquipped;

    private EntityCommandBufferSystem _entityCommandBufferSystem;
 
    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>(); 
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();
        EntityQuery equippedWeaponQuery = GetEntityQuery(typeof(WeaponEquippedTag));
        NativeArray<Entity> equippedWeapons = equippedWeaponQuery.ToEntityArray(Allocator.TempJob);

        Entities
            .WithDisposeOnCompletion(equippedWeapons)
            .WithNone<DestroyedTag>()
            .ForEach((
                Entity playerEntity, 
                in PlayerTag playerTag,
                in WeaponEquipRequest request) =>
            {
                commandBuffer.DestroyEntity(equippedWeapons);

                commandBuffer.RemoveComponent<WeaponEquipRequest>(playerEntity);

                Entity weaponEntity = commandBuffer.Instantiate(request.WeaponPrefab);
                commandBuffer.AddComponent<WeaponEquippedTag>(weaponEntity);

                // make weapon child of player
                commandBuffer.AddComponent(weaponEntity, new Parent { Value = playerEntity });
                commandBuffer.AddComponent(weaponEntity, new LocalToParent {});
                commandBuffer.AddComponent(weaponEntity, new LocalToWorld{ });
                commandBuffer.AddComponent(weaponEntity, new Translation { Value = request.WeaponSpawnOffset });

                commandBuffer.AppendToBuffer<LinkedEntityGroup>(playerEntity, weaponEntity);

                Entity eventEntity = commandBuffer.CreateEntity();
                commandBuffer.AddComponent<WeaponEquipedEvent>(eventEntity,
                    new WeaponEquipedEvent
                    {
                        PlaySound = request.PlaySound
                    });
                


            }).Schedule();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);


        //
        // dispatch events
        //
        var eventsCommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();

        Entities
            .WithoutBurst()
            .ForEach((Entity eventEntity, ref WeaponEquipedEvent eventComponent) =>
            {
                OnWeaponEquipped?.Invoke();
                eventsCommandBuffer.DestroyEntity(eventEntity);

                if (eventComponent.PlaySound) SfxPlayer.Instance.PlaySound(SoundId.WEAPON_PICKED);

            }).Run();
    }

    public struct WeaponEquipedEvent : IComponentData
    {
        public bool PlaySound;
    }
}
