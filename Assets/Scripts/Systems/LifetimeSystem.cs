using Unity.Entities;

/*
 * Decreases Lifetime of entities and destroys entities with depleted Lifetime
 */
public partial class LifetimeSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        float deltaTime = Time.DeltaTime;

        Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref Lifetime lifetime) =>
            {
                lifetime.Value -= deltaTime;

                if (lifetime.Value <= 0)
                {
                    ecb.DestroyEntity(entityInQueryIndex, entity);
                }

            }).ScheduleParallel();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}
