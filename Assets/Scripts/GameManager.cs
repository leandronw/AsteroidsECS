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
using System.Threading.Tasks;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float _delayBeforeNextLevel;
    [SerializeField] private float _delayBeforeRespawn;

    private bool _isGameRunning;
    private bool _isGameOver;
    private uint _currentLevel;
    private uint _playerLives;
    private Entity _player1Entity;

    private World _world;
    private EntityCommandBufferSystem _ecbSystem;
    private EntityManager _entityManager;

    private EntityQuery _allGameEntitiesQuery;
    private EntityQuery _allAsteroidsQuery;
    private EntityQuery _playerCollidedQuery;
    private EntityQuery _asteroidsCollidedQuery;
    private EntityQuery _bulletsCollidedQuery;

    private Dictionary<AsteroidType, Entity> _asteroidPrefabs = new Dictionary<AsteroidType, Entity>();

    void Start()
    {
        _world = World.DefaultGameObjectInjectionWorld;
        _ecbSystem = _world.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        _entityManager = _world.EntityManager;

        _allGameEntitiesQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[] {
                typeof(PlayerTag),
                typeof(AsteroidTypeData),
                typeof(BulletTag)}
        });

        _allAsteroidsQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] {
                typeof(AsteroidTypeData)}
        });

        _playerCollidedQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] {
                typeof(PlayerTag),
                typeof(CollidedTag)}
        });


        _asteroidsCollidedQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] {
                typeof(AsteroidTypeData),
                typeof(PhysicsVelocity),
                typeof(Translation),
                typeof(CollidedTag)}
        });

        _bulletsCollidedQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] {
                typeof(BulletTag),
                typeof(Translation),
                typeof(CollidedTag)}
        });

        _asteroidPrefabs[AsteroidType.Big] = _ecbSystem.GetSingleton<AsteroidBigPrefabReference>().Prefab;
        _asteroidPrefabs[AsteroidType.Medium] = _ecbSystem.GetSingleton<AsteroidMediumPrefabReference>().Prefab;
        _asteroidPrefabs[AsteroidType.Small] = _ecbSystem.GetSingleton<AsteroidSmallPrefabReference>().Prefab;

        StartNewGame();
    }

    private void Update()
    {
        if (_isGameRunning)
        {
            CheckEntitiesCollided();
            CheckHyperspaceTravel(_player1Entity);
            CheckThrusting();
        }
        else if (_isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartNewGame();
            }
        }
    }

    private void CleanupPreviousGame()
    {
        _entityManager.DestroyEntity(_allGameEntitiesQuery);
    }

    private async void StartNewGame()
    {
        CleanupPreviousGame();

        _isGameRunning = false;
        _isGameOver = false;
        _currentLevel = 0;
        _playerLives = 3;

        // TO DO: Show countdown

        SpawnPlayer();
        StartNextLevel();
        _isGameRunning = true;
    }

    private void GameOver()
    {
        _isGameRunning = false;
        _isGameOver = true;

        // TO DO: show UI
    }

    private void SpawnPlayer()
    {
        _player1Entity = _entityManager.Instantiate(_ecbSystem.GetSingleton<PlayerPrefabReference>().Prefab);
    }

    private void PlayerDied(Entity playerEntity)
    {
        Debug.Log("Player died!");

        // TO DO: show death VFX

        _playerLives--;

        // TO DO: update UI

        if (_playerLives > 0)
        {
            RespawnPlayer();
        }
        else
        {
            GameOver();
        }
    }
    private async void RespawnPlayer()
    {
        await Task.Delay((int)_delayBeforeRespawn * 1000); // wait "_delayBeforeRespawn" seconds before respawning

        SpawnPlayer();
    }


    private async void StartNextLevel(float delay = 0f)
    {
        _currentLevel++;
        Debug.Log($"Starting level {_currentLevel}!");

        uint asteroidsAmount = _currentLevel * 2 + 2;

        await Task.Delay((int)(delay * 1000)); // wait "delay" seconds before starting

        float2 playerPosition = GetPlayerPosition(_player1Entity);

        for (int i = 0; i < asteroidsAmount; i++)
        {
            SpawnAsteroids(
                prefab: _asteroidPrefabs[AsteroidType.Big],
                amount: 1,
                position: GetRandomPositionFarFromPlayer(playerPosition, 5f),
                previousVelocity: float2.zero);
        }     
    }

    private void SpawnAsteroids(
        Entity prefab,
        uint amount,
        float2 position,
        float2 previousVelocity)
    {
        Entity spawnerEntity = _entityManager.CreateEntity();
        _entityManager.AddComponentData<AsteroidSpawnRequestData>(
            spawnerEntity,
            new AsteroidSpawnRequestData
            {
                Amount = amount,
                Position = position,
                PreviousVelocity = previousVelocity,
                Prefab = prefab
            });
    }

    private void CheckHyperspaceTravel(Entity playerEntity)
    {
        if (_entityManager.HasComponent<JumpToHyperspaceTag>(playerEntity))
        {
            DoHyperspaceTravel(playerEntity);
        }
    }

    private void DoHyperspaceTravel(Entity playerEntity)
    {
        if (!_entityManager.HasComponent<Translation>(playerEntity))
        {
            return; // player entity is not valid
        }

        Debug.Log("Player went through HYPERSPACE");

        Translation oldPosition = _entityManager.GetComponentData<Translation>(playerEntity);

        // TO DO: show VFX

        float2 newPosition = GetRandomPosition();
        _entityManager.SetComponentData<Translation>(
            playerEntity,
            new Translation
            {
                Value = new float3(newPosition.x, newPosition.y, 0f)
            });
        _entityManager.RemoveComponent(playerEntity, typeof(JumpToHyperspaceTag));

        // TO DO: show VFX
    }

    private void CheckEntitiesCollided()
    {
        CheckPlayerCollided(_player1Entity);
        CheckAsteroidsCollided();
        CheckBulletsCollided();
    }

    private void CheckPlayerCollided(Entity playerEntity)
    {
        if (_playerCollidedQuery.Matches(playerEntity))
        {
            PlayerDied(playerEntity);
        }

        _entityManager.DestroyEntity(_playerCollidedQuery);
    }

    private void CheckAsteroidsCollided()
    {
        bool newAsteroidsSpawned = false;
        bool anyAsteroidCollided = false;

        if (!_asteroidsCollidedQuery.IsEmpty)
        {
            anyAsteroidCollided = true;

            var velocities = _asteroidsCollidedQuery.ToComponentDataArray<PhysicsVelocity>(Allocator.Temp);
            var positions = _asteroidsCollidedQuery.ToComponentDataArray<Translation>(Allocator.Temp);
            var asteroidTypes = _asteroidsCollidedQuery.ToComponentDataArray<AsteroidTypeData>(Allocator.Temp);

            for (int i = 0; i < asteroidTypes.Length; i++)
            {
                AsteroidType type = asteroidTypes[i].type;
                Entity prefab = Entity.Null;
                switch (type)
                {
                    case AsteroidType.Big:
                        prefab = _asteroidPrefabs[AsteroidType.Medium];
                        break;
                    case AsteroidType.Medium:
                        prefab = _asteroidPrefabs[AsteroidType.Small];
                        break;
                    default:
                        break;
                }

                if (prefab != Entity.Null)
                {
                    newAsteroidsSpawned = true;

                    SpawnAsteroids(
                        prefab: prefab,
                        amount: (uint)UnityEngine.Random.Range(2, 4),
                        position: positions[i].Value.xy,
                        previousVelocity: velocities[i].Linear.xy);
                }
            }
        }

        // TO DO: add score
        // TO DO: show VFX

        _entityManager.DestroyEntity(_asteroidsCollidedQuery);

        if (anyAsteroidCollided && !newAsteroidsSpawned && _allAsteroidsQuery.IsEmpty)
        {
            StartNextLevel(_delayBeforeNextLevel);
        }
    }

    private void CheckBulletsCollided()
    {
        if (!_bulletsCollidedQuery.IsEmpty)
        {
            var positions = _bulletsCollidedQuery.ToComponentDataArray<Translation>(Allocator.Temp);
            // TO DO: show VFX

            _entityManager.DestroyEntity(_bulletsCollidedQuery);
        }
    }

    private void CheckThrusting()
    {
        bool isP1Thrusting = _entityManager.HasComponent<ThrustingTag>(_player1Entity);

        // TO DO: show or hide VFX
    }

    private float2 GetPlayerPosition(Entity playerEntity)
    {
        if (_entityManager.HasComponent<Translation>(playerEntity))
        {
            Translation playerPosition = _entityManager.GetComponentData<Translation>(playerEntity);
            return playerPosition.Value.xy;
        }
        else
        {
            return float2.zero;
        }
    }

    private float2 GetRandomPosition()
    {
        GameArea gameArea = GameArea.Instance;
        float2 randomPosition = new float2(
            UnityEngine.Random.Range(gameArea.LeftEdge, gameArea.RightEdge),
            UnityEngine.Random.Range(gameArea.BottomEdge, gameArea.TopEdge));

        return randomPosition;
    }

    private float2 GetRandomPositionFarFromPlayer(float2 playerPosition, float threshold)
    {
        GameArea gameArea = GameArea.Instance;
        float2 randomPosition = GetRandomPosition();
        
        if (math.distance(playerPosition, randomPosition) < threshold)
        {
            randomPosition.x += gameArea.Width * 0.5f;
        }

        return randomPosition;
    }

}
