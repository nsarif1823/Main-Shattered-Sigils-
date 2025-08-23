using UnityEngine;
using Aeloria.Core;

public class SystemTester : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== System Test Started ===");

        // Test GameManager
        if (GameManager.Instance != null)
        {
            Debug.Log("✓ GameManager initialized");
            Debug.Log($"  Current State: {GameManager.Instance.CurrentState}");
        }
        else
        {
            Debug.LogError("✗ GameManager failed to initialize");
        }

        // Test EventManager
        if (EventManager.Instance != null)
        {
            Debug.Log("✓ EventManager initialized");

            // Subscribe to test event
            EventManager.StartListening("TestEvent", OnTestEvent);

            // Trigger test event
            EventManager.TriggerEvent("TestEvent", "Hello from EventManager!");
        }
        else
        {
            Debug.LogError("✗ EventManager failed to initialize");
        }

        // Test game state changes
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnTestEvent(object data)
    {
        Debug.Log($"✓ Event System Working! Data: {data}");
    }

    private void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        Debug.Log($"✓ State Change Event: {oldState} → {newState}");
    }

    private void Update()
    {
        // Test controls
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Starting game...");
            GameManager.Instance.StartGame();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                GameManager.Instance.PauseGame();
            }
            else if (GameManager.Instance.CurrentState == GameManager.GameState.Paused)
            {
                GameManager.Instance.ResumeGame();
            }
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            GameManager.Instance.GameOver();
        }
    }

    private void OnDestroy()
    {
        EventManager.StopListening("TestEvent", OnTestEvent);
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }
}