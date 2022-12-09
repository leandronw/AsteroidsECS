using Unity.Entities;

[GenerateAuthoringComponent]
public struct WeaponPowerUpBluePrefabReference : IComponentData
{
    public Entity Prefab;
}