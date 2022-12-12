using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct PlayerKeyboardData : IComponentData
{
    public KeyCode ShootKey;
    public KeyCode ThrustKey;
    public KeyCode HyperspaceKey;
    public KeyCode TurnLeftKey;
    public KeyCode TurnRightKey;
}
