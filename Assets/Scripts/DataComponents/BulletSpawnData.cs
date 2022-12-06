using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct BulletSpawnData : IComponentData
{
    public float BulletSpeed;
    public float3 SpawnOffset;
    public float AmountPerSecond;
    public float ElapsedTimeSinceLast;
}