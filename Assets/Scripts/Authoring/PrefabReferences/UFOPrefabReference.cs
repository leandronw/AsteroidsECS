using Unity.Entities;

[GenerateAuthoringComponent]
public class UFOPrefabReference : IComponentData
{
    public Entity Prefab;
}
