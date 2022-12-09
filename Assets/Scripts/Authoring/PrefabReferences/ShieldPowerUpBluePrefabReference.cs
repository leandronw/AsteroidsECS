using Unity.Entities;

[GenerateAuthoringComponent]
public struct ShieldPowerUpBluePrefabReference : IComponentData
{
    public Entity Prefab;
}