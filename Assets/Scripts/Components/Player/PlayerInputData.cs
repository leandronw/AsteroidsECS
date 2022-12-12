using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct PlayerInputData : IComponentData
{
    public bool IsShooting;
    public bool IsThrusting;
    public bool IsJumpingToHyperspace;
    public bool IsTurningLeft;
    public bool IsTurningRight;
}

