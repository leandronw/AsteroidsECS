using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct PlayerDestroyedEvent : IComponentData
{
    public float2 Position;
}
public struct UFODestroyedEvent : IComponentData
{
    public float2 Position;
}

public struct AsteroidDestroyedEvent : IComponentData
{
    public AsteroidSize Size;
    public float2 Position;
}

public struct ShieldEnabledEvent : IComponentData
{
    public float Duration;
}

public struct ShieldDepletedEvent : IComponentData
{
}

public struct WeaponEquipedEvent : IComponentData
{
}

public struct HyperspaceEvent : IComponentData
{
    public float2 PreviousPosition;
    public float2 NewPosition;
}

public struct SfxEvent : IComponentData 
{
    public SoundId Sound;
}

public struct SfxLoopPlayEvent : IComponentData
{
    public SoundId Sound;
}

public struct SfxLoopStopEvent : IComponentData
{
    public SoundId Sound;
}