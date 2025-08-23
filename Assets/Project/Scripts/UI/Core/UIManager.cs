using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Aeloria.Core;

namespace Aeloria.UI
{
    /// <summary>
    /// Central UI management system
    /// Manages all UI panels, coordinates UI updates, handles UI state
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if(_instance == null)
        {
                    // Unity 2023+ API - use FindFirstObjectByType
                    _instance = FindFirstObjectByType<UIManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[UIManager]");
                        _instance = go.AddComponent<UIManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // ===== UI PANELS =====
        [Header("UI Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject cardRewardPanel;

        // ===== HUD ELEMENTS =====
        [Header("HUD Elements")]
        [SerializeField] private HealthDisplay playerHealthDisplay;
        [SerializeField] private EnergyDisplay energyDisplay;
        [SerializeField] private CardHandDisplay cardHandDisplay;
        [SerializeField] private CorruptionDisplay corruptionDisplay;

        // ===== FLOATING UI =====
        [Header("Dynamic UI")]
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private GameObject floatingHealthBarPrefab;
        [SerializeField] private Transform worldSpaceUIContainer;

        // ===== UI POOLS =====
        private Dictionary<string, Queue<GameObject>> uiPools;
        private Dictionary<GameObject, GameObject> entityHealthBars;

        // ===== STATE =====
        private Stack<UIState> stateStack;
        private bool isInitialized = false;

        public enum UIState
        {
            MainMenu,
            InGame,
            Paused,
            CardSelection,
            GameOver,
            Dialogue
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeUISystem();
        }

        /// <summary>
        /// Initialize all UI subsystems and create object pools
        /// </summary>
        private void InitializeUISystem()
        {
            // Initialize collections
            uiPools = new Dictionary<string, Queue<GameObject>>();
            entityHealthBars = new Dictionary<GameObject, GameObject>();
            stateStack = new Stack<UIState>();

            // Create UI pools for performance
            CreateUIPool("DamageNumber", damageNumberPrefab, 20);
            CreateUIPool("HealthBar", floatingHealthBarPrefab, 10);

            // Find or create world space UI container
            if (worldSpaceUIContainer == null)
            {
                GameObject container = new GameObject("WorldSpaceUI");
                container.transform.SetParent(transform);
                worldSpaceUIContainer = container.transform;
            }

            // Subscribe to game events
            SubscribeToEvents();

            isInitialized = true;
            Debug.Log("UI System Initialized");
        }

        /// <summary>
        /// Create object pool for UI elements
        /// </summary>
        private void CreateUIPool(string poolName, GameObject prefab, int size)
        {
            if (prefab == null) return;

            Queue<GameObject> pool = new Queue<GameObject>();
            GameObject poolContainer = new GameObject($"Pool_{poolName}");
            poolContainer.transform.SetParent(worldSpaceUIContainer);

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, poolContainer.transform);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }

            uiPools[poolName] = pool;
        }

        /// <summary>
        /// Subscribe to all game events that affect UI
        /// </summary>
        private void SubscribeToEvents()
        {
            // Health events
            UIEvents.OnDamageReceived += ShowDamageNumber;
            UIEvents.OnHealReceived += ShowHealNumber;
            UIEvents.OnHealthChanged += UpdateHealthDisplay;

            // Game state events
            GameManager.OnGameStateChanged += OnGameStateChanged;

            // Entity events
            EventManager.StartListening("EntitySpawned", OnEntitySpawned);
            EventManager.StartListening("EntityDied", OnEntityDied);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            UIEvents.OnDamageReceived -= ShowDamageNumber;
            UIEvents.OnHealReceived -= ShowHealNumber;
            UIEvents.OnHealthChanged -= UpdateHealthDisplay;

            if (GameManager.Instance != null)
            {
                GameManager.OnGameStateChanged -= OnGameStateChanged;
            }

            EventManager.StopListening("EntitySpawned", OnEntitySpawned);
            EventManager.StopListening("EntityDied", OnEntityDied);

            UIEvents.ClearAllSubscriptions();
        }

        // ===== UI STATE MANAGEMENT =====

        /// <summary>
        /// Push new UI state onto stack
        /// </summary>
        public void PushState(UIState newState)
        {
            stateStack.Push(newState);
            UpdateUIForState(newState);
        }

        /// <summary>
        /// Pop current UI state from stack
        /// </summary>
        public void PopState()
        {
            if (stateStack.Count > 0)
            {
                stateStack.Pop();
                if (stateStack.Count > 0)
                {
                    UpdateUIForState(stateStack.Peek());
                }
            }
        }

        /// <summary>
        /// Update UI panels based on current state
        /// </summary>
        private void UpdateUIForState(UIState state)
        {
            // Hide all panels first
            if (hudPanel) hudPanel.SetActive(false);
            if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
            if (gameOverPanel) gameOverPanel.SetActive(false);
            if (cardRewardPanel) cardRewardPanel.SetActive(false);

            // Show relevant panels
            switch (state)
            {
                case UIState.InGame:
                    if (hudPanel) hudPanel.SetActive(true);
                    break;

                case UIState.Paused:
                    if (hudPanel) hudPanel.SetActive(true);
                    if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
                    break;

                case UIState.GameOver:
                    if (gameOverPanel) gameOverPanel.SetActive(true);
                    break;

                case UIState.CardSelection:
                    if (hudPanel) hudPanel.SetActive(true);
                    if (cardRewardPanel) cardRewardPanel.SetActive(true);
                    break;
            }
        }

        // ===== DAMAGE NUMBERS =====

        /// <summary>
        /// Show floating damage number at entity position
        /// </summary>
        private void ShowDamageNumber(float amount, GameObject entity)
        {
            if (entity == null || !uiPools.ContainsKey("DamageNumber")) return;

            GameObject damageNumber = GetFromPool("DamageNumber");
            if (damageNumber != null)
            {
                damageNumber.transform.position = entity.transform.position + Vector3.up;
                damageNumber.SetActive(true);

                DamageNumber dmgComponent = damageNumber.GetComponent<DamageNumber>();
                if (dmgComponent != null)
                {
                    dmgComponent.Initialize(amount, DamageNumber.DamageType.Normal);
                }
            }
        }

        /// <summary>
        /// Show floating heal number at entity position
        /// </summary>
        private void ShowHealNumber(float amount, GameObject entity)
        {
            if (entity == null || !uiPools.ContainsKey("DamageNumber")) return;

            GameObject healNumber = GetFromPool("DamageNumber");
            if (healNumber != null)
            {
                healNumber.transform.position = entity.transform.position + Vector3.up;
                healNumber.SetActive(true);

                DamageNumber dmgComponent = healNumber.GetComponent<DamageNumber>();
                if (dmgComponent != null)
                {
                    dmgComponent.Initialize(amount, DamageNumber.DamageType.Heal);
                }
            }
        }

        // ===== HEALTH BARS =====

        /// <summary>
        /// Create or update health bar for entity
        /// </summary>
        private void OnEntitySpawned(object data)
        {
            // This will be implemented when we have enemies
        }

        /// <summary>
        /// Remove health bar when entity dies
        /// </summary>
        private void OnEntityDied(object data)
        {
            // This will be implemented when we have enemies
        }

        /// <summary>
        /// Update health display (player or enemy)
        /// </summary>
        private void UpdateHealthDisplay(float current, float max, GameObject entity)
        {
            // Update player health if it's the player
            if (entity != null && entity.CompareTag(Constants.TAG_PLAYER))
            {
                if (playerHealthDisplay != null)
                {
                    playerHealthDisplay.UpdateHealth(current, max);
                }
            }

            // Update floating health bar if entity has one
            if (entityHealthBars.ContainsKey(entity))
            {
                // Update floating health bar
            }
        }

        // ===== OBJECT POOL HELPERS =====

        /// <summary>
        /// Get object from UI pool
        /// </summary>
        private GameObject GetFromPool(string poolName)
        {
            if (!uiPools.ContainsKey(poolName)) return null;

            Queue<GameObject> pool = uiPools[poolName];
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }

            // Pool empty - could expand here if needed
            Debug.LogWarning($"UI Pool {poolName} is empty!");
            return null;
        }

        /// <summary>
        /// Return object to UI pool
        /// </summary>
        public void ReturnToPool(string poolName, GameObject obj)
        {
            if (!uiPools.ContainsKey(poolName))
            {
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            uiPools[poolName].Enqueue(obj);
        }

        // ===== GAME STATE HANDLING =====

        private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
        {
            switch (newState)
            {
                case GameManager.GameState.Playing:
                    PushState(UIState.InGame);
                    break;

                case GameManager.GameState.Paused:
                    PushState(UIState.Paused);
                    break;

                case GameManager.GameState.GameOver:
                    PushState(UIState.GameOver);
                    break;
            }
        }
    }
}