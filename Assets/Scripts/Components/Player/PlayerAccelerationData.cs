using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct PlayerAccelerationData : IComponentData
{
    public float Acceleration;
    public float MaxSpeed;
}
