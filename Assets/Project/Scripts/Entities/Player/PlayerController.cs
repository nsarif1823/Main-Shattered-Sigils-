using UnityEngine;
using Aeloria.Core;
using Aeloria.Entities;

namespace Aeloria.Entities.Player
{
    /// <summary>
    /// Player Controller with screen-relative movement for isometric view
    /// </summary>
    public class PlayerController : EntityBase, IMoveable
    {
        [Header("Player Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float dodgeSpeed = 15f;
        [SerializeField] private float dodgeDuration = 0.3f;
        [SerializeField] private float dodgeCooldown = 1f;

        [Header("Energy Settings")]
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float energyRegenRate = 2f;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool useScreenRelativeMovement = true;

        // Player states
        public enum PlayerState
        {
            Idle,
            Moving,
            Dodging,
            Casting,
            Dead
        }

        private PlayerState currentState = PlayerState.Idle;

        // Movement variables
        private Vector2 moveInput;
        private Vector2 lastMoveDirection = Vector2.down;
        private bool canMove = true;

        // Dodge variables
        private float dodgeTimer = 0f;
        private float dodgeCooldownTimer = 0f;
        private Vector2 dodgeDirection;

        // Visual components
        private Renderer visualRenderer;

        // Energy
        private float currentEnergy;

        // Public properties
        public bool IsMoving => currentState == PlayerState.Moving;
        public bool IsDodging => currentState == PlayerState.Dodging;
        public float CurrentMoveSpeed => moveSpeed;
        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => maxEnergy;
        public PlayerState CurrentState => currentState;
        public float MoveSpeed => moveSpeed;

        protected override void Awake()
        {
            base.Awake();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (spriteRenderer == null)
            {
                visualRenderer = GetComponentInChildren<Renderer>();
            }

            if (transform.position.y < 0.5f)
            {
                transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
                Debug.LogWarning($"PlayerController: Adjusted Y position to {transform.position.y}");
            }

            if (rb != null)
            {
                rb.linearDamping = 0f;
                rb.angularDamping = 0.05f;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezePositionY |
                                RigidbodyConstraints.FreezeRotationX |
                                RigidbodyConstraints.FreezeRotationZ;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                if (enableDebugLogs)
                {
                    Debug.Log($"Rigidbody configured: linearDamping={rb.linearDamping}, constraints={rb.constraints}");
                }
            }
            else
            {
                Debug.LogError("PlayerController: No Rigidbody found!");
            }
        }

        protected override void Start()
        {
            base.Start();
            entityName = "Player";
            currentEnergy = maxEnergy;
            EventManager.TriggerEvent("PlayerSpawned", gameObject);

            if (enableDebugLogs)
            {
                Debug.Log($"Player spawned at {transform.position}! Health: {CurrentHealth}/{MaxHealth}, MoveSpeed: {moveSpeed}");
            }
        }

        private void Update()
        {
            if (!IsAlive) return;

            ProcessInput();
            UpdateTimers();
            UpdateState();
            RegenerateEnergyOverTime();

            if (enableDebugLogs)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    PrintDebugInfo();
                }
                if (Input.GetKeyDown(KeyCode.F9))
                {
                    PrintRigidbodyDebug();
                }
                if (Input.GetKeyDown(KeyCode.F10))
                {
                    transform.position = new Vector3(0, 1, 0);
                    rb.linearVelocity = Vector3.zero;
                    Debug.Log("Player position reset to (0, 1, 0)");
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;

            ApplyMovement();

            if (transform.position.y < 0.5f)
            {
                transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"Player falling! Reset Y to {transform.position.y}");
                }
            }
        }

        private void ProcessInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (useScreenRelativeMovement)
            {
                // Isometric screen-relative movement
                // W moves north (up on screen), S south, A west, D east
                float worldX = (horizontal + vertical) * 0.707f;
                float worldZ = (vertical - horizontal) * 0.707f;
                moveInput = new Vector2(worldX, worldZ);

                if (moveInput.magnitude > 1f)
                {
                    moveInput = moveInput.normalized;
                }
            }
            else
            {
                // Direct world-space movement
                moveInput = new Vector2(horizontal, vertical).normalized;
            }

            if (enableDebugLogs && moveInput.magnitude > 0.1f)
            {
                Debug.Log($"Input: H={horizontal}, V={vertical} → Move: {moveInput}");
            }

            if (moveInput.magnitude > 0.1f)
            {
                lastMoveDirection = moveInput;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                TryDodge();
            }
        }

        private void UpdateState()
        {
            PlayerState previousState = currentState;

            if (!IsAlive)
            {
                currentState = PlayerState.Dead;
                return;
            }

            if (dodgeTimer > 0)
            {
                currentState = PlayerState.Dodging;
                return;
            }

            if (moveInput.magnitude > 0.1f && canMove)
            {
                currentState = PlayerState.Moving;
            }
            else
            {
                currentState = PlayerState.Idle;
            }

            if (enableDebugLogs && previousState != currentState)
            {
                Debug.Log($"State changed: {previousState} → {currentState}");
            }
        }

        private void UpdateTimers()
        {
            if (dodgeTimer > 0)
            {
                dodgeTimer -= Time.deltaTime;
                if (dodgeTimer <= 0)
                {
                    EndDodge();
                }
            }

            if (dodgeCooldownTimer > 0)
            {
                dodgeCooldownTimer -= Time.deltaTime;
            }
        }

