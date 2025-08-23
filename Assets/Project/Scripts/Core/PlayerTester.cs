using UnityEngine;
using Aeloria.Core;
using Aeloria.Entities.Player;  // This should work now!

public class PlayerTester : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject playerPrefab;

    private PlayerController player;

    private void Start()
    {
        Debug.Log("=== Player Controller Test Started ===");
        Debug.Log("Controls:");
        Debug.Log("  WASD/Arrows = Move");
        Debug.Log("  Left Shift = Dodge");
        Debug.Log("  Q = Damage player");
        Debug.Log("  H = Heal player");
        Debug.Log("  Tab = Show stats");

        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("❌ Player Prefab not assigned in PlayerTester!");
            Debug.LogError("Fix: Drag Player.prefab into the Player Prefab slot in Inspector");

            player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                Debug.Log("Found existing player in scene");
            }
            return;
        }

        Debug.Log("Spawning player from prefab...");
        GameObject playerObj = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        player = playerObj.GetComponent<PlayerController>();

        if (player != null)
        {
            Debug.Log($"✅ Player spawned! Health: {player.CurrentHealth}/{player.MaxHealth}");
        }
        else
        {
            Debug.LogError("❌ PlayerController component not found on prefab!");
        }
    }

    private void Update()
    {
        if (player == null)
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    Debug.Log("Found player with F5!");
                }
                else
                {
                    Debug.LogError("No player found in scene!");
                }
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("--- Q Pressed: Damaging Player ---");
            player.TakeDamage(2f, gameObject);
            Debug.Log($"Health: {player.CurrentHealth}/{player.MaxHealth}");
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("--- H Pressed: Healing Player ---");
            player.Heal(3f);
            Debug.Log($"Health: {player.CurrentHealth}/{player.MaxHealth}");
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("=== PLAYER STATS (Tab) ===");
            Debug.Log($"  Alive: {player.IsAlive}");
            Debug.Log($"  Health: {player.CurrentHealth}/{player.MaxHealth}");
            Debug.Log($"  Moving: {player.IsMoving}");
            Debug.Log($"  Dodging: {player.IsDodging}");
            Debug.Log($"  Position: {player.transform.position}");
        }
    }
}