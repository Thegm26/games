using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>Plays one soft, non-spatial confirmation when the guardian enters tree form.</summary>
    [RequireComponent(typeof(ForestGuardianController))]
    public sealed class TreeTransformSfx : MonoBehaviour
    {
        [SerializeField] private AudioSource uiSoundSource;
        [SerializeField] private AudioClip transformSound;
        [SerializeField, Range(0f, 1f)] private float volume = .5f;

        private ForestGuardianController guardian;

        private void Awake()
        {
            guardian = GetComponent<ForestGuardianController>();
        }

        private void OnEnable()
        {
            if (guardian == null) guardian = GetComponent<ForestGuardianController>();
            if (guardian != null) guardian.TreeFormEntering += PlayTreeEntrySound;
        }

        private void OnDisable()
        {
            if (guardian != null) guardian.TreeFormEntering -= PlayTreeEntrySound;
        }

        private void PlayTreeEntrySound()
        {
            // This event is only emitted for a successful human-to-tree entry, before the visual
            // swap. Returning to human form and stamina-forced exits stay quiet.
            if (transformSound == null || uiSoundSource == null) return;
            uiSoundSource.PlayOneShot(transformSound, volume);
        }
    }
}
