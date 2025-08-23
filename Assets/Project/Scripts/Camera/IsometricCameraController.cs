using UnityEngine;
using Aeloria.Core;

namespace Aeloria.Camera
{
    /// <summary>
    /// Isometric camera that follows the player with smooth movement
    /// Maintains proper isometric angle and handles boundaries
    /// </summary>
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Isometric Settings")]
        [SerializeField] private float cameraAngleX = 30f;  // Classic isometric uses 30-35 degrees
        [SerializeField] private float cameraAngleY = 45f;  // 45 degrees for true isometric
        [SerializeField] private float cameraDistance = 10f;

        [Header("Follow Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Follow Behavior")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
        [SerializeField] private bool lockYPosition = false;
        [SerializeField] private float fixedYPosition = 10f;

        [Header("Camera Bounds")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-20, -20);
        [SerializeField] private Vector2 maxBounds = new Vector2(20, 20);

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Internal state
        private Vector3 desiredPosition;
        private Vector3 smoothedPosition;
        private bool isInitialized = false;

        private void Start()
        {
            InitializeCamera();

            // Listen for player spawn event
            EventManager.StartListening("PlayerSpawned", OnPlayerSpawned);
        }

        private void OnDestroy()
        {
            EventManager.StopListening("PlayerSpawned", OnPlayerSpawned);
        }

        /// <summary>
        /// Set up the camera for isometric view
        /// </summary>
        private void InitializeCamera()
        {
            // Set the rotation for isometric view
            transform.rotation = Quaternion.Euler(cameraAngleX, cameraAngleY, 0);

            // Try to find player if auto-find is enabled
            if (autoFindPlayer && target == null)
            {
                FindPlayer();
            }

            // Set initial position if we have a target
            if (target != null)
            {
                SnapToTarget();
            }

            // Ensure camera component settings are correct
            UnityEngine.Camera cam = GetComponent<UnityEngine.Camera>();
            if (cam != null)
            {
                // Orthographic is often better for isometric
                if (cam.orthographic)
                {
                    cam.orthographicSize = cameraDistance;
                }
            }

            isInitialized = true;
            Debug.Log("Isometric Camera initialized with angle: " + cameraAngleX + ", " + cameraAngleY);
        }

        /// <summary>
        /// Called when player spawns
        /// </summary>
        private void OnPlayerSpawned(object data)
        {
            GameObject player = data as GameObject;
            if (player != null)
            {
                SetTarget(player.transform);
                Debug.Log("Camera found spawned player!");
            }
        }

        /// <summary>
        /// Find the player in the scene
        /// </summary>
        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(Constants.TAG_PLAYER);
            if (playerObj != null)
            {
                target = playerObj.transform;
                Debug.Log("Camera found player with tag!");
            }
            else
            {
                // Try to find by component
                var playerController = FindFirstObjectByType<Entities.Player.PlayerController>();
                if (playerController != null)
                {
                    target = playerController.transform;
                    Debug.Log("Camera found player by component!");
                }
            }
        }

        /// <summary>
        /// Immediately position camera at target
        /// </summary>
        private void SnapToTarget()
        {
            if (target == null) return;

            Vector3 targetPosition = CalculateDesiredPosition();
            transform.position = targetPosition;
        }

        private void LateUpdate()
        {
            if (!isInitialized) return;

            // Try to find player if we don't have one
            if (target == null && autoFindPlayer)
            {
                FindPlayer();
                if (target == null) return;
            }

            FollowTarget();
        }

        /// <summary>
        /// Smoothly follow the target
        /// </summary>
        private void FollowTarget()
        {
            if (target == null) return;

            // Calculate where we want to be
            desiredPosition = CalculateDesiredPosition();

            // Apply bounds if enabled
            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
                desiredPosition.z = Mathf.Clamp(desiredPosition.z, minBounds.y, maxBounds.y);
            }

            // Smooth movement
            smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }

        /// <summary>
        /// Calculate the desired camera position based on target
        /// </summary>
        private Vector3 CalculateDesiredPosition()
        {
            Vector3 targetPos = target.position + offset;

            // For isometric, we need to position the camera at an angle
            Vector3 direction = Quaternion.Euler(cameraAngleX, cameraAngleY, 0) * Vector3.back;
            Vector3 cameraPos = target.position + direction * cameraDistance;

            // Lock Y if requested
            if (lockYPosition)
            {
                cameraPos.y = fixedYPosition;
            }

            return cameraPos;
        }

        /// <summary>
        /// Set a new target for the camera to follow
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null && isInitialized)
            {
                SnapToTarget();
            }
        }

        /// <summary>
        /// Shake the camera (for impacts, etc)
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        private System.Collections.IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            float elapsed = 0;
            Vector3 originalPos = transform.position;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;

                transform.position = new Vector3(
                    originalPos.x + x,
                    originalPos.y + y,
                    originalPos.z
                );

                elapsed += Time.deltaTime;
                intensity *= 0.95f; // Decay intensity

                yield return null;
            }

            transform.position = originalPos;
        }

        /// <summary>
        /// Draw debug information in Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showDebugInfo) return;

            // Draw camera bounds
            if (useBounds)
            {
                Gizmos.color = Color.yellow;

                float y = transform.position.y;
                Vector3[] corners = new Vector3[]
                {
                    new Vector3(minBounds.x, y, minBounds.y),
                    new Vector3(maxBounds.x, y, minBounds.y),
                    new Vector3(maxBounds.x, y, maxBounds.y),
                    new Vector3(minBounds.x, y, maxBounds.y)
                };

                Gizmos.DrawLine(corners[0], corners[1]);
                Gizmos.DrawLine(corners[1], corners[2]);
                Gizmos.DrawLine(corners[2], corners[3]);
                Gizmos.DrawLine(corners[3], corners[0]);
            }

            // Draw line to target
            if (target != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}