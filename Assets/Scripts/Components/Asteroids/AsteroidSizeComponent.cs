using Unity.Entities;

[GenerateAuthoringComponent]
public struct AsteroidSizeComponent : IComponentData 
{
    public AsteroidSize Size;
}