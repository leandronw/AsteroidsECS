using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

/*
 * Handles input for players
 * */

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(UpdateWorldTimeSystem))]
public partial class PlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities.ForEach((
            ref PhysicsVelocity velocity,
            ref Rotation rotation,
            in PlayerAccelerationData accelerationData,
            in PlayerRotationData rotationData,
            in PlayerInputData inputData) =>
            {
                if (inputData.IsTurningLeft)
                {
                    rotation.Value = math.mul(rotation.Value, quaternion.RotateZ(rotationData.Speed * deltaTime));
                }
                else if (inputData.IsTurningRight)
                {
                    rotation.Value = math.mul(rotation.Value, quaternion.RotateZ(-rotationData.Speed * deltaTime));
                }

                if (inputData.IsThrusting)
                {
                    float3 currentVelocity = velocity.Linear;
                    float3 forwardVector = math.rotate(rotation.Value, math.up());
                    float currentSpeedForward = math.dot(currentVelocity, forwardVector);
                    if (currentSpeedForward < accelerationData.MaxSpeed)
                    {
                        velocity.Linear += forwardVector * accelerationData.Acceleration * deltaTime;
                    }
                }
            })
            .Run();
    }
}
