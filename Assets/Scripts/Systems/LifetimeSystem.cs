using Unity.Entities;

/*
 * Decreases Lifetime of entities and destroys entities Lifetime reaches 0
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
        var commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        float deltaTime = Time.DeltaTime;

        Entities
            .ForEach((
                Entity entity, 
                int entityInQueryIndex, 
                ref Lifetime lifetime) =>
            {
                lifetime.Value -= deltaTime;

                if (lifetime.Value <= 0)
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }

            }).ScheduleParallel();

        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}
