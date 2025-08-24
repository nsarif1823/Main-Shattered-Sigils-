using UnityEngine;
using Aeloria.Core;

namespace Aeloria.Entities.Enemies
{
    /// <summary>
    /// Factory for creating enemies from data
    /// </summary>
    public static class EnemyFactory
    {
        /// <summary>
        /// Create enemy from data at position
        /// </summary>
        public static GameObject CreateEnemy(EnemyData data, Vector3 position)
        {
            // Create base enemy object
            GameObject enemyObj = new GameObject(data.enemyName);
            enemyObj.transform.position = position;
            enemyObj.tag = "Enemy";

            // Add required components
            var enemy = enemyObj.AddComponent<Enemy>();

            // Add physics
            var rb = enemyObj.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                           RigidbodyConstraints.FreezeRotationZ;
            rb.useGravity = true;
            rb.linearDamping = 0f;

            // Add collider based on type
            if (data.enemyType == EnemyData.EnemyType.Tank)
            {
                var col = enemyObj.AddComponent<BoxCollider>();
                col.size = Vector3.one * 1.5f;
            }
            else
            {
                var col = enemyObj.AddComponent<CapsuleCollider>();
                col.height = 2f;
                col.radius = 0.5f;
            }

            // Initialize with data
            enemy.Initialize(data);

            Debug.Log($"Created {data.enemyName} at {position}");

            return enemyObj;
        }

        /// <summary>
        /// Create a test enemy with default settings
        /// </summary>
        public static GameObject CreateTestEnemy(Vector3 position)
        {
            // Create default data
            EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
            testData.enemyName = "Test Enemy";
            testData.maxHealth = 10f;
            testData.moveSpeed = 2f;
            testData.damage = 1f;
            testData.attackRange = 1.5f;
            testData.attackCooldown = 1f;
            testData.detectionRange = 8f;
            testData.aiBehavior = EnemyData.AIBehavior.Aggressive;
            testData.tintColor = Color.red;
            testData.experienceValue = 5;
            testData.energyOnKill = 2f;

            return CreateEnemy(testData, position);
        }
    }
}