using UnityEngine;
using Aeloria.Entities;

namespace Aeloria.Entities.Summons
{
    public class WolfAI : EntityBase
    {
        [Header("Wolf Settings")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float attackCooldown = 1f;

        [Header("Wolf Stats")]
        [SerializeField] private float damage = 3f;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float followDistance = 2f; // How close to stay to player

        private Transform target;
        private Transform player;
        private float attackTimer;
        private bool isFollowingPlayer = false;

        private enum AIState { Idle, Following, Attacking }
        private AIState currentState = AIState.Idle;

        protected override void Start()
        {
            base.Start();
            entityName = "Wolf";

            // Find player reference
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;

            FindTarget();
        }

        void Update()
        {
            if (!IsAlive) return;

            attackTimer -= Time.deltaTime;
            UpdateAI();
        }

        void UpdateAI()
        {
            // Find nearest enemy
            if (target == null || !target.gameObject.activeSelf || isFollowingPlayer)
            {
                FindTarget();
            }

            if (target == null)
            {
                currentState = AIState.Idle;
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            // State machine
            switch (currentState)
            {
                case AIState.Idle:
                    if (distanceToTarget < detectionRange)
                        currentState = AIState.Following;
                    break;

                case AIState.Following:
                    MoveTowardsTarget();

                    // Different behavior for following player vs enemy
                    if (isFollowingPlayer)
                    {
                        // Stay near player but not too close
                        if (distanceToTarget < followDistance)
                            StopMovement();
                    }
                    else
                    {
                        // Attack enemy when in range
                        if (distanceToTarget < attackRange)
                            currentState = AIState.Attacking;
                        else if (distanceToTarget > detectionRange)
                            currentState = AIState.Idle;
                    }
                    break;

                case AIState.Attacking:
                    if (distanceToTarget > attackRange)
                        currentState = AIState.Following;
                    else if (attackTimer <= 0)
                        Attack();
                    break;
            }
        }

        void MoveTowardsTarget()
        {
            if (target == null) return;

            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0; // Keep on ground plane

            // Use MovePosition for smoother, physics-based movement
            Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;
            rb.MovePosition(newPosition);

            // Face target with only Y rotation (no tilting)
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }

        void StopMovement()
        {
            rb.linearVelocity = Vector3.zero;
        }

        void Attack()
        {
            if (target.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage, gameObject);
                attackTimer = attackCooldown;

                Debug.Log($"Wolf attacked {target.name} for {damage} damage!");
            }
        }

        void FindTarget()
        {
            // First look for enemies
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            if (enemies.Length > 0)
            {
                float closestDistance = detectionRange;
                Transform closestEnemy = null;

                foreach (var enemy in enemies)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy.transform;
                    }
                }

                if (closestEnemy != null)
                {
                    target = closestEnemy;
                    isFollowingPlayer = false;
                    return;
                }
            }

            // No enemies found - follow player instead
            if (player != null)
            {
                target = player;
                isFollowingPlayer = true;
                currentState = AIState.Following;
            }
        }

        protected override void HandleDeath()
        {
            // Wolf death - maybe particle effect later
            Destroy(gameObject, 0.5f);
        }

        void OnDrawGizmosSelected()
        {
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Line to current target
            if (target != null)
            {
                Gizmos.color = isFollowingPlayer ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}