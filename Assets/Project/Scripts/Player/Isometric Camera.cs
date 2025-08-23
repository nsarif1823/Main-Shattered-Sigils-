using UnityEngine;

namespace AshesOfAeloria.Camera
{
    public class IsometricCamera : MonoBehaviour
    {
        [Header("Isometric Settings")]
        [SerializeField] private float cameraDistance = 10f;
        [SerializeField] private float cameraHeight = 10f;
        [SerializeField] private float isometricAngle = 35.264f; // Classic isometric angle

        [Header("Follow Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

        [Header("Camera Bounds (Optional)")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 minBounds;
        [SerializeField] private Vector2 maxBounds;

        private Vector3 velocity = Vector3.zero;

        void Start()
        {
            SetupIsometricView();
        }

        void SetupIsometricView()
        {
            // Set rotation for isometric view
            transform.rotation = Quaternion.Euler(isometricAngle, 45f, 0);

            // Position camera at proper distance
            if (target != null)
            {
                Vector3 desiredPosition = target.position +
                    Quaternion.Euler(isometricAngle, 45f, 0) *
                    new Vector3(0, 0, -cameraDistance);
                desiredPosition.y = target.position.y + cameraHeight;
                transform.position = desiredPosition;
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            FollowTarget();
        }

        void FollowTarget()
        {
            // Calculate desired position
            Vector3 desiredPosition = target.position + offset;
            desiredPosition.y = target.position.y + cameraHeight;

            // Apply bounds if enabled
            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
                desiredPosition.z = Mathf.Clamp(desiredPosition.z, minBounds.y, maxBounds.y);
            }

            // Smooth camera movement
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition,
                smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        // For debugging in Scene view
        void OnDrawGizmosSelected()
        {
            if (useBounds)
            {
                Gizmos.color = Color.yellow;
                Vector3 bottomLeft = new Vector3(minBounds.x, transform.position.y, minBounds.y);
                Vector3 topRight = new Vector3(maxBounds.x, transform.position.y, maxBounds.y);

                Gizmos.DrawWireCube(
                    (bottomLeft + topRight) / 2,
                    new Vector3(maxBounds.x - minBounds.x, 0.1f, maxBounds.y - minBounds.y)
                );
            }
        }
    }
}