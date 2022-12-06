using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct PlayerRotationData : IComponentData
{
    public float Speed;
}
