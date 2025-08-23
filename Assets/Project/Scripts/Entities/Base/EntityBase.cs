using Aeloria.Core;
using Aeloria.UI;
using UnityEngine;

namespace Aeloria.Entities
{
    public abstract class EntityBase : MonoBehaviour, IEntity, IDamageable, ITargetable
    {
        [Header("Entity Settings")]
        [SerializeField] protected string entityName = "Entity";
        [SerializeField] protected float maxHealth = 10f;
        [SerializeField] protected bool canBeTargeted = true;

        // Properties
        public string EntityName => entityName;
        public Transform Transform => transform;
        public bool IsAlive { get; protected set; } = true;
        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; protected set; }
        public bool CanBeTargeted => canBeTargeted && IsAlive;

        // Components
        protected Rigidbody2D rb;
        protected Collider2D col;
        protected SpriteRenderer spriteRenderer;

        // Events
        public System.Action<float, float> OnHealthChanged;
        public System.Action<float, GameObject> OnDamageTaken;
        public System.Action OnDeath;

        protected virtual void Awake()
        {
            // Cache components
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Create components if missing
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f; // Top-down game
            }

            if (col == null)
            {
                col = gameObject.AddComponent<CircleCollider2D>();
            }
        }

        protected virtual void Start()
        {
            Initialize();
        }

        public virtual void Initialize()
        {
            CurrentHealth = MaxHealth;
            IsAlive = true;

            if (Constants.DEBUG_MODE)
            {
                Debug.Log($"{EntityName} initialized with {MaxHealth} HP");
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
            UIEvents.TriggerHealthChanged(CurrentHealth, MaxHealth, gameObject);
            UIEvents.TriggerDamageReceived(damage, gameObject);

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

            UIEvents.TriggerHealthChanged(CurrentHealth, MaxHealth, gameObject);
            UIEvents.TriggerHealReceived(actualHealing, gameObject);

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
                Debug.Log($"{EntityName} died!");
            }

            // Override in derived classes for specific death behavior
            HandleDeath();
        }

        protected abstract void HandleDeath();

        public virtual Vector3 GetTargetPosition()
        {
            return transform.position;
        }

        public virtual float GetTargetPriority()
        {
            // Lower health = higher priority
            return 1f - (CurrentHealth / MaxHealth);
        }

        protected System.Collections.IEnumerator DamageFlash()
        {
            if (spriteRenderer != null)
            {
                Color originalColor = spriteRenderer.color;
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(Constants.HIT_FLASH_DURATION);
                spriteRenderer.color = originalColor;
            }
        }

        // Debug visualization
        protected virtual void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Health bar
            Vector3 barPos = transform.position + Vector3.up * 1.5f;
            float barWidth = 1f;
            float healthPercent = CurrentHealth / MaxHealth;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(barPos - Vector3.right * barWidth / 2, barPos + Vector3.right * barWidth / 2);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(barPos - Vector3.right * barWidth / 2, barPos - Vector3.right * barWidth / 2 + Vector3.right * barWidth * healthPercent);
        }
    }
}