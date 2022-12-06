using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine.Rendering;
using Unity.Jobs;
using Unity.Physics;
using Unity.Burst;

public class GameManager : MonoBehaviour
{
  
    [SerializeField] private float _worldWidth = 100f;
    [SerializeField] private float _worldHeight = 100f;

    void Start()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var ecbSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer entityCommandBuffer = ecbSystem.CreateCommandBuffer();
        Entity entity = entityCommandBuffer.Instantiate(ecbSystem.GetSingleton<PlayerPrefabReference>().Prefab);

        entity = entityCommandBuffer.CreateEntity();
        entityCommandBuffer.AddComponent(entity, new AsteroidSpawnRequestData
        {
            Amount = 3,
            Position = float2.zero,
            Prefab = ecbSystem.GetSingleton<AsteroidBigPrefabReference>().Prefab
        });
    }
}
