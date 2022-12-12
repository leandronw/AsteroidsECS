using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;

/*
 * Handles all collision events for bullets, players, UFOs and powerups
 */
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
public partial class CollisionHandlingSystem : SystemBase
{
    private EntityCommandBufferSystem _entityCommandBufferSystem;

    protected override void OnCreate()
    {
        _entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter commandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        // player dies when colliding with anything except power-ups, 
        // so we need to know what entities are power-ups
        EntityQuery allPowerupsQuery = GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[] {
                typeof(ShieldPowerUpTag),
                typeof(WeaponPowerUpTag)},
            None = new ComponentType[] {
                typeof(PickedTag)}
        });
        NativeArray<Entity> allPowerupEntities = allPowerupsQuery.ToEntityArray(Allocator.TempJob);

        // player doesn't get hurt if shielded,
        // so we need to know which players have shield
        EntityQuery shieldedQuery = GetEntityQuery(typeof(PlayerTag), typeof(ShieldData));
        NativeArray<Entity> shieldedPlayers = shieldedQuery.ToEntityArray(Allocator.TempJob);

        // handle bullets collisions
        JobHandle bulletsJobHandle = new HandleBulletsJob()
        {
            CommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()

        }.ScheduleParallel(this.Dependency);

        // handle UFOs collisions
        JobHandle ufosJobHandle = new HandleUFOJob()
        {
            CommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()

        }.ScheduleParallel(this.Dependency);

        // handle asteroids collisions
        JobHandle asteroidsJobHandle = new HandleAsteroidJob()
        {
            CommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()

        }.ScheduleParallel(this.Dependency);

        // handle players collisions
        JobHandle playersJobHandle = new HandlePlayerJob()
        {
            AllPowerups = allPowerupEntities,
            AllShieldedPlayers = shieldedPlayers,
            CommandBuffer = _entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter()

        }.ScheduleParallel(this.Dependency);

        this.Dependency = JobHandle.CombineDependencies(this.Dependency, bulletsJobHandle);
        this.Dependency = JobHandle.CombineDependencies(this.Dependency, ufosJobHandle);
        this.Dependency = JobHandle.CombineDependencies(this.Dependency, asteroidsJobHandle);
        this.Dependency = JobHandle.CombineDependencies(this.Dependency, playersJobHandle);
        _entityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }

    [BurstCompile]
    partial struct HandleBulletsJob : IJobEntity
    {
        [WriteOnly] public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(
            Entity bulletEntity,
            [EntityInQueryIndex] int entityInQueryIndex,
            in BulletTag bulletTag,
            in CollisionComponent collision
            )
        {
            // bullets are just destroyed
            CommandBuffer.DestroyEntity(entityInQueryIndex, bulletEntity);
        }
    }

    [BurstCompile]
    [WithNone(typeof(DestroyedTag))]
    partial struct HandleUFOJob : IJobEntity
    {
        [WriteOnly] public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(
            Entity ufoEntity,
            [EntityInQueryIndex] int entityInQueryIndex,
            in UFOTag ufoTag,
            in CollisionComponent collision
            )
        {
            CommandBuffer.AddComponent<DestroyedTag>(
                entityInQueryIndex,
                ufoEntity);

            Entity soundEventEntity = CommandBuffer.CreateEntity(entityInQueryIndex);
            CommandBuffer.AddComponent<SfxEvent>(
                entityInQueryIndex,
                soundEventEntity,
                new SfxEvent
                {
                    Sound = SoundId.UFO_DIED
                });
        }
    }


    [BurstCompile]
    [WithNone(typeof(DestroyedTag))]
    partial struct HandlePlayerJob : IJobEntity
    {
        [DeallocateOnJobCompletion] public NativeArray<Entity> AllPowerups;
        [DeallocateOnJobCompletion] public NativeArray<Entity> AllShieldedPlayers;

        [WriteOnly] public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(
            Entity playerEntity,
            [EntityInQueryIndex] int entityInQueryIndex,
            in PlayerTag playerTag,
            in CollisionComponent collision,
            in Translation position
            )
        {
            bool collidedWithPowerUp = AllPowerups.Contains(collision.otherEntity);
            if (collidedWithPowerUp)
            {
                CommandBuffer.AddComponent<PickedTag>(
                       entityInQueryIndex,
                       collision.otherEntity);
            }
            else
            {
                bool isShielded = AllShieldedPlayers.Contains(playerEntity);
                if (!isShielded)
                {
                    CommandBuffer.AddComponent<DestroyedTag>(
                        entityInQueryIndex,
                        playerEntity);

                    Entity playerDestroyedEventEntity = CommandBuffer.CreateEntity(entityInQueryIndex);
                    CommandBuffer.AddComponent<PlayerDestroyedEvent>(
                        entityInQueryIndex,
                        playerDestroyedEventEntity,
                        new PlayerDestroyedEvent
                        {
                            Position = position.Value.xy
                        });

                    Entity soundEventEntity = CommandBuffer.CreateEntity(entityInQueryIndex);
                    CommandBuffer.AddComponent<SfxEvent>(
                        entityInQueryIndex,
                        soundEventEntity,
                        new SfxEvent
                        {
                            Sound = SoundId.PLAYER_DIED
                        });

                    Entity loopStopEventEntity = CommandBuffer.CreateEntity(entityInQueryIndex);
                    CommandBuffer.AddComponent<SfxLoopStopEvent>(
                        entityInQueryIndex,
                        loopStopEventEntity,
                        new SfxLoopStopEvent
                        {
                            Sound = SoundId.PLAYER_THRUST
                        });
                }
            }

            CommandBuffer.RemoveComponent<CollisionComponent>(entityInQueryIndex, playerEntity);
        }
    }

    [BurstCompile]
    partial struct HandleAsteroidJob : IJobEntity
    {
        [WriteOnly] public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute(
            Entity asteroidEntity,
            [EntityInQueryIndex] int entityInQueryIndex,
            in AsteroidSizeComponent asteroidSize,
            in CollisionComponent collision,
            in Translation position,
            in PhysicsVelocity velocity
            )
        {
            CommandBuffer.AddComponent<DestroyedTag>(entityInQueryIndex, asteroidEntity);

            Entity asteroidDestroyedEventEntity = CommandBuffer.CreateEntity(entityInQueryIndex);
            CommandBuffer.AddComponent<AsteroidDestroyedEvent>(
                entityInQueryIndex,
                asteroidDestroyedEventEntity,
                new AsteroidDestroyedEvent
                {
                    Size = asteroidSize.Size,
                    Position = position.Value.xy
                });

            Entity soundEventEntity = CommandBuffer.CreateEntity(entityInQueryIndex);
            CommandBuffer.AddComponent<SfxEvent>(
                entityInQueryIndex,
                soundEventEntity,
                new SfxEvent
                {
                    Sound = asteroidSize.Size == AsteroidSize.Big ? SoundId.ASTEROID_DESTROYED_BIG :
                            asteroidSize.Size == AsteroidSize.Medium ? SoundId.ASTEROID_DESTROYED_MEDIUM :
                                                                        SoundId.ASTEROID_DESTROYED_SMALL
                });
        }
    }
}


