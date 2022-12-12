using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct PlayerKeyboardComponent : IComponentData
{
    public KeyCode ShootKey;
    public KeyCode ThrustKey;
    public KeyCode HyperspaceKey;
    public KeyCode TurnLeftKey;
    public KeyCode TurnRightKey;
}
