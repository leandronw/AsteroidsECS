using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct WeaponData : IComponentData
{
    public Entity BulletPrefab;
    public float3 BulletSpawnOffset;
    public float BulletSpeed;
    public float BulletsPerSecond;
    public float ElapsedTimeSinceLastShot;
}