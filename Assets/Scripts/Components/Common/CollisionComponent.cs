using Unity.Entities;


public struct CollisionComponent : IComponentData 
{
    public Entity otherEntity;
}