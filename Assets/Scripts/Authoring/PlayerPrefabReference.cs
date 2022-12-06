using Unity.Entities;

[GenerateAuthoringComponent]
public class PlayerPrefabReference : IComponentData
{
    public Entity Prefab;
}
