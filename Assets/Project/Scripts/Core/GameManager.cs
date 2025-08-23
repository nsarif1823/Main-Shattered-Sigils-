using UnityEngine;
using System;

namespace Aeloria.Core
{
    public class GameManager : MonoBehaviour
    {
        // Singleton instance
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[GameManager]");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Game state
        public enum GameState
        {
            MainMenu,
            Playing,
            Paused,
            GameOver
        }

        private GameState _currentState = GameState.MainMenu;
        public GameState CurrentState
        {
            get => _currentState;
            private set
            {
                GameState previousState = _currentState;
                _currentState = value;
                OnGameStateChanged?.Invoke(previousState, _currentState);
                Debug.Log($"Game State Changed: {previousState} → {_currentState}");
            }
        }

        // Events
        public static event Action<GameState, GameState> OnGameStateChanged;

        // Core systems references (will add later)
        // public DeckManager DeckManager { get; private set; }
        // public CombatManager CombatManager { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGame();
        }

        private void InitializeGame()
        {
            Debug.Log("=== Ashes of Aeloria Initializing ===");
            Application.targetFrameRate = 60;

            // Initialize subsystems (we'll add these later)
            InitializeEventManager();
            InitializeObjectPooler(); 
        }

        private void InitializeEventManager()
        {
            // Ensure EventManager exists
            if (EventManager.Instance == null)
            {
                GameObject eventManager = new GameObject("[EventManager]");
                eventManager.AddComponent<EventManager>();
                eventManager.transform.SetParent(transform);
            }
        }

        // Public methods for game flow
        public void StartGame()
        {
            CurrentState = GameState.Playing;
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Paused;
                Time.timeScale = 0f;
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Playing;
                Time.timeScale = 1f;
            }
        }

        public void GameOver()
        {
            CurrentState = GameState.GameOver;
        }

        // Debug helpers
        private void Update()
        {
            // Debug keys for testing
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log($"Current State: {CurrentState}");
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        private void InitializeObjectPooler()
        {
            // Ensure ObjectPooler exists
            if (ObjectPooler.Instance == null)
            {
                GameObject pooler = new GameObject("[ObjectPooler]");
                pooler.AddComponent<ObjectPooler>();
                pooler.transform.SetParent(transform);
            }
        }
    }

}