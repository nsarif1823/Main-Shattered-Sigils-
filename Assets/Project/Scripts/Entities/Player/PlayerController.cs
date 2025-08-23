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
        private enum PlayerState
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

        // ===== PUBLIC PROPERTIES =====
        // Other systems can check these
        public float MoveSpeed => moveSpeed;
        public bool IsMoving => moveInput.magnitude > 0.1f;
        public bool IsDodging => currentState == PlayerState.Dodging;

        /// <summary>
        /// Initialize player-specific settings
        /// Called before Start()
        /// </summary>
        protected override void Awake()
        {
            base.Awake(); // Call parent class setup

            // Set player-specific values
            entityName = "Player";
            maxHealth = Constants.PLAYER_BASE_HEALTH; // Pull from constants file
            tag = Constants.TAG_PLAYER;               // Set Unity tag
        }

        /// <summary>
        /// Called when player spawns
        /// Notifies other systems that player exists
        /// </summary>
        protected override void Start()
        {
            base.Start();

            // Tell other systems (like camera) that player spawned
            EventManager.TriggerEvent("PlayerSpawned", this);
        }

        /// <summary>
        /// Called every frame
        /// Handles input and state updates
        /// </summary>
        private void Update()
        {
            // Don't process input if dead
            if (!IsAlive) return;

            HandleInput();    // Get keyboard/controller input
            UpdateTimers();   // Count down dodge cooldown
            UpdateState();    // Change state based on input
        }

        /// <summary>
        /// Called at fixed intervals for physics
        /// Handles actual movement
        /// </summary>
        private void FixedUpdate()
        {
            if (!IsAlive) return;

            HandleMovement(); // Apply velocity based on state
        }

        /// <summary>
        /// Gets input from keyboard/controller
        /// Processes dodge and movement commands
        /// </summary>
        private void HandleInput()
        {
            // ===== MOVEMENT INPUT =====
            // Get WASD or arrow keys (-1 to 1 for each axis)
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");

            // Normalize diagonal movement
            // Without this, diagonal = 1.414 speed (too fast)
            if (moveInput.magnitude > 1f)
            {
                moveInput.Normalize();
            }

            // Remember direction for dodging when standing still
            if (moveInput.magnitude > 0.1f)
            {
                lastMoveDirection = moveInput;
            }

            // ===== DODGE INPUT =====
            // Left Shift on keyboard OR Right Trigger on controller
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Joystick1Button5))
            {
                TryDodge();
            }

            // ===== DEBUG CHEATS =====
            // Only works in debug mode
            if (Constants.DEBUG_MODE && Input.GetKeyDown(KeyCode.E))
            {
                EventManager.TriggerEvent("AddEnergy", 10f);
            }
        }

        /// <summary>
        /// Updates all countdown timers
        /// Called every frame
        /// </summary>
        private void UpdateTimers()
        {
            // Count down dodge cooldown
            if (dodgeCooldownTimer > 0)
            {
                dodgeCooldownTimer -= Time.deltaTime;
            }

            // Count down dodge duration
            if (currentState == PlayerState.Dodging)
            {
                dodgeTimer -= Time.deltaTime;
                if (dodgeTimer <= 0)
                {
                    EndDodge(); // Dodge finished
                }
            }
        }

        /// <summary>
        /// Updates player state based on current input
        /// States determine what actions are available
        /// </summary>
        private void UpdateState()
        {
            // Don't change state if dead or dodging
            if (currentState == PlayerState.Dead || currentState == PlayerState.Dodging)
                return;

            // Set state based on movement
            if (IsMoving)
            {
                currentState = PlayerState.Moving;
            }
            else
            {
                currentState = PlayerState.Idle;
            }
        }

        /// <summary>
        /// Applies movement based on current state
        /// Called in FixedUpdate for smooth physics
        /// </summary>
        private void HandleMovement()
        {
            switch (currentState)
            {
                case PlayerState.Idle:
                    rb.linearVelocity = Vector3.zero;
                    break;

                case PlayerState.Moving:
                    // Direct mapping - adjust these if movement feels wrong
                    Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed;
                    rb.linearVelocity = movement;
                    break;

                case PlayerState.Dodging:
                    Vector3 dodgeMovement = new Vector3(dodgeDirection.x, 0, dodgeDirection.y) * dodgeSpeed;
                    rb.linearVelocity = dodgeMovement;
                    break;

                case PlayerState.Dead:
                    rb.linearVelocity = Vector3.zero;
                    break;
            }
        }
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
            rb.linearVelocity = Vector2.zero;
            moveInput = Vector2.zero;
        }

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

            // Notify other systems (for sound effects, etc.)
            EventManager.TriggerEvent("PlayerDodged", dodgeDirection);

            if (Constants.DEBUG_MODE)
            {
                Debug.Log("Player dodged!");
            }
        }

        /// <summary>
        /// Called when dodge roll completes
        /// Returns player to normal state
        /// </summary>
        private void EndDodge()
        {
            currentState = PlayerState.Idle;
            canBeTargeted = true; // Can be hit again

            // Reset visual to fully opaque
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }

        /// <summary>
        /// Called when player health reaches 0
        /// Triggers game over sequence
        /// </summary>
        protected override void HandleDeath()
        {
            currentState = PlayerState.Dead;
            StopMovement();

            // Wait 2 seconds then trigger game over
            Invoke(nameof(TriggerGameOver), 2f);
        }

        /// <summary>
        /// Tells GameManager to show game over screen
        /// </summary>
        private void TriggerGameOver()
        {
            GameManager.Instance.GameOver();
        }

        /// <summary>
        /// Called when player starts casting a card
        /// Prevents movement during cast
        /// </summary>
        public void StartCasting(float castTime)
        {
            // Can't cast while dead or dodging
            if (currentState == PlayerState.Dead || currentState == PlayerState.Dodging)
                return;

            currentState = PlayerState.Casting;
            canMove = false; // Lock movement
            Invoke(nameof(EndCasting), castTime); // Auto-end after cast time
        }

        /// <summary>
        /// Called when card cast completes
        /// Returns control to player
        /// </summary>
        private void EndCasting()
        {
            if (currentState == PlayerState.Casting)
            {
                currentState = PlayerState.Idle;
                canMove = true; // Unlock movement
            }
        }

        /// <summary>
        /// Debug visualization in Scene view
        /// Shows movement direction and dodge cooldown
        /// </summary>
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos(); // Draw health bar from parent

            if (!Application.isPlaying) return;

            // ===== DRAW MOVEMENT DIRECTION =====
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, new Vector3(moveInput.x, 0, moveInput.y));

            // ===== DRAW DODGE COOLDOWN =====
            if (dodgeCooldownTimer > 0)
            {
                Gizmos.color = Color.yellow;
                float cooldownPercent = dodgeCooldownTimer / dodgeCooldown;
                Vector3 indicatorPos = transform.position + Vector3.up * 0.5f;

                // Draw shrinking circle to show cooldown
                Gizmos.DrawWireSphere(indicatorPos, 0.2f * cooldownPercent);
            }
        }
    }
}