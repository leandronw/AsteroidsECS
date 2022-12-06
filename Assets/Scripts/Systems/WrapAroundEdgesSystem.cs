using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

/**
 * Handles teleporting entities to the other side when they leave the screen
 * */
public partial class WrapAroundEdgesSystem : SystemBase
{
    private EntityQuery _query;

    private const float MARGIN = 0.5f;

    protected override void OnCreate()
    {
        _query = EntityManager.CreateEntityQuery(
            ComponentType.ReadWrite<Translation>(),
            ComponentType.ReadOnly<WrapAroundEdgesTag>(),
            ComponentType.ReadOnly<PhysicsVelocity>());
    }

    protected override void OnUpdate()
    {
        var gameArea = GameArea.Instance;

        new WrapAroundEdgesJob()
        {
            BottomEdge = gameArea.BottomEdge - MARGIN,
            TopEdge = gameArea.TopEdge + MARGIN,
            LeftEdge = gameArea.LeftEdge - MARGIN,
            RightEdge = gameArea.RightEdge + MARGIN
        }
        .ScheduleParallel(_query);
    }
}

[BurstCompile]
public partial struct WrapAroundEdgesJob : IJobEntity
{
    public float BottomEdge;
    public float TopEdge;
    public float LeftEdge;
    public float RightEdge;

    void Execute(ref Translation translation, in PhysicsVelocity velocity)
    {
        if (translation.Value.x < LeftEdge && velocity.Linear.x < 0)
        {
            translation.Value.x = RightEdge;
        }
        else if (translation.Value.x > RightEdge && velocity.Linear.x > 0)
        {
            translation.Value.x = LeftEdge;
        }

        if (translation.Value.y < BottomEdge && velocity.Linear.y < 0)
        {
            translation.Value.y = TopEdge;
        }
        else if (translation.Value.y > TopEdge && velocity.Linear.y > 0)
        {
            translation.Value.y = BottomEdge;
        }
    }
}