using Unity.Entities;
using Unity.Mathematics;

public struct AsteroidSpawnRequest : IComponentData
{
    public AsteroidSize Size;
    public float2 Position;
    public float3 PreviousVelocity;
    public uint Amount;
}
