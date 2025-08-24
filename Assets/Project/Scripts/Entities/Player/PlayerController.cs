using UnityEngine;
using Aeloria.Core;
using Aeloria.Entities;

namespace Aeloria.Entities.Player
{
    /// <summary>
    /// Controls player movement, dodging, and state management
    /// Handles all player input and broadcasts player events
    /// </summary>
    public class PlayerController : EntityBase, IMoveable
    {
        [Header("Player Settings")]
        [SerializeField] private float moveSpeed = 5f;      // How fast player moves normally
        [SerializeField] private float dodgeSpeed = 15f;    // Speed during dodge roll
        [SerializeField] private float dodgeDuration = 0.3f; // How long dodge lasts
        [SerializeField] private float dodgeCooldown = 1f;   // Time between dodges

        // ===== STATE MANAGEMENT =====
        // Player can only be in one state at a time
        public enum PlayerState
        {
            Idle,    // Standing still
            Moving,  // Walking/running
            Dodging, // Mid-dodge roll (invulnerable)
            Casting, // Playing a card (can't move)
            Dead     // Game over
        }

        private PlayerState currentState = PlayerState.Idle;

        // ===== MOVEMENT VARIABLES =====
        private Vector2 moveInput;                      // Raw input from WASD/arrows
        private Vector2 lastMoveDirection = Vector2.down; // Remember last direction for dodge
        private bool canMove = true;                    // Can be disabled during casting

        // ===== DODGE VARIABLES =====
        private float dodgeTimer = 0f;         // Counts down during dodge
        private float dodgeCooldownTimer = 0f; // Prevents dodge spam
        private Vector2 dodgeDirection;        // Direction we're dodging

        // ===== VISUAL COMPONENTS =====
        private SpriteRenderer spriteRenderer;  // For flipping sprite and visual effects
        private Renderer visualRenderer;        // Alternative if using 3D model

        // ===== PUBLIC PROPERTIES =====
        public bool IsMoving => currentState == PlayerState.Moving;
        public bool IsDodging => currentState == PlayerState.Dodging;
        public float CurrentMoveSpeed => moveSpeed;
        public float CurrentEnergy { get; private set; } = 100f;  // Energy system
        public float MaxEnergy { get; private set; } = 100f;
        public PlayerState CurrentState => currentState;

        // ===== UNITY LIFECYCLE =====

        protected override void Awake()
        {
            base.Awake();  // IMPORTANT: Calls EntityBase.Awake() to set up rb and collider

            // Get visual components
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                visualRenderer = GetComponentInChildren<Renderer>();
            }
        }

        protected override void Start()
        {
            base.Start();

            // Set player specific values
            entityName = "Player";
            entityType = EntityType.Player;

            // Initialize energy
            CurrentEnergy = MaxEnergy;

            // Notify systems player has spawned
            EventManager.TriggerEvent("PlayerSpawned", gameObject);

            if (Constants.DEBUG_MODE)
            {
                Debug.Log($"Player spawned! Health: {CurrentHealth}/{MaxHealth}");
            }
        }

        private void Update()
        {
            if (!IsAlive) return;

            HandleInput();
            UpdateTimers();
            UpdateState();
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;

            HandleMovement();
        }

        // ===== INPUT HANDLING =====

