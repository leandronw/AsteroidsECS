using Unity.Entities;

[GenerateAuthoringComponent]
public class AsteroidSmallPrefabReference : IComponentData
{
    public Entity Prefab;
}
