using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

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
        [Header("Tree Occlusion")]
        [SerializeField] private bool hideOccludingTrees = true;
        [SerializeField] private float occlusionRadius = 0.18f;
        [SerializeField] private string treeNameToken = "tree";
        private float yaw;
        private float pitch;
        private readonly RaycastHit[] occlusionHits = new RaycastHit[32];
        private readonly HashSet<Renderer> hiddenTreeRenderers = new HashSet<Renderer>();
        private readonly HashSet<Renderer> requiredHiddenRenderers = new HashSet<Renderer>();

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
            RestoreHiddenTrees();
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
            UpdateTreeOcclusion(pivot);
        }

        // Trees remain physically solid.  Only their renderers are temporarily disabled while
        // they occupy the line between the active camera and the guardian.
        private void UpdateTreeOcclusion(Vector3 pivot)
        {
            if (!hideOccludingTrees)
            {
                RestoreHiddenTrees();
                return;
            }

            requiredHiddenRenderers.Clear();
            Vector3 origin = transform.position;
            Vector3 toPivot = pivot - origin;
            float length = toPivot.magnitude;
            if (length > 0.01f)
            {
                int count = Physics.SphereCastNonAlloc(origin, occlusionRadius, toPivot / length, occlusionHits,
                    length, collisionMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    Collider collider = occlusionHits[i].collider;
                    if (collider == null || !TryGetTreeRoot(collider.transform, out Transform treeRoot)) continue;
                    Renderer[] renderers = treeRoot.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null) requiredHiddenRenderers.Add(renderer);
                    }
                }
            }

            foreach (Renderer renderer in hiddenTreeRenderers)
            {
                if (renderer != null && !requiredHiddenRenderers.Contains(renderer)) renderer.enabled = true;
            }
            hiddenTreeRenderers.RemoveWhere(renderer => renderer == null || !requiredHiddenRenderers.Contains(renderer));

            foreach (Renderer renderer in requiredHiddenRenderers)
            {
                if (renderer == null) continue;
                renderer.enabled = false;
                hiddenTreeRenderers.Add(renderer);
            }
        }

        private bool TryGetTreeRoot(Transform transformToCheck, out Transform treeRoot)
        {
            treeRoot = null;
            if (string.IsNullOrWhiteSpace(treeNameToken)) return false;

            for (Transform current = transformToCheck; current != null; current = current.parent)
            {
                if (current.name.IndexOf(treeNameToken, StringComparison.OrdinalIgnoreCase) < 0) continue;
                treeRoot = current;
            }
            return treeRoot != null;
        }

        private void RestoreHiddenTrees()
        {
            foreach (Renderer renderer in hiddenTreeRenderers)
            {
                if (renderer != null) renderer.enabled = true;
            }
            hiddenTreeRenderers.Clear();
            requiredHiddenRenderers.Clear();
        }

        private static void LockCursor()
        {
            if (!Application.isPlaying) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
