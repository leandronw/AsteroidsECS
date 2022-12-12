using Unity.Entities;

[GenerateAuthoringComponent]
public struct ShieldData : IComponentData 
{
    public Entity ShieldPrefab;
    public float TimeRemaining;
}