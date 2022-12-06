using Unity.Entities;

[GenerateAuthoringComponent]
public class BulletPrefabReference : IComponentData
{
    public Entity Prefab;
}
