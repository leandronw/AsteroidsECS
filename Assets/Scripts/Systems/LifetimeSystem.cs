using Unity.Entities;

/*
 * Decreases Lifetime of entities and destroys them when it reaches 0
 */
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class LifetimeSystem : SystemBase
{
    private EntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        float deltaTime = Time.DeltaTime;

        Entities
            .WithNone<Prefab>()
            .ForEach((
                Entity entity, 
                int entityInQueryIndex, 
                ref LifetimeComponent lifetime) =>
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
