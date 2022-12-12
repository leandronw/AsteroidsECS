using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct UFORotatingWeaponData : IComponentData
{
    public Entity BulletPrefab;
    public float RotationPerShot;
    public float LastShotRotation;
    public float BulletSpeed;
    public float BulletsPerSecond;
    public float ElapsedTimeSinceLastShot;
}