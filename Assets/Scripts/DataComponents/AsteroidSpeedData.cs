using Unity.Entities;

[GenerateAuthoringComponent]
public struct AsteroidSpeedData : IComponentData
{
    public float MinSpeed;
    public float MaxSpeed;
}
