using Unity.Entities;

[GenerateAuthoringComponent]
public struct WeaponPowerUpRedPrefabReference : IComponentData
{
    public Entity Prefab;
}