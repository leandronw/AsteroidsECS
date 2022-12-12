using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] private RectTransform _livesContainer;
    [SerializeField] private GameObject _lifePrefab;
    private LinkedList<GameObject> _lives = new LinkedList<GameObject>();

    [SerializeField] private GameObject _gameOverScreen;

    [SerializeField] private GameObject _shieldContainer;
    [SerializeField] private Image _shieldBar;
    private float _shieldTotalTime;
    private float _shieldRemainingTime;

    [SerializeField] private Countdown _countdown;

    void Start()
    {
        // register to events
        World world = World.DefaultGameObjectInjectionWorld;
        EventsDispatcherSystem eventsSystem = world.GetOrCreateSystem<EventsDispatcherSystem>();
        eventsSystem.OnShieldDepleted += HideShield;
        eventsSystem.OnShieldEnabled += ShowShield;

        GameManager gameManager = GameManager.Instance;
        gameManager.OnCountdownStarted += ShowCountdown;
        gameManager.OnLivesChanged += UpdateLives;
        gameManager.OnGameEnded += ShowGameOver;

        HideGameOver();
        HideShield();
    }

    void Update()
    {
        // update shield
        if (_shieldContainer.activeSelf)
        {
            _shieldRemainingTime -= Time.deltaTime;
            if (_shieldRemainingTime <= 0f)
            {
                HideShield();
            }
            else
            {
                _shieldBar.fillAmount = _shieldRemainingTime / _shieldTotalTime;
            }
        }
    }

    private void UpdateLives(int newValue)
    {
        if (_lives.Count < newValue)
        {
            AddLives(newValue - _lives.Count);
        }
        else if (_lives.Count > newValue)
        {
            RemoveLives(_lives.Count - newValue);
        }
    }

    private void AddLives(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject newLife = GameObject.Instantiate(_lifePrefab, _livesContainer);
            _lives.AddLast(newLife);
        }
    }

    private void RemoveLives(int amount)
    {
        if (_lives.Count < amount) // invalid call
            return;

        for (int i = 0; i < amount; i++)
        {
            GameObject.Destroy(_lives.Last.Value);
            _lives.RemoveLast();
        }
    }

    private void ShowShield(float totalTime)
    {
        _shieldContainer.SetActive(true);
        _shieldTotalTime = totalTime;
        _shieldRemainingTime = totalTime;
        _shieldBar.fillAmount = 1f;
    }

    private void HideShield()
    {
        _shieldContainer.SetActive(false);
    }

    private void ShowGameOver()
    {
        _gameOverScreen.SetActive(true);
    }

    private void HideGameOver()
    {
        _gameOverScreen.SetActive(false);
    }

    private void ShowCountdown(float time)
    {
        HideGameOver();

        _countdown.Play(time);
    }
 
}
