using Unity.Entities;
using Unity.Mathematics;

public struct AsteroidSpawnRequestData : IComponentData
{
    public Entity Prefab;
    public float2 Position;
    public float2 PreviousVelocity;
    public uint Amount;
}
