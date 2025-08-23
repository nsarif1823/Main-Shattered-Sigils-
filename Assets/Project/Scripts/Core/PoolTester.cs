using UnityEngine;
using Aeloria.Core;

public class PoolTester : MonoBehaviour
{
    [SerializeField] private GameObject testPrefab;
    private bool poolCreated = false;

    private void Start()
    {
        Debug.Log("=== Pool System Test ===");

        // Create a simple test prefab if none assigned
        if (testPrefab == null)
        {
            CreateTestPrefab();
        }

        // Create the pool
        ObjectPooler.Instance.CreatePool("TestObject", testPrefab, 5, true);
        poolCreated = true;

        Debug.Log("Press SPACE to spawn objects from pool");
        Debug.Log("Objects will auto-return after 2 seconds");
    }

    private void CreateTestPrefab()
    {
        // Create a simple cube prefab for testing
        testPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testPrefab.name = "TestPoolCube";
        testPrefab.AddComponent<TestPoolObject>();

        // Make it red
        Renderer renderer = testPrefab.GetComponent<Renderer>();
        renderer.material.color = Color.red;

        // Hide the template
        testPrefab.SetActive(false);
    }

    private void Update()
    {
        if (!poolCreated) return;

        // Spawn on spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-3f, 3f),
                Random.Range(0f, 3f),
                Random.Range(-3f, 3f)
            );

            GameObject spawned = ObjectPooler.Instance.SpawnFromPool(
                "TestObject",
                randomPos,
                Quaternion.identity
            );

            if (spawned != null)
            {
                Debug.Log($"Spawned at {randomPos}. Pool has {ObjectPooler.Instance.GetPoolSize("TestObject")} objects remaining");
            }
        }

        // Check pool status
        if (Input.GetKeyDown(KeyCode.I))
        {
            int available = ObjectPooler.Instance.GetPoolSize("TestObject");
            Debug.Log($"Pool Status - Available: {available}");
        }
    }
}