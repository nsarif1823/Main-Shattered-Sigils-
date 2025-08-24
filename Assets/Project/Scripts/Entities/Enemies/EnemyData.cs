using UnityEngine;

namespace Aeloria.Entities.Enemies
{
    /// <summary>
    /// ScriptableObject defining enemy stats and behavior
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Aeloria/Enemies/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName = "Enemy";
        public EnemyType enemyType = EnemyType.Melee;
        public GameObject visualPrefab; // Just the visual/model

        [Header("Stats")]
        public float maxHealth = 10f;
        public float moveSpeed = 3f;
        public float damage = 2f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1f;
        public float detectionRange = 8f;

        [Header("Behavior")]
        public AIBehavior aiBehavior = AIBehavior.Aggressive;
        public float aggroDropDistance = 15f;
        public bool canDodge = false;
        public float dodgeChance = 0.1f;

        [Header("Rewards")]
        public int experienceValue = 10;
        public float energyOnKill = 5f;

        [Header("Visual")]
        public Color tintColor = Color.white;
        public float scale = 1f;

        public enum EnemyType
        {
            Melee,
            Ranged,
            Tank,
            Support,
            Boss
        }

        public enum AIBehavior
        {
            Aggressive,  // Always attacks
            Defensive,   // Attacks when threatened
            Passive,     // Never attacks
            Guardian,    // Protects area
            Swarm        // Groups with others
        }
    }
}