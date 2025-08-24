using UnityEngine;
using Aeloria.Core;
using Aeloria.Entities;

namespace Aeloria.Entities.Player
{
    /// <summary>
    /// Player Controller fixed for Unity 2023/6 with comprehensive debug
    /// </summary>
    public class PlayerController : EntityBase, IMoveable
    {
        [Header("Player Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float dodgeSpeed = 15f;
        [SerializeField] private float dodgeDuration = 0.3f;
        [SerializeField] private float dodgeCooldown = 1f;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showDebugGizmos = true;

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

        // Public properties
        public bool IsMoving => currentState == PlayerState.Moving;
        public bool IsDodging => currentState == PlayerState.Dodging;
        public float CurrentMoveSpeed => moveSpeed;
        public float CurrentEnergy { get; private set; } = 100f;
        public float MaxEnergy { get; private set; } = 100f;
        public PlayerState CurrentState => currentState;

        protected override void Awake()
        {
            base.Awake();

            // spriteRenderer is already set by base.Awake() from EntityBase
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (spriteRenderer == null)
            {
                visualRenderer = GetComponentInChildren<Renderer>();
            }

            // CRITICAL FIX: Ensure player is above ground
            if (transform.position.y < 0.5f)
            {
                transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
                Debug.LogWarning($"PlayerController: Adjusted Y position to {transform.position.y} to prevent floor collision");
            }

            // Verify Rigidbody settings
            if (rb != null)
            {
                // For Unity 2023/6 - ensure no damping
                rb.linearDamping = 0f;
                rb.angularDamping = 0.05f;
                rb.useGravity = false;

                // Lock Y position and rotations
                rb.constraints = RigidbodyConstraints.FreezePositionY |
                                RigidbodyConstraints.FreezeRotationX |
                                RigidbodyConstraints.FreezeRotationZ;

                // Set collision detection to prevent tunneling
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                if (enableDebugLogs)
                {
                    Debug.Log($"Rigidbody configured: linearDamping={rb.linearDamping}, constraints={rb.constraints}");
                }
            }
            else
            {
                Debug.LogError("PlayerController: No Rigidbody found! Movement will not work!");
            }
        }

        protected override void Start()
        {
            base.Start();

            entityName = "Player";
            CurrentEnergy = MaxEnergy;

            EventManager.TriggerEvent("PlayerSpawned", gameObject);

            if (enableDebugLogs)
            {
                Debug.Log($"Player spawned at {transform.position}! Health: {CurrentHealth}/{MaxHealth}, MoveSpeed: {moveSpeed}");
            }
        }

        private void Update()
        {
            if (!IsAlive) return;

            HandleInput();
            UpdateTimers();
            UpdateState();

            // DEBUG CONTROLS
            if (enableDebugLogs)
            {
                // Press Space for debug info
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    PrintDebugInfo();
                }

                // Press F9 for Rigidbody info
                if (Input.GetKeyDown(KeyCode.F9))
                {
                    PrintRigidbodyDebug();
                }

                // Press F10 to reset position
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

            HandleMovement();

            // Prevent falling through floor
            if (transform.position.y < 0.5f)
            {
                transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"Player falling! Reset Y to {transform.position.y}");
                }
            }
        }

