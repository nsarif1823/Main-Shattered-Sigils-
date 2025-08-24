using UnityEngine;
using System.Collections;
using Aeloria.Core;

namespace Aeloria.Entities.Enemies
{
    /// <summary>
    /// Universal enemy controller that uses EnemyData for configuration
    /// </summary>
    public class Enemy : EntityBase
    {
        [Header("Enemy Configuration")]
        [SerializeField] private EnemyData enemyData;

        // Runtime state
        private Transform target;
        private float nextAttackTime;
        private bool isAggro = false;
        private Coroutine behaviorCoroutine;

        // Components
        private GameObject visualInstance;

        // Properties
        public EnemyData Data => enemyData;
        public bool IsAggro => isAggro;

        /// <summary>
        /// Initialize enemy with specific data
        /// </summary>
        public void Initialize(EnemyData data)
        {
            enemyData = data;
            SetupFromData();
        }

        protected override void Awake()
        {
            base.Awake();

            // Ensure enemy tag
            if (!CompareTag("Enemy"))
            {
                gameObject.tag = "Enemy";
            }
        }

        protected override void Start()
        {
            base.Start();

            if (enemyData != null)
            {
                SetupFromData();
            }

            // Start AI behavior
            behaviorCoroutine = StartCoroutine(AIBehaviorLoop());
        }

        /// <summary>
        /// Configure enemy from data
        /// </summary>
        void SetupFromData()
        {
            if (enemyData == null)
            {
                Debug.LogError($"Enemy {gameObject.name} has no EnemyData!");
                return;
            }

            // Set stats
            entityName = enemyData.enemyName;
            maxHealth = enemyData.maxHealth;
            CurrentHealth = maxHealth;

            // Create visual if specified
            if (enemyData.visualPrefab != null && visualInstance == null)
            {
                visualInstance = Instantiate(enemyData.visualPrefab, transform);
                visualInstance.transform.localPosition = Vector3.zero;
                visualInstance.transform.localScale = Vector3.one * enemyData.scale;

                // Apply tint
                var renderers = visualInstance.GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    rend.material.color = enemyData.tintColor;
                }
            }

            Debug.Log($"{enemyData.enemyName} initialized: {maxHealth} HP, {enemyData.damage} DMG, {enemyData.aiBehavior} AI");
        }

        /// <summary>
        /// Main AI behavior loop
        /// </summary>
        IEnumerator AIBehaviorLoop()
        {
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f)); // Stagger AI updates

            while (IsAlive)
            {
                // Find target based on behavior
                UpdateTarget();

                if (target != null && isAggro)
                {
                    float distance = Vector3.Distance(transform.position, target.position);

                    // Drop aggro if too far
                    if (distance > enemyData.aggroDropDistance)
                    {
                        isAggro = false;
                        target = null;
                    }
                    // Attack if in range
                    else if (distance <= enemyData.attackRange && Time.time >= nextAttackTime)
                    {
                        PerformAttack();
                    }
                    // Move toward target
                    else if (distance > enemyData.attackRange)
                    {
                        MoveToward(target.position);
                    }
                }
                else
                {
                    // Idle behavior
                    HandleIdleBehavior();
                }

                yield return new WaitForSeconds(0.1f); // Update 10 times per second
            }
        }

        /// <summary>
        /// Update current target based on AI behavior
        /// </summary>
        void UpdateTarget()
        {
            switch (enemyData.aiBehavior)
            {
                case EnemyData.AIBehavior.Aggressive:
                    // Always target player
                    if (target == null)
                    {
                        GameObject player = GameObject.FindWithTag("Player");
                        if (player != null)
                        {
                            float distance = Vector3.Distance(transform.position, player.transform.position);
                            if (distance <= enemyData.detectionRange)
                            {
                                target = player.transform;
                                isAggro = true;
                            }
                        }
                    }
                    break;

                case EnemyData.AIBehavior.Defensive:
                    // Only aggro if attacked
                    if (!isAggro && target == null)
                    {
                        // Will be set when taking damage
                    }
                    break;

                case EnemyData.AIBehavior.Passive:
                    // Never aggro
                    isAggro = false;
                    target = null;
                    break;

                case EnemyData.AIBehavior.Guardian:
                    // Attack anything in range
                    if (target == null)
                    {
                        Collider[] nearby = Physics.OverlapSphere(transform.position, enemyData.detectionRange);
                        foreach (var col in nearby)
                        {
                            if (col.CompareTag("Player") || col.CompareTag("Summon"))
                            {
                                target = col.transform;
                                isAggro = true;
                                break;
                            }
                        }
                    }
                    break;

                case EnemyData.AIBehavior.Swarm:
                    // Group behavior - find player if other enemies are attacking
                    if (target == null)
                    {
                        Enemy[] allies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                        foreach (var ally in allies)
                        {
                            if (ally != this && ally.isAggro && ally.target != null)
                            {
                                target = ally.target;
                                isAggro = true;
                                break;
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Move toward position
        /// </summary>
        void MoveToward(Vector3 targetPos)
        {
            Vector3 direction = (targetPos - transform.position).normalized;
            direction.y = 0; // Stay on ground

            // Use Rigidbody if available
            if (rb != null)
            {
                rb.linearVelocity = direction * enemyData.moveSpeed;
            }
            else
            {
                transform.position += direction * enemyData.moveSpeed * Time.deltaTime;
            }

            // Face target
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        /// <summary>
        /// Perform attack
        /// </summary>
        void PerformAttack()
        {
            if (target == null) return;

            nextAttackTime = Time.time + enemyData.attackCooldown;

            // Deal damage
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(enemyData.damage, gameObject);
                Debug.Log($"{enemyData.enemyName} attacked for {enemyData.damage} damage!");

                // Trigger attack event
                EventManager.TriggerEvent("EnemyAttacked", new { enemy = this, target = target, damage = enemyData.damage });
            }
        }

        /// <summary>
        /// Handle idle behavior
        /// </summary>
        void HandleIdleBehavior()
        {
            // Stop moving
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }

            // Could add patrol behavior here
        }

        public override void TakeDamage(float damage, GameObject source)
        {
            base.TakeDamage(damage, source);

            // Defensive enemies aggro when attacked
            if (enemyData.aiBehavior == EnemyData.AIBehavior.Defensive && source != null)
            {
                target = source.transform;
                isAggro = true;
            }

            // Chance to dodge
            if (enemyData.canDodge && Random.value < enemyData.dodgeChance)
            {
                Debug.Log($"{enemyData.enemyName} dodged!");
                // Add dodge visual effect
            }
        }

        protected override void HandleDeath()
        {
            // Stop AI
            if (behaviorCoroutine != null)
            {
                StopCoroutine(behaviorCoroutine);
            }

            // Grant rewards
            EventManager.TriggerEvent("EnemyKilled", new
            {
                enemy = this,
                experience = enemyData.experienceValue,
                energy = enemyData.energyOnKill
            });

            Debug.Log($"{enemyData.enemyName} defeated! Rewards: {enemyData.experienceValue} XP, {enemyData.energyOnKill} Energy");

            // Cleanup
            Destroy(gameObject, 1f);
        }

        void OnDrawGizmosSelected()
        {
            if (enemyData == null) return;

            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, enemyData.detectionRange);

            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);

            // Aggro drop range
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, enemyData.aggroDropDistance);
        }
    }
}