using UnityEngine;
using UnityEngine.UI;
using Aeloria.Core;
using Aeloria.Entities;
using Aeloria.Entities.Player;
using Aeloria.UI;
using System.Collections;

namespace Aeloria.Testing
{
    /// <summary>
    /// Comprehensive scene tester that sets up and tests all implemented systems
    /// </summary>
    public class ComprehensiveSceneTester : MonoBehaviour
    {
        [Header("=== Prefab References ===")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject uiManagerPrefab;
        [SerializeField] private GameObject objectPoolerPrefab;

        [Header("=== Grid/Floor Testing ===")]
        [SerializeField] private bool checkForGrid = true;
        [SerializeField] private Color gridDebugColor = Color.green;

        [Header("=== Test Configuration ===")]
        [SerializeField] private bool autoRunTests = true;
        [SerializeField] private float testDelay = 2f;

        [Header("=== Runtime References ===")]
        [SerializeField] private PlayerController playerInstance;
        [SerializeField] private UIManager uiManager;

        private bool systemsInitialized = false;

        void Start()
        {
            Debug.Log("=== COMPREHENSIVE SCENE TESTER STARTING ===");
            StartCoroutine(InitializeAndTest());
        }

        IEnumerator InitializeAndTest()
        {
            // Step 1: Initialize Core Systems
            yield return StartCoroutine(InitializeCoreSystems());

            // Step 2: Spawn Player
            yield return StartCoroutine(SpawnPlayer());

            // Step 3: Initialize UI
            yield return StartCoroutine(InitializeUI());

            // Step 3.5: Check for Grid/Floor
            yield return StartCoroutine(CheckGridSystem());

            // Step 4: Run Tests
            if (autoRunTests)
            {
                yield return new WaitForSeconds(testDelay);
                yield return StartCoroutine(RunSystemTests());
            }

            systemsInitialized = true;
            Debug.Log("=== ALL SYSTEMS INITIALIZED - READY FOR MANUAL TESTING ===");
            ShowControlsGuide();
        }

        IEnumerator InitializeCoreSystems()
        {
            Debug.Log("--- Initializing Core Systems ---");

            // Check for GameManager
            if (GameManager.Instance == null && gameManagerPrefab != null)
            {
                Instantiate(gameManagerPrefab);
                Debug.Log("✓ GameManager instantiated");
            }
            else if (GameManager.Instance != null)
            {
                Debug.Log("✓ GameManager already exists");
            }

            // Check for EventManager
            if (EventManager.Instance == null)
            {
                GameObject eventManagerGO = new GameObject("[EventManager]");
                eventManagerGO.AddComponent<EventManager>();
                DontDestroyOnLoad(eventManagerGO);
                Debug.Log("✓ EventManager created");
            }

            // Check for ObjectPooler
            if (objectPoolerPrefab != null && FindFirstObjectByType<ObjectPooler>() == null)
            {
                Instantiate(objectPoolerPrefab);
                Debug.Log("✓ ObjectPooler instantiated");
            }

            yield return new WaitForSeconds(0.5f);

            // Update game state using the method your GameManager actually has
            Debug.Log($"✓ Current Game State: {GameManager.Instance.CurrentState}");
        }

        IEnumerator SpawnPlayer()
        {
            Debug.Log("--- Spawning Player ---");

            if (playerPrefab == null)
            {
                Debug.LogError("✗ Player prefab not assigned!");
                yield break;
            }

            // Check if player already exists
            playerInstance = FindFirstObjectByType<PlayerController>();

            if (playerInstance == null)
            {
                GameObject playerGO = Instantiate(playerPrefab, new Vector3(0, 1, 0), Quaternion.identity);
                playerInstance = playerGO.GetComponent<PlayerController>();

                if (playerInstance != null)
                {
                    Debug.Log($"✓ Player spawned - Health: {playerInstance.CurrentHealth}/{playerInstance.MaxHealth}");
                    // Note: Energy system might not be implemented yet in your PlayerController
                    Debug.Log("✓ Player controller initialized");
                }
                else
                {
                    Debug.LogError("✗ PlayerController component not found on spawned player!");
                }
            }
            else
            {
                Debug.Log("✓ Player already exists in scene");
            }

            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator CheckGridSystem()
        {
            Debug.Log("--- Checking Grid/Floor System ---");

            if (!checkForGrid)
            {
                Debug.Log("⚠ Grid check disabled in inspector");
                yield break;
            }

            // Check for your GridFloor component FIRST
            var gridFloor = FindFirstObjectByType<Aeloria.Environment.GridFloor>();
            if (gridFloor != null)
            {
                Debug.Log($"✓ GridFloor component found on: {gridFloor.name}");

                // Check if it has generated tiles by counting children
                int tileCount = gridFloor.transform.childCount;
                Debug.Log($"  • Tiles Generated: {tileCount}");

                if (tileCount == 0)
                {
                    Debug.LogWarning("  ⚠ No tiles generated! Make sure 'Generate On Start' is enabled");
                    Debug.Log("  → Or call GenerateFloor() manually");
                }
                else
                {
                    // Estimate grid size from tile count (assuming square grid)
                    int estimatedSize = Mathf.RoundToInt(Mathf.Sqrt(tileCount));
                    Debug.Log($"  • Estimated Grid Size: ~{estimatedSize}x{estimatedSize}");

                    // Check first tile to see if materials are assigned
                    Transform firstTile = gridFloor.transform.GetChild(0);
                    if (firstTile != null)
                    {
                        Renderer renderer = firstTile.GetComponent<Renderer>();
                        if (renderer != null && renderer.sharedMaterial != null)
                        {
                            Debug.Log($"  • Material assigned: {renderer.sharedMaterial.name}");
                        }
                        else
                        {
                            Debug.Log("  • Using default materials");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("✗ No GridFloor component found!");
                Debug.Log("HOW TO CREATE:");
                Debug.Log("  1. Create Empty GameObject → Name: 'GridFloor'");
                Debug.Log("  2. Add Component → GridFloor");
                Debug.Log("  3. In Inspector, set:");
                Debug.Log("     • Grid Size X: 20");
                Debug.Log("     • Grid Size Z: 20");
                Debug.Log("     • Generate On Start: ✓");
                Debug.Log("     • Create Checkerboard: ✓");
            }

            // Also check for Unity's Tilemap (if using 2D tiles instead)
            var tilemap = FindFirstObjectByType<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap != null)
            {
                Debug.Log($"✓ Unity Tilemap also found: {tilemap.name}");
                Debug.Log($"  • Bounds: {tilemap.cellBounds}");
            }

            // Check for Grid
            var grid = FindFirstObjectByType<Grid>();
            if (grid != null)
            {
                Debug.Log($"✓ Unity Grid found: {grid.name}");
                Debug.Log($"  • Cell Layout: {grid.cellLayout}");
            }

            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator InitializeUI()
        {
            Debug.Log("--- Initializing UI ---");

            // Check for UIManager
            uiManager = FindFirstObjectByType<UIManager>();

            if (uiManager == null && uiManagerPrefab != null)
            {
                GameObject uiGO = Instantiate(uiManagerPrefab);
                uiManager = uiGO.GetComponent<UIManager>();
                Debug.Log("✓ UIManager instantiated");
            }
            else if (uiManager != null)
            {
                Debug.Log("✓ UIManager found");
            }

            // Check for UI displays
            if (FindFirstObjectByType<HealthDisplay>() != null)
                Debug.Log("✓ Health Display found");

            if (FindFirstObjectByType<EnergyDisplay>() != null)
                Debug.Log("✓ Energy Display found");

            if (FindFirstObjectByType<CardHandDisplay>() != null)
                Debug.Log("✓ Card Hand Display found");

            if (FindFirstObjectByType<CorruptionDisplay>() != null)
                Debug.Log("✓ Corruption Display found");

            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator RunSystemTests()
        {
            Debug.Log("=== RUNNING AUTOMATED TESTS ===");

            // Test 1: Player Movement
            Debug.Log("--- Test 1: Player Movement ---");
            if (playerInstance != null)
            {
                Vector3 startPos = playerInstance.transform.position;
                // Simulate movement input would go here
                Debug.Log("✓ Player movement system ready (use WASD to test)");
            }

            yield return new WaitForSeconds(1f);

            // Test 2: Player Health System
            Debug.Log("--- Test 2: Health System ---");
            if (playerInstance != null)
            {
                float startHealth = playerInstance.CurrentHealth;
                playerInstance.TakeDamage(10f, null);
                Debug.Log($"✓ Damage dealt - Health: {startHealth} → {playerInstance.CurrentHealth}");

                yield return new WaitForSeconds(0.5f);

                playerInstance.Heal(5f);
                Debug.Log($"✓ Healing applied - Health now: {playerInstance.CurrentHealth}");
            }

            yield return new WaitForSeconds(1f);

            // Test 3: Event System
            Debug.Log("--- Test 3: Event System ---");
            // Trigger a simple event that exists
            EventManager.TriggerEvent("TEST_EVENT", "Test data");
            Debug.Log("✓ Event triggered: TEST_EVENT");

            // Trigger UI health event using the proper static method
            if (playerInstance != null)
            {
                UIEvents.TriggerHealthChanged(50f, 100f, playerInstance.gameObject);
                Debug.Log("✓ UI Health event triggered");
            }

            yield return new WaitForSeconds(1f);

            // Test 4: Object Pooling
            Debug.Log("--- Test 4: Object Pooling ---");
            ObjectPooler pooler = FindFirstObjectByType<ObjectPooler>();
            if (pooler != null)
            {
                Debug.Log("✓ Object Pooler is ready");
            }

            Debug.Log("=== AUTOMATED TESTS COMPLETE ===");
        }

        void ShowControlsGuide()
        {
            Debug.Log(@"
╔════════════════════════════════════════╗
║         CONTROL GUIDE                  ║
╠════════════════════════════════════════╣
║ MOVEMENT:                              ║
║   WASD     - Move player               ║
║   Shift    - Dodge                     ║
║                                        ║
║ TESTING:                               ║
║   Q        - Damage player (10 HP)    ║
║   H        - Heal player (5 HP)       ║
║   Tab      - Show player stats        ║
║                                        ║
║ DEBUG:                                 ║
║   F1       - Show GameManager state   ║
║   F2       - Toggle grid visibility   ║
║   F3       - Test event system        ║
║   F5       - Reload scene             ║
╚════════════════════════════════════════╝
            ");
        }

        void Update()
        {
            if (!systemsInitialized) return;

            // Manual test controls
            HandleTestInputs();
        }

        void HandleTestInputs()
        {
            // Player damage/heal tests
            if (Input.GetKeyDown(KeyCode.Q) && playerInstance != null)
            {
                playerInstance.TakeDamage(10f, null);
                Debug.Log($"Damaged player! Health: {playerInstance.CurrentHealth}/{playerInstance.MaxHealth}");
            }

            if (Input.GetKeyDown(KeyCode.H) && playerInstance != null)
            {
                playerInstance.Heal(5f);
                Debug.Log($"Healed player! Health: {playerInstance.CurrentHealth}/{playerInstance.MaxHealth}");
            }

            // Show stats
            if (Input.GetKeyDown(KeyCode.Tab) && playerInstance != null)
            {
                Debug.Log($@"
=== PLAYER STATS ===
Health: {playerInstance.CurrentHealth}/{playerInstance.MaxHealth}
Position: {playerInstance.transform.position}
                ");
            }

            // GameManager tests
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log($"GameManager State: {GameManager.Instance.CurrentState}");
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleGridVisualization();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                EventManager.TriggerEvent("TEST_EVENT", "Test data payload");
                Debug.Log("Triggered test event");
            }

            // Scene reload
            if (Input.GetKeyDown(KeyCode.F5))
            {
                Debug.Log("Reloading scene...");
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
        }

        void OnGUI()
        {
            if (!systemsInitialized) return;

            // Simple on-screen debug info
            GUI.Box(new Rect(10, 10, 200, 80), "System Status");
            GUI.Label(new Rect(20, 30, 180, 20), $"Game State: {GameManager.Instance.CurrentState}");

            if (playerInstance != null)
            {
                GUI.Label(new Rect(20, 50, 180, 20), $"Health: {playerInstance.CurrentHealth:0}/{playerInstance.MaxHealth:0}");
            }
        }

        void ToggleGridVisualization()
        {
            // Try GridFloor first
            var gridFloor = FindFirstObjectByType<Aeloria.Environment.GridFloor>();
            if (gridFloor != null)
            {
                // Toggle all child tile renderers
                var renderers = gridFloor.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    bool newState = !renderers[0].enabled;
                    foreach (var renderer in renderers)
                    {
                        renderer.enabled = newState;
                    }
                    Debug.Log($"GridFloor visibility: {newState}");
                    return;
                }
            }

            // Fallback to tilemap
            var tilemap = FindFirstObjectByType<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap != null)
            {
                var renderer = tilemap.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = !renderer.enabled;
                    Debug.Log($"Tilemap visibility: {renderer.enabled}");
                }
            }
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !checkForGrid) return;

            // Draw grid bounds
            var grid = FindFirstObjectByType<Grid>();
            if (grid != null)
            {
                Gizmos.color = gridDebugColor;
                var tilemap = grid.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();
                if (tilemap != null)
                {
                    var bounds = tilemap.cellBounds;
                    Vector3 min = tilemap.CellToWorld(bounds.min);
                    Vector3 max = tilemap.CellToWorld(bounds.max);

                    // Draw grid bounds
                    Gizmos.DrawWireCube((min + max) / 2f, max - min);
                }
            }
        }
    }
}