        private void HandleInput()
        {
            // Get movement input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            moveInput = new Vector2(horizontal, vertical).normalized;

            // Log input if moving
            if (enableDebugLogs && moveInput.magnitude > 0.1f)
            {
                Debug.Log($"Input detected: ({horizontal}, {vertical}) → normalized: {moveInput}");
            }

            // Remember last direction
            if (moveInput.magnitude > 0.1f)
            {
                lastMoveDirection = moveInput;
            }

            // Check for dodge
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

            // Log state changes
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

        private void HandleMovement()
        {
            Vector3 targetVelocity = Vector3.zero;

            switch (currentState)
            {
                case PlayerState.Idle:
                    targetVelocity = Vector3.zero;
                    break;

                case PlayerState.Moving:
                    // 3D movement on X-Z plane (Y stays 0)
                    targetVelocity = new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed;
                    break;

                case PlayerState.Dodging:
                    targetVelocity = new Vector3(dodgeDirection.x, 0, dodgeDirection.y) * dodgeSpeed;
                    break;

                case PlayerState.Dead:
                    targetVelocity = Vector3.zero;
                    break;
            }

            // Apply velocity using the Unity 2023/6 property
            if (rb != null)
            {
                rb.linearVelocity = targetVelocity;

                // Log significant velocity changes
                if (enableDebugLogs && rb.linearVelocity.magnitude > 0.1f)
                {
                    Debug.Log($"Velocity applied: {rb.linearVelocity} (magnitude: {rb.linearVelocity.magnitude:F2})");
                }
            }
            else
            {
                Debug.LogError("No Rigidbody found - cannot apply movement!");
            }
        }

        public void Move(Vector3 direction)
        {
            if (!canMove || currentState == PlayerState.Dodging) return;

            // Apply 3D movement
            rb.linearVelocity = new Vector3(direction.x, 0, direction.z) * moveSpeed;

            // Flip sprite
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

            // Visual feedback
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

            // Reset visual
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

        // DEBUG METHODS
        private void PrintDebugInfo()
        {
            Debug.Log("=== PLAYER DEBUG INFO ===");
            Debug.Log($"Position: {transform.position}");
            Debug.Log($"State: {currentState}");
            Debug.Log($"MoveInput: {moveInput}");
            Debug.Log($"CanMove: {canMove}");
            Debug.Log($"MoveSpeed: {moveSpeed}");
            Debug.Log($"IsAlive: {IsAlive}");
            Debug.Log($"Health: {CurrentHealth}/{MaxHealth}");
        }

        private void PrintRigidbodyDebug()
        {
            if (rb != null)
            {
                Debug.Log("=== RIGIDBODY DEBUG ===");
                Debug.Log($"Velocity: {rb.linearVelocity}");
                Debug.Log($"Linear Damping: {rb.linearDamping}");
                Debug.Log($"Angular Damping: {rb.angularDamping}");
                Debug.Log($"Use Gravity: {rb.useGravity}");
                Debug.Log($"Is Kinematic: {rb.isKinematic}");
                Debug.Log($"Constraints: {rb.constraints}");
                Debug.Log($"Collision Mode: {rb.collisionDetectionMode}");
            }
            else
            {
                Debug.LogError("NO RIGIDBODY FOUND!");
            }
        }

        // Energy system methods
        public bool TryConsumeEnergy(float amount)
        {
            if (CurrentEnergy >= amount)
            {
                CurrentEnergy -= amount;
                EventManager.TriggerEvent("PlayerEnergyChanged", new { current = CurrentEnergy, max = MaxEnergy });
                return true;
            }
            return false;
        }

        public void RegenerateEnergy(float amount)
        {
            CurrentEnergy = Mathf.Min(MaxEnergy, CurrentEnergy + amount);
            EventManager.TriggerEvent("PlayerEnergyChanged", new { current = CurrentEnergy, max = MaxEnergy });
        }

        protected override void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            base.OnDrawGizmos();

            if (!Application.isPlaying) return;

            // Draw movement direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, new Vector3(moveInput.x, 0, moveInput.y) * 2f);

            // Draw velocity
            if (rb != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, rb.linearVelocity);
            }

            // Draw dodge cooldown
            if (dodgeCooldownTimer > 0)
            {
                Gizmos.color = Color.yellow;
                Vector3 from = transform.position + Vector3.up * 0.5f;
                Gizmos.DrawWireSphere(from, 0.2f * (dodgeCooldownTimer / dodgeCooldown));
            }
        }
    }

    public interface IMoveable
    {
        void Move(Vector3 direction);
        void StopMovement();
    }
}