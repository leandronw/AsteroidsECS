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
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class WrapAroundEdgesSystem : SystemBase
{


    protected override void OnUpdate()
    {
        var gameArea = GameArea.Instance;

        new WrapAroundEdgesJob()
        {
            BottomEdge = gameArea.BottomEdge,
            TopEdge = gameArea.TopEdge,
            LeftEdge = gameArea.LeftEdge,
            RightEdge = gameArea.RightEdge
        }
        .ScheduleParallel();
    }
}

[BurstCompile]
public partial struct WrapAroundEdgesJob : IJobEntity
{
    public float BottomEdge;
    public float TopEdge;
    public float LeftEdge;
    public float RightEdge;

    void Execute(ref Translation translation, in PhysicsVelocity velocity, in WrapAroundEdgesComponent wrapData)
    {
        float margin = wrapData.objectSize / 2;
        float thisBottomEdge = BottomEdge - margin;
        float thisTopEdge = TopEdge + margin;
        float thisLeftEdge = LeftEdge - margin;
        float thisRightEdge = RightEdge + margin;

        if (translation.Value.x < thisLeftEdge && velocity.Linear.x < 0)
        {
            translation.Value.x = thisRightEdge;
        }
        else if (translation.Value.x > thisRightEdge && velocity.Linear.x > 0)
        {
            translation.Value.x = thisLeftEdge;
        }

        if (translation.Value.y < thisBottomEdge && velocity.Linear.y < 0)
        {
            translation.Value.y = thisTopEdge;
        }
        else if (translation.Value.y > thisTopEdge && velocity.Linear.y > 0)
        {
            translation.Value.y = thisBottomEdge;
        }
    }
}