        private void ApplyMovement()
        {
            Vector3 targetVelocity = Vector3.zero;

            switch (currentState)
            {
                case PlayerState.Idle:
                    targetVelocity = Vector3.zero;
                    break;

                case PlayerState.Moving:
                    targetVelocity = new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed;
                    break;

                case PlayerState.Dodging:
                    targetVelocity = new Vector3(dodgeDirection.x, 0, dodgeDirection.y) * dodgeSpeed;
                    break;

                case PlayerState.Dead:
                    targetVelocity = Vector3.zero;
                    break;
            }

            if (rb != null)
            {
                rb.linearVelocity = targetVelocity;

                if (enableDebugLogs && rb.linearVelocity.magnitude > 0.1f)
                {
                    Debug.Log($"Velocity: {rb.linearVelocity} (mag: {rb.linearVelocity.magnitude:F2})");
                }
            }
        }

        public void Move(Vector3 direction)
        {
            if (!canMove || currentState == PlayerState.Dodging) return;

            rb.linearVelocity = new Vector3(direction.x, 0, direction.z) * moveSpeed;

            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.1f)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }

        public void StopMovement()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
            moveInput = Vector2.zero;

            if (enableDebugLogs)
            {
                Debug.Log("Movement stopped");
            }
        }

        private void TryDodge()
        {
            if (dodgeCooldownTimer > 0 || currentState == PlayerState.Dodging)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"Cannot dodge: Cooldown={dodgeCooldownTimer:F2}, State={currentState}");
                }
                return;
            }

            currentState = PlayerState.Dodging;
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown;
            dodgeDirection = moveInput.magnitude > 0.1f ? moveInput : lastMoveDirection;
            canBeTargeted = false;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
            }
            else if (visualRenderer != null)
            {
                Color c = visualRenderer.material.color;
                c.a = 0.5f;
                visualRenderer.material.color = c;
            }

            EventManager.TriggerEvent("PlayerDodged", dodgeDirection);

            if (enableDebugLogs)
            {
                Debug.Log($"Dodge started! Direction: {dodgeDirection}");
            }
        }

        private void EndDodge()
        {
            canBeTargeted = true;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            else if (visualRenderer != null)
            {
                Color c = visualRenderer.material.color;
                c.a = 1f;
                visualRenderer.material.color = c;
            }

            if (enableDebugLogs)
            {
                Debug.Log("Dodge ended");
            }
        }

        protected override void HandleDeath()
        {
            currentState = PlayerState.Dead;
            StopMovement();

            if (col != null)
            {
                col.enabled = false;
            }

            EventManager.TriggerEvent("PlayerDied", transform.position);
            Debug.Log("GAME OVER - Player has died!");
        }

        // ENERGY SYSTEM
        private void RegenerateEnergyOverTime()
        {
            if (currentEnergy < maxEnergy)
            {
                currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegenRate * Time.deltaTime);

                if (Time.frameCount % 30 == 0)
                {
                    EventManager.TriggerEvent("PlayerEnergyChanged", new { current = currentEnergy, max = maxEnergy });
                }
            }
        }

        public void ConsumeEnergy(float amount)
        {
            currentEnergy = Mathf.Max(0, currentEnergy - amount);
            EventManager.TriggerEvent("PlayerEnergyChanged", new { current = currentEnergy, max = maxEnergy });

            if (enableDebugLogs)
            {
                Debug.Log($"Energy consumed: {amount}. Current: {currentEnergy}/{maxEnergy}");
            }
        }

        public bool HasEnergy(float amount)
        {
            return currentEnergy >= amount;
        }

        public bool TryConsumeEnergy(float amount)
        {
            if (currentEnergy >= amount)
            {
                ConsumeEnergy(amount);
                return true;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"Not enough energy! Need: {amount}, Have: {currentEnergy}");
            }
            return false;
        }

        public void AddEnergy(float amount)
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
            EventManager.TriggerEvent("PlayerEnergyChanged", new { current = currentEnergy, max = maxEnergy });
        }

        // DEBUG
        private void PrintDebugInfo()
        {
            Debug.Log("=== PLAYER DEBUG INFO ===");
            Debug.Log($"Position: {transform.position}");
            Debug.Log($"State: {currentState}");
            Debug.Log($"MoveInput: {moveInput}");
            Debug.Log($"Energy: {currentEnergy}/{maxEnergy}");
            Debug.Log($"Health: {CurrentHealth}/{MaxHealth}");
        }

        private void PrintRigidbodyDebug()
        {
            if (rb != null)
            {
                Debug.Log("=== RIGIDBODY DEBUG ===");
                Debug.Log($"Velocity: {rb.linearVelocity}");
                Debug.Log($"Constraints: {rb.constraints}");
            }
        }

        protected override void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            base.OnDrawGizmos();
            if (!Application.isPlaying) return;

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, new Vector3(moveInput.x, 0, moveInput.y) * 2f);

            if (rb != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, rb.linearVelocity);
            }

            if (dodgeCooldownTimer > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.2f * (dodgeCooldownTimer / dodgeCooldown));
            }
        }
    }
}