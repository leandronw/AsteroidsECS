using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

/*
 * Handles game rules, collisions and spawning of entities
 */
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        INIT,
        STARTING_GAME,
        STARTING_LEVEL,
        RUNNING,
        GAME_OVER,
        DESTROYED,
    }


    // Delegates
    public event Action OnGameStarted;
    public event Action OnGameEnded;
    public event Action<int> OnLivesChanged;
    public event Action<float> OnCountdownStarted;

    // Inspector variables
    [SerializeField] private float _countdownTime;
    [SerializeField] private float _delayBeforeNextLevel;
    [SerializeField] private float _delayBeforeRespawn;
    [SerializeField] private float _minUfoInterval;
    [SerializeField] private float _maxUfoInterval;
    [SerializeField] private float _ufoIntervalDecrease;

    // Game logic
    private GameState _state = GameState.INIT;
    private int _currentLevel;
    private int _playerLives;
    private float _currentUfoInterval;
    private float _currentUfoTimer;

    // ECS references
    private Entity _player1Entity;
    private World _world;
    private BeginInitializationEntityCommandBufferSystem _entityCommandBufferSystem;
    private EntityManager _entityManager;

    // EntityQueries
    private EntityQuery _allGameEntitiesQuery;
    private EntityQuery _anyEnemiesLeftQuery;

    // Prefabs
    private Entity _playerPrefab;
    private Entity _ufoPrefab;
    private List<Entity> _powerUpPrefabs = new List<Entity>();

    // Config
    private PlayerKeyboardData _player1KeyboardConfig;


    void Start()
    {
        _world = World.DefaultGameObjectInjectionWorld;
        _entityCommandBufferSystem = _world.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        _entityManager = _world.EntityManager;

        _allGameEntitiesQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[] {
                typeof(PlayerTag),
                typeof(AsteroidSizeComponent),
                typeof(BulletTag),
                typeof(ShieldPowerUpTag),
                typeof(WeaponPowerUpTag),
                typeof(UFOTag)}
        });

        _anyEnemiesLeftQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[] {
                typeof(AsteroidSizeComponent),
                typeof(AsteroidSpawnRequest),
                typeof(UFOTag)}
        });

        _playerPrefab = _entityCommandBufferSystem.GetSingleton<PlayerPrefabReference>().Prefab;
        _ufoPrefab = _entityCommandBufferSystem.GetSingleton<UFOPrefabReference>().Prefab;
        _powerUpPrefabs.Add(_entityCommandBufferSystem.GetSingleton<ShieldPowerUpBluePrefabReference>().Prefab);
        _powerUpPrefabs.Add(_entityCommandBufferSystem.GetSingleton<ShieldPowerUpRedPrefabReference>().Prefab);
        _powerUpPrefabs.Add(_entityCommandBufferSystem.GetSingleton<ShieldPowerUpYellowPrefabReference>().Prefab);
        _powerUpPrefabs.Add(_entityCommandBufferSystem.GetSingleton<ShieldPowerUpGreenPrefabReference>().Prefab);
        _powerUpPrefabs.Add(_entityCommandBufferSystem.GetSingleton<WeaponPowerUpBluePrefabReference>().Prefab);
        _powerUpPrefabs.Add(_entityCommandBufferSystem.GetSingleton<WeaponPowerUpRedPrefabReference>().Prefab);
        _powerUpPrefabs.Add(_entityCommandBufferSystem.GetSingleton<WeaponPowerUpYellowPrefabReference>().Prefab);
        _powerUpPrefabs.Add(_entityCommandBufferSystem.GetSingleton<WeaponPowerUpGreenPrefabReference>().Prefab);


        CollisionHandlingSystem collisionsSystem = _world.GetOrCreateSystem<CollisionHandlingSystem>();
        collisionsSystem.OnPlayerDestroyed += PlayerDied;
        collisionsSystem.OnUFODestroyed += UFODestroyed;
        collisionsSystem.OnPowerUpPicked += PowerUpPicked;
        collisionsSystem.OnAsteroidDestroyed += AsteroidDestroyed;

        HyperspaceSystem hyperspaceSystem = _world.GetOrCreateSystem<HyperspaceSystem>();
        hyperspaceSystem.OnHyperspace += JumpedIntoHyperspace;

        var player1KeyboardConfigEntity = _entityCommandBufferSystem.GetSingletonEntity<Player1KeyboardConfigTag>();
        _player1KeyboardConfig = _entityManager.GetComponentData<PlayerKeyboardData>(player1KeyboardConfigEntity);
    }

    private void Update()
    {
        switch (_state)
        {
            case GameState.INIT: 
                StartNewGame();
                break;
            case GameState.RUNNING:
                CheckSpawnUFO();
                CheckLevelCompleted();
                break;
            case GameState.GAME_OVER:
                if (Input.GetKeyDown(KeyCode.Space)) // restart by pressing spacebar
                {
                    StartNewGame();
                }
                break;
        }
    }

    private void OnDestroy()
    {
        _state = GameState.DESTROYED;
    }

    private void CleanupPreviousGame()
    {
        _entityManager.DestroyEntity(_allGameEntitiesQuery);
    }

    private async void StartNewGame()
    {
        CleanupPreviousGame();

        _state = GameState.STARTING_GAME;
        _currentLevel = 0;
        _playerLives = 3;

        _currentUfoInterval = _maxUfoInterval;
        _currentUfoTimer = 0f;

        OnLivesChanged?.Invoke(_playerLives);

        OnCountdownStarted?.Invoke(_countdownTime);
        await Task.Delay((int)(_countdownTime * 1000f));

        if (_state != GameState.STARTING_GAME)
            return;


        OnLivesChanged?.Invoke(_playerLives - 1);
        SpawnPlayer();
        await StartNextLevel();

        _state = GameState.RUNNING;

        OnGameStarted?.Invoke();
    }

    private void GameOver()
    {
        _state = GameState.GAME_OVER;

        OnGameEnded?.Invoke();

        Debug.Log("GAME OVER :(");
    }

    private void SpawnPlayer()
    {
        _player1Entity = _entityManager.Instantiate(_playerPrefab);
        _entityManager.AddComponentData<PlayerKeyboardData>(_player1Entity, _player1KeyboardConfig);
        
        // dynamic buffer for linking children so they destroy when player dies 
        _entityManager.AddBuffer<LinkedEntityGroup>(_player1Entity); 
    }

    private void SpawnUFO()
    {
        Entity ufoEntity = _entityManager.Instantiate(_ufoPrefab);

        bool fromLeftToRight = UnityEngine.Random.value < 0.5f;
        float yPosition = UnityEngine.Random.Range(GameArea.Instance.BottomEdge, GameArea.Instance.TopEdge);
        float speed = _entityManager.GetComponentData<UFOSpeedData>(_ufoPrefab).Speed;

        float3 position = new float3(
            fromLeftToRight ? GameArea.Instance.LeftEdge : GameArea.Instance.RightEdge,
            yPosition,
            0f);

        _entityManager.AddComponentData<Translation>(
            ufoEntity, 
            new Translation
            {
                Value = position
            });

        _entityManager.AddComponentData<PhysicsVelocity>(
            ufoEntity,
            new PhysicsVelocity
            {
                Linear = new float3(
                    fromLeftToRight ? speed : -speed,
                    0f, 0f),
                Angular = new float3(0f, 0f, 1f)
            });
    }

    private void SpawnPowerUp()
    {
        if (_powerUpPrefabs.Count == 0)
        {
            Debug.LogError("PowerUp prefabs not found");
            return;
        }

        Entity randomPrefab = _powerUpPrefabs[UnityEngine.Random.Range(0, _powerUpPrefabs.Count)];
        float2 randomPosition = GameArea.GetRandomPosition();
 
        Entity powerUpEntity = _entityManager.Instantiate(randomPrefab);
        _entityManager.SetComponentData<Translation>(
            powerUpEntity,
            new Translation
            {
                Value = new float3(randomPosition.x, randomPosition.y, 0f)
            });
    }

    private void PowerUpPicked(float2 position)
    {
        Debug.Log("Powerup picked!");
        // TO DO: show VFX
    }

    private void UFODestroyed(float2 position)
    {
        // TO DO: add score
        // TO DO: show VFX
    }


    private void AsteroidDestroyed(float2 position, AsteroidSize size)
    {
        // TO DO: add score
        // TO DO: show VFX
    }

    private void PlayerDied(float2 position)
    {
        Debug.Log("Player died!");

        // TO DO: show death VFX

        if (_playerLives > 1)
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
        await Task.Delay((int)(_delayBeforeRespawn * 1000)); // wait "_delayBeforeRespawn" seconds before respawning

        if (_state == GameState.DESTROYED) 
            return;

        _playerLives--;
        OnLivesChanged?.Invoke(_playerLives - 1);

        SpawnPlayer();
    }

  
    private async Task StartNextLevel(float delay = 0f)
    {
        _state = GameState.STARTING_LEVEL;
        _currentLevel++;

        Debug.Log($"Starting level {_currentLevel}!");

        int asteroidsAmount = _currentLevel * 2 + 2;
        int powerUpsAmount = _currentLevel + 1;

        await Task.Delay((int)(delay * 1000)); // wait "delay" seconds before starting

        if (_state != GameState.STARTING_LEVEL)
            return;

        float2 playerPosition = GetPlayerPosition(_player1Entity);

        for (int i = 0; i < asteroidsAmount; i++)
        {
            SpawnAsteroid(position: GameArea.GetRandomPositionFarFromPlayer(playerPosition, 5f));
        }

        for (int i = 0; i < powerUpsAmount; i++)
        {
            SpawnPowerUp();
        }
    }

    private void SpawnAsteroid(float2 position)
    {
        Entity spawnerEntity = _entityManager.CreateEntity();
        _entityManager.AddComponentData<AsteroidSpawnRequest>(
            spawnerEntity,
            new AsteroidSpawnRequest
            {
                Amount = 1,
                Position = position,
                PreviousVelocity = float3.zero,
                Size = AsteroidSize.Big
            });
    }


    private void JumpedIntoHyperspace(float2 previousPosition, float2 newPosition)
    {
        Debug.Log("Player went through HYPERSPACE");

        // TO DO: show VFX
    }

    private void CheckSpawnUFO()
    {
        _currentUfoTimer += Time.deltaTime;
        if (_currentUfoTimer >= _currentUfoInterval)
        {
            _currentUfoInterval -= _ufoIntervalDecrease;
            if (_currentUfoInterval < _minUfoInterval) { _currentUfoInterval = _minUfoInterval; }
            _currentUfoTimer = 0f;
            SpawnUFO();
        }
    }

    private async void CheckLevelCompleted()
    {
        if (_anyEnemiesLeftQuery.IsEmpty)
        {
            await StartNextLevel(_delayBeforeNextLevel);

            _state = GameState.RUNNING;
        }
    }

    private float2 GetPlayerPosition(Entity playerEntity)
    {
        if (_entityManager.Exists(playerEntity) &&
            _entityManager.HasComponent<Translation>(playerEntity))
        {
            Translation playerPosition = _entityManager.GetComponentData<Translation>(playerEntity);
            return playerPosition.Value.xy;
        }
        else
        {
            return float2.zero;
        }
    }
}