        /// <summary>
        /// Reads player input from keyboard/controller
        /// Called every frame in Update()
        /// </summary>
        private void HandleInput()
        {
            // Get movement input (WASD or Arrow keys)
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
            moveInput = moveInput.normalized; // Prevent diagonal speed boost

            // Remember last direction for dodging
            if (moveInput.magnitude > 0.1f)
            {
                lastMoveDirection = moveInput;
            }

            // Check for dodge input (Left Shift by default)
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                TryDodge();
            }
        }

        // ===== STATE MANAGEMENT =====

        /// <summary>
        /// Updates the player's state based on current conditions
        /// States control what actions are available
        /// </summary>
        private void UpdateState()
        {
            // Dead state overrides everything
            if (!IsAlive)
            {
                currentState = PlayerState.Dead;
                return;
            }

            // Dodging has priority over movement
            if (dodgeTimer > 0)
            {
                currentState = PlayerState.Dodging;
                return;
            }

            // Check if moving
            if (moveInput.magnitude > 0.1f && canMove)
            {
                currentState = PlayerState.Moving;
            }
            else
            {
                currentState = PlayerState.Idle;
            }
        }

        /// <summary>
        /// Updates all timer countdowns
        /// Called every frame
        /// </summary>
        private void UpdateTimers()
        {
            // Count down dodge duration
            if (dodgeTimer > 0)
            {
                dodgeTimer -= Time.deltaTime;
                if (dodgeTimer <= 0)
                {
                    EndDodge();
                }
            }

            // Count down dodge cooldown
            if (dodgeCooldownTimer > 0)
            {
                dodgeCooldownTimer -= Time.deltaTime;
            }
        }

        // ===== MOVEMENT =====

        /// <summary>
        /// Applies movement based on current state
        /// Called in FixedUpdate for smooth physics
        /// </summary>
        private void HandleMovement()
        {
            switch (currentState)
            {
                case PlayerState.Idle:
                    // Stop completely when idle
                    rb.linearVelocity = Vector3.zero;
                    break;

                case PlayerState.Moving:
                    // For 3D isometric movement
                    Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed;
                    rb.linearVelocity = movement;
                    break;

                case PlayerState.Dodging:
                    // Fast movement in dodge direction
                    Vector3 dodgeMovement = new Vector3(dodgeDirection.x, 0, dodgeDirection.y) * dodgeSpeed;
                    rb.linearVelocity = dodgeMovement;
                    break;

                case PlayerState.Dead:
                    // No movement when dead
                    rb.linearVelocity = Vector3.zero;
                    break;
            }
        }

        /// <summary>
        /// Moves the player in a direction
        /// Implements IMoveable interface
        /// </summary>
        public void Move(Vector3 direction)
        {
            // Can't move while dodging or if movement disabled
            if (!canMove || currentState == PlayerState.Dodging) return;

            // Apply movement velocity
            rb.linearVelocity = direction * moveSpeed;

            // Flip sprite to face movement direction
            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.1f)
            {
                spriteRenderer.flipX = direction.x < 0; // Flip when moving left
            }
        }

        /// <summary>
        /// Stops all player movement immediately
        /// Used when entering cutscenes or menus
        /// </summary>
        public void StopMovement()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
            moveInput = Vector2.zero;
        }

        // ===== DODGE MECHANICS =====

        /// <summary>
        /// Attempts to perform a dodge roll
        /// Fails if on cooldown or already dodging
        /// </summary>
        private void TryDodge()
        {
            // Check if we can dodge
            if (dodgeCooldownTimer > 0 || currentState == PlayerState.Dodging)
                return;

            // ===== START DODGE =====
            currentState = PlayerState.Dodging;
            dodgeTimer = dodgeDuration;           // Start countdown
            dodgeCooldownTimer = dodgeCooldown;   // Start cooldown

            // Dodge in movement direction, or last faced direction if standing
            dodgeDirection = moveInput.magnitude > 0.1f ? moveInput : lastMoveDirection;

            // Make invulnerable during dodge
            canBeTargeted = false;

            // ===== VISUAL FEEDBACK =====
            // Make player semi-transparent
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

            // Notify other systems (for sound effects, etc.)
            EventManager.TriggerEvent("PlayerDodged", dodgeDirection);

            if (Constants.DEBUG_MODE)
            {
                Debug.Log("Player dodged!");
            }
        }

        /// <summary>
        /// Called when dodge roll ends
        /// Restores vulnerability and visual state
        /// </summary>
        private void EndDodge()
        {
            // Restore vulnerability
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

            if (Constants.DEBUG_MODE)
            {
                Debug.Log("Dodge ended");
            }
        }

        // ===== DEATH HANDLING =====

        /// <summary>
        /// Called when player health reaches zero
        /// Triggers game over sequence
        /// </summary>
        protected override void HandleDeath()
        {
            currentState = PlayerState.Dead;
            StopMovement();

            // Disable collisions
            if (col != null)
            {
                col.enabled = false;
            }

            // Notify game systems
            EventManager.TriggerEvent("PlayerDied", transform.position);

            // Could trigger death animation here
            if (Constants.DEBUG_MODE)
            {
                Debug.Log("GAME OVER - Player has died!");
            }
        }

        // ===== ENERGY SYSTEM =====

        /// <summary>
        /// Try to consume energy for casting
        /// Returns true if successful
        /// </summary>
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

        /// <summary>
        /// Regenerate energy over time or from pickups
        /// </summary>
        public void RegenerateEnergy(float amount)
        {
            CurrentEnergy = Mathf.Min(MaxEnergy, CurrentEnergy + amount);
            EventManager.TriggerEvent("PlayerEnergyChanged", new { current = CurrentEnergy, max = MaxEnergy });
        }

        // ===== DEBUG =====

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!Application.isPlaying) return;

            // Draw movement direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, new Vector3(moveInput.x, 0, moveInput.y));

            // Draw dodge cooldown
            if (dodgeCooldownTimer > 0)
            {
                Gizmos.color = Color.yellow;
                float angle = (dodgeCooldownTimer / dodgeCooldown) * 360f;
                Vector3 from = transform.position + Vector3.up * 0.5f;
                // Simple cooldown indicator
                Gizmos.DrawWireSphere(from, 0.2f);
            }
        }
    }

    // ===== INTERFACES =====

    public interface IMoveable
    {
        void Move(Vector3 direction);
        void StopMovement();
    }
}