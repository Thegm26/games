using UnityEngine;
using UnityEngine.InputSystem;

namespace BeforeTheAxes
{
    public sealed class ThirdPersonForestCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float distance = 5.5f;
        [SerializeField] private float legacyMouseAxisScale = 0.1f;
        [SerializeField] private float legacySensitivity = 0.8f;
        [SerializeField] private LayerMask collisionMask = ~0;
        private float yaw;
        private float pitch;

        public void SetTarget(Transform value)
        {
            target = value;
            if (target == null) return;

            yaw = target.eulerAngles.y;
            UpdateCameraTransform();
        }

        public void SetFraming(float pivotHeight, float cameraDistance)
        {
            pivotOffset = new Vector3(0f, pivotHeight, 0f);
            distance = cameraDistance;
        }

        private void OnEnable()
        {
            if (Application.isPlaying) LockCursor();
        }

        private void Start()
        {
            if (Application.isPlaying) LockCursor();
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                UpdateCameraTransform();
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector2 mouseDelta = Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue();
            yaw += mouseDelta.x * legacyMouseAxisScale * legacySensitivity;
            pitch = Mathf.Clamp(pitch - mouseDelta.y * legacyMouseAxisScale * legacySensitivity, -20f, 65f);
            UpdateCameraTransform();
        }

        private void UpdateCameraTransform()
        {
            Vector3 pivot = target.position + pivotOffset;
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            float correctedDistance = distance;
            if (Physics.SphereCast(pivot, 0.22f, rotation * Vector3.back, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
                correctedDistance = Mathf.Max(0.8f, hit.distance - 0.15f);
            transform.SetPositionAndRotation(pivot + rotation * Vector3.back * correctedDistance, rotation);
        }

        private static void LockCursor()
        {
            if (!Application.isPlaying) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
