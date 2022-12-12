using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct WeaponEquipRequest : IComponentData
{
    public Entity WeaponPrefab;
    public float3 WeaponSpawnOffset;
    public bool PlaySound;
}