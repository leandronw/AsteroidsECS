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
//[UpdateAfter(typeof(Physics))]
public partial class WrapAroundEdgesSystem : SystemBase
{
    private EntityQuery _query;
    private float _bottomEdge;
    private float _topEdge;
    private float _leftEdge;
    private float _rightEdge;

    protected override void OnCreate()
    {
        Camera camera = Camera.main;

        Vector3 worldBottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 worldTopRight = camera.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));

        float worldWidth = worldTopRight.x - worldBottomLeft.x;
        float worldHeight = worldTopRight.y - worldBottomLeft.y;

        _bottomEdge = -worldHeight / 2;
        _topEdge = worldHeight / 2;
        _leftEdge = -worldWidth / 2;
        _rightEdge = worldWidth / 2;
    }

    protected override void OnStartRunning()
    {
        _query = EntityManager.CreateEntityQuery(
            ComponentType.ReadWrite<Translation>(),
            ComponentType.ReadOnly<WrapAroundEdgesTag>(),
            ComponentType.ReadOnly<PhysicsVelocity>());
    }

    protected override void OnUpdate()
    {
        new WrapAroundEdgesJob()
        {
            BottomEdge = _bottomEdge,
            TopEdge = _topEdge,
            LeftEdge = _leftEdge,
            RightEdge = _rightEdge
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