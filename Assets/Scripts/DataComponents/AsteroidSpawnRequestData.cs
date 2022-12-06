using Unity.Entities;
using Unity.Mathematics;

public struct AsteroidSpawnRequestData : IComponentData
{
    public Entity Prefab;
    public float2 Position;
    public uint Amount;
}
