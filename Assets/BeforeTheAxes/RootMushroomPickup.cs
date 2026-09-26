using System;
using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>A single physical objective the guardian can collect by walking close to it.</summary>
    public sealed class RootMushroomPickup : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float pickupRadius = 1.35f;
        [SerializeField] private float turnDegreesPerSecond = 120f;
        [SerializeField] private float bobHeight = .09f;
        [SerializeField] private float bobCyclesPerSecond = .85f;
        [Header("Audio")]
        [SerializeField] private AudioClip pickupSound;
        [SerializeField, Range(0f, 1f)] private float pickupSoundVolume = .72f;
        [SerializeField] private AudioSource uiSoundSource;

        private ForestGuardianController player;
        private Vector3 restingPosition;
        private bool collected;

        public static event Action<RootMushroomPickup> Collected;
        public bool IsCollected => collected;
        public float PickupRadius => pickupRadius;

        private void Awake()
        {
            restingPosition = transform.position;
            player = FindFirstObjectByType<ForestGuardianController>();
        }

        private void Update()
        {
            if (collected) return;
            transform.Rotate(Vector3.up, turnDegreesPerSecond * Time.deltaTime, Space.World);
            float bob = Mathf.Sin(Time.time * bobCyclesPerSecond * Mathf.PI * 2f) * bobHeight;
            transform.position = restingPosition + Vector3.up * bob;

            if (player == null) player = FindFirstObjectByType<ForestGuardianController>();
            if (player == null) return;
            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= pickupRadius * pickupRadius) Collect();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            ForestGuardianController guardian = other.GetComponentInParent<ForestGuardianController>();
            if (guardian != null) Collect();
        }

        public void Collect()
        {
            if (collected) return;
            collected = true;
            if (pickupSound != null && uiSoundSource != null)
                uiSoundSource.PlayOneShot(pickupSound, pickupSoundVolume);
            Collected?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
