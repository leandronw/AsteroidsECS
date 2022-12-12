using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

/*
 * Handles keyboard input for players
 * */

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(PlayerInputHandlingSystem))]
public partial class PlayerKeyboardInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities
            .ForEach((
                Entity playerEntity,
                ref PlayerInputData inputData,
                in PlayerKeyboardComponent keyboardData) =>
            {
                inputData.IsShooting = Input.GetKey(keyboardData.ShootKey);
                inputData.IsThrusting = Input.GetKey(keyboardData.ThrustKey);
                inputData.IsJumpingToHyperspace = Input.GetKeyDown(keyboardData.HyperspaceKey);
                inputData.IsTurningLeft = Input.GetKey(keyboardData.TurnLeftKey);
                inputData.IsTurningRight = Input.GetKey(keyboardData.TurnRightKey);

            })
            .Run();

    }

}
