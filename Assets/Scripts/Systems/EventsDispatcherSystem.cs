
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/*
 * Collects all Events from ECS and dispatches them for any listener
 */
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class EventsDispatcherSystem : SystemBase
{
    // delegates
    public event Action<float2> OnPlayerDestroyed;
    public event Action<float2> OnUFODestroyed;
    public event Action<float2, AsteroidSize> OnAsteroidDestroyed;

    public event Action<float> OnShieldEnabled;
    public event Action OnShieldDepleted;

    public event Action OnWeaponEquipped;

    public event Action<float2, float2> OnHyperspace;

    public event Action<SoundId> OnSoundPlayed;
    public event Action<SoundId> OnSoundLoopStarted;
    public event Action<SoundId> OnSoundLoopStopped;

    private EntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var eventsCommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer();

        Entities
            .WithoutBurst()
            .ForEach((Entity eventEntity, ref PlayerDestroyedEvent eventComponent) =>
            {
                OnPlayerDestroyed?.Invoke(eventComponent.Position);
                eventsCommandBuffer.DestroyEntity(eventEntity);

            }).Run();

        Entities
          .WithoutBurst()
          .ForEach((Entity eventEntity, ref UFODestroyedEvent eventComponent) =>
          {
              OnUFODestroyed?.Invoke(eventComponent.Position);
              eventsCommandBuffer.DestroyEntity(eventEntity);

          }).Run();

        Entities
           .WithoutBurst()
           .ForEach((Entity eventEntity, ref AsteroidDestroyedEvent eventComponent) =>
           {
               OnAsteroidDestroyed?.Invoke(eventComponent.Position, eventComponent.Size);
               eventsCommandBuffer.DestroyEntity(eventEntity);

           }).Run();

        Entities
            .WithoutBurst()
            .ForEach((Entity eventEntity, ref ShieldEnabledEvent eventComponent) =>
            {
                OnShieldEnabled?.Invoke(eventComponent.Duration);
                eventsCommandBuffer.DestroyEntity(eventEntity);

            }).Run();

        Entities
           .WithoutBurst()
           .ForEach((Entity eventEntity, ref ShieldDepletedEvent eventComponent) =>
           {
               OnShieldDepleted?.Invoke();
               eventsCommandBuffer.DestroyEntity(eventEntity);

           }).Run();

        Entities
           .WithoutBurst()
           .ForEach((Entity eventEntity, ref WeaponEquipedEvent eventComponent) =>
           {
               OnWeaponEquipped?.Invoke();
               eventsCommandBuffer.DestroyEntity(eventEntity);

           }).Run();

        Entities
            .WithoutBurst()
            .ForEach((Entity eventEntity, ref HyperspaceEvent eventComponent) =>
            {
                OnHyperspace?.Invoke(eventComponent.PreviousPosition, eventComponent.NewPosition);
                eventsCommandBuffer.DestroyEntity(eventEntity);

            }).Run();

        Entities
          .WithoutBurst()
          .ForEach((Entity eventEntity, ref SfxEvent eventComponent) =>
          {
              OnSoundPlayed?.Invoke(eventComponent.Sound);
              eventsCommandBuffer.DestroyEntity(eventEntity);

          }).Run();

        Entities
          .WithoutBurst()
          .ForEach((Entity eventEntity, ref SfxLoopPlayEvent eventComponent) =>
          {
              OnSoundLoopStarted?.Invoke(eventComponent.Sound);
              eventsCommandBuffer.DestroyEntity(eventEntity);

          }).Run();

        Entities
          .WithoutBurst()
          .ForEach((Entity eventEntity, ref SfxLoopStopEvent eventComponent) =>
          {
              OnSoundLoopStopped?.Invoke(eventComponent.Sound);
              eventsCommandBuffer.DestroyEntity(eventEntity);

          }).Run();
    }
}
