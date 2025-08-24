using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Aeloria.Core;

namespace Aeloria.Entities.Enemies
{
    /// <summary>
    /// Flexible enemy spawning system using EnemyData
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private float spawnDelay = 2f;
        [SerializeField] private int enemiesPerWave = 3;

        [Header("Enemy Data")]
        [SerializeField] private EnemyData defaultEnemyData;
        [SerializeField] private EnemyData[] enemyTypes;

        [Header("Spawn Positions")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private float minSpawnDistance = 5f;

        private List<GameObject> activeEnemies = new List<GameObject>();
        private Transform player;

        void Start()
        {
            player = GameObject.FindWithTag("Player")?.transform;

            if (spawnOnStart)
            {
                StartCoroutine(SpawnInitialEnemies());
            }

            EventManager.StartListening("SpawnEnemyWave", (data) => SpawnWave());
            EventManager.StartListening("ClearAllEnemies", (data) => ClearAllEnemies());
        }

        IEnumerator SpawnInitialEnemies()
        {
            yield return new WaitForSeconds(spawnDelay);
            SpawnWave();
        }

        public void SpawnWave()
        {
            for (int i = 0; i < enemiesPerWave; i++)
            {
                SpawnEnemy(GetSpawnPosition(i));
            }

            Debug.Log($"Spawned {enemiesPerWave} enemies");
            EventManager.TriggerEvent("EnemyWaveSpawned", enemiesPerWave);
        }

        public GameObject SpawnEnemy(Vector3 position)
        {
            // Get enemy data
            EnemyData dataToUse = GetEnemyData();

            // If no data available, create test enemy
            if (dataToUse == null)
            {
                dataToUse = CreateTestEnemyData();
            }

            // Create enemy using factory
            GameObject enemy = EnemyFactory.CreateEnemy(dataToUse, position);

            activeEnemies.Add(enemy);

            // Track when enemy dies
            var enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.OnDeath += () => OnEnemyDied(enemy);
            }

            return enemy;
        }

        Vector3 GetSpawnPosition(int index)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                return spawnPoints[index % spawnPoints.Length].position;
            }

            if (player != null)
            {
                float angle = (360f / enemiesPerWave) * index;
                float rad = angle * Mathf.Deg2Rad;

                Vector3 offset = new Vector3(
                    Mathf.Sin(rad) * spawnRadius,
                    0,
                    Mathf.Cos(rad) * spawnRadius
                );

                Vector3 spawnPos = player.position + offset;
                spawnPos.y = 1f;

                return spawnPos;
            }

            Vector3[] fallbackPositions = new Vector3[]
            {
                new Vector3(5, 1, 5),
                new Vector3(-5, 1, 5),
                new Vector3(0, 1, -5)
            };

            return fallbackPositions[index % fallbackPositions.Length];
        }

        EnemyData GetEnemyData()
        {
            if (defaultEnemyData != null)
                return defaultEnemyData;

            if (enemyTypes != null && enemyTypes.Length > 0)
            {
                return enemyTypes[Random.Range(0, enemyTypes.Length)];
            }

            return null;
        }

        /// <summary>
        /// Create test enemy data if none exists
        /// </summary>
        EnemyData CreateTestEnemyData()
        {
            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = "Test Enemy";
            data.maxHealth = 10f;
            data.moveSpeed = 2f;
            data.damage = 1f;
            data.attackRange = 1.5f;
            data.attackCooldown = 1f;
            data.detectionRange = 8f;
            data.aiBehavior = EnemyData.AIBehavior.Aggressive;
            data.tintColor = Color.red;
            data.experienceValue = 5;
            data.energyOnKill = 2f;

            Debug.Log("Created test enemy data - consider creating actual EnemyData assets!");

            return data;
        }

        void OnEnemyDied(GameObject enemy)
        {
            activeEnemies.Remove(enemy);

            if (activeEnemies.Count == 0)
            {
                EventManager.TriggerEvent("AllEnemiesDefeated", null);
                Debug.Log("All enemies defeated!");
            }
        }

        public void ClearAllEnemies()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                    Destroy(enemy);
            }
            activeEnemies.Clear();
        }

        void OnDrawGizmosSelected()
        {
            if (player != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(player.position, spawnRadius);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(player.position, minSpawnDistance);
            }

            if (spawnPoints != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var point in spawnPoints)
                {
                    if (point != null)
                        Gizmos.DrawSphere(point.position, 0.5f);
                }
            }
        }
    }
}