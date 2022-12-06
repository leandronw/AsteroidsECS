using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct PlayerInputData : IComponentData
{
    public KeyCode ShootKey;
    public KeyCode ThrustKey;
    public KeyCode HyperspaceKey;
    public KeyCode TurnLeftKey;
    public KeyCode TurnRightKey;

    public bool IsShooting => Input.GetKey(ShootKey);
    public bool IsThrusting => Input.GetKey(ThrustKey);
    public bool IsJumpingToHyperspace => Input.GetKeyDown(HyperspaceKey);
    public bool IsTurningLeft => Input.GetKey(TurnLeftKey);
    public bool IsTurningRight => Input.GetKey(TurnRightKey);
}
