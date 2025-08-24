using UnityEngine;
using System;
using System.Collections;
using Aeloria.Core;

namespace Aeloria.Entities
{
    /// <summary>
    /// Base class for all entities - Updated for 3D physics (isometric game)
    /// </summary>
    public abstract class EntityBase : MonoBehaviour, IEntity, IDamageable, ITargetable
    {
        [Header("Entity Settings")]
        [SerializeField] protected string entityName = "Entity";
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected bool canBeTargeted = true;

        // Properties
        public string EntityName => entityName;
        public Transform Transform => transform;
        public bool IsAlive { get; protected set; } = true;
        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; protected set; }
        public bool CanBeTargeted => canBeTargeted && IsAlive;

        // Components - CHANGED TO 3D
        protected Rigidbody rb;  // Changed from Rigidbody2D
        protected Collider col;  // Changed from Collider2D
        protected SpriteRenderer spriteRenderer;

        // Events
        public Action<float, float> OnHealthChanged;
        public Action<float, GameObject> OnDamageTaken;
        public Action OnDeath;

        protected virtual void Awake()
        {
            // Cache components - NOW 3D
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Create components if missing
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                // Configure for isometric movement
                rb.useGravity = false;  // No gravity for isometric
                rb.linearDamping = 0f;   // No damping for responsive movement
                rb.angularDamping = 0.05f;
                rb.constraints = RigidbodyConstraints.FreezePositionY |  // Lock Y for flat movement
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationZ;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                if (Constants.DEBUG_MODE)
                {
                    Debug.LogWarning($"{entityName}: Added missing Rigidbody component (3D)");
                }
            }

            if (col == null)
            {
                col = gameObject.AddComponent<CapsuleCollider>();
                if (Constants.DEBUG_MODE)
                {
                    Debug.LogWarning($"{entityName}: Added missing Collider component (3D)");
                }
            }
        }

        protected virtual void Start()
        {
            Initialize();

            // Register with game systems
            EventManager.TriggerEvent("EntitySpawned", this);
        }

        public virtual void Initialize()
        {
            CurrentHealth = MaxHealth;
            IsAlive = true;

            if (Constants.DEBUG_MODE)
            {
                Debug.Log($"{EntityName} initialized with {MaxHealth} HP at position {transform.position}");
            }
        }

        public virtual void TakeDamage(float damage, GameObject source)
        {
            if (!IsAlive) return;
            if (Constants.IMMORTAL_MODE && CompareTag(Constants.TAG_PLAYER)) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

            // Flash effect
            StartCoroutine(DamageFlash());

            // Events
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnDamageTaken?.Invoke(damage, source);
            EventManager.TriggerEvent("EntityDamaged", new { entity = this, damage = damage, source = source });

            if (Constants.DEBUG_MODE)
            {
                Debug.Log($"{EntityName} took {damage} damage from {source?.name ?? "unknown"}. HP: {CurrentHealth}/{MaxHealth}");
            }

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        public virtual void Heal(float amount)
        {
            if (!IsAlive) return;

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            float actualHealing = CurrentHealth - previousHealth;

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            EventManager.TriggerEvent("EntityHealed", new { entity = this, amount = actualHealing });

            if (Constants.DEBUG_MODE)
            {
                Debug.Log($"{EntityName} healed for {actualHealing}. HP: {CurrentHealth}/{MaxHealth}");
            }
        }

        public virtual void Die()
        {
            if (!IsAlive) return;

            IsAlive = false;
            canBeTargeted = false;

            OnDeath?.Invoke();
            EventManager.TriggerEvent("EntityDied", this);

            if (Constants.DEBUG_MODE)
            {
                Debug.Log($"{EntityName} has died!");
            }

            // Disable physics
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            if (col != null)
            {
                col.enabled = false;
            }

            HandleDeath();
        }

        /// <summary>
        /// Override to implement death behavior
        /// </summary>
        protected abstract void HandleDeath();

        /// <summary>
        /// Get position for targeting
        /// </summary>
        public virtual Vector3 GetTargetPosition()
        {
            // Return center mass position or custom target point
            return transform.position + Vector3.up * 0.5f;
        }

        /// <summary>
        /// Get targeting priority (higher = more likely to be targeted)
        /// </summary>
        public virtual float GetTargetPriority()
        {
            // Base priority on health percentage
            float healthPercent = CurrentHealth / MaxHealth;
            return 1f - healthPercent;  // Lower health = higher priority
        }

        /// <summary>
        /// Damage flash effect
        /// </summary>
        protected virtual IEnumerator DamageFlash()
        {
            if (spriteRenderer == null) yield break;

            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }

        /// <summary>
        /// Debug visualization
        /// </summary>
        protected virtual void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Health bar
            Vector3 barPos = transform.position + Vector3.up * 2f;
            float healthPercent = CurrentHealth / MaxHealth;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(barPos - Vector3.right * 0.5f, barPos + Vector3.right * 0.5f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(barPos - Vector3.right * 0.5f,
                          barPos - Vector3.right * 0.5f + Vector3.right * healthPercent);

            // Target position
            if (canBeTargeted)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(GetTargetPosition(), 0.2f);
            }
        }
    }

    // Interfaces are defined in IEntity.cs - don't duplicate them here
}