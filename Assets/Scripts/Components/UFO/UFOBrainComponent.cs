using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct UFOBrainComponent: IComponentData
{
    public float MinDistanceToCheckObstacle;
    public float MinTimeSinceLastChange;
    public float ElapsedTimeSinceLastChange;
}