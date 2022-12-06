using Unity.Entities;

[GenerateAuthoringComponent]
public struct AsteroidTypeData : IComponentData 
{
    public AsteroidType type;
}