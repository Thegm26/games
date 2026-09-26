using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>Small, self-contained presentation motion for the menu only.</summary>
    public sealed class MainMenuPresentation : MonoBehaviour
    {
        [SerializeField] private AudioSource themeMusic;
        [SerializeField, Range(0f, 1f)] private float themeVolume = .24f;
        [SerializeField, Min(.01f)] private float themeFadeSeconds = 1.6f;
        [SerializeField] private Animator guardianAnimator;
        [SerializeField] private Animator[] woodcutterAnimators;
        [SerializeField] private Transform[] woodcutterRoots;

        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Quaternion[] woodcutterBaseRotations;
        private float startedAt;

        public Vector3 BaseCameraPosition => basePosition;
        public float CameraOffsetMagnitude => Vector3.Distance(transform.position, basePosition);
        public float ThemeVolume => themeMusic != null ? themeMusic.volume : 0f;
        public bool ThemeIsLooping => themeMusic != null && themeMusic.loop && themeMusic.isPlaying;

        private void Awake()
        {
            basePosition = transform.position;
            baseRotation = transform.rotation;
            woodcutterBaseRotations = new Quaternion[woodcutterRoots == null ? 0 : woodcutterRoots.Length];
            for (int i = 0; i < woodcutterBaseRotations.Length; i++)
                if (woodcutterRoots[i] != null) woodcutterBaseRotations[i] = woodcutterRoots[i].rotation;
        }

        private void Start()
        {
            startedAt = Time.unscaledTime;
            if (themeMusic != null)
            {
                themeMusic.loop = true;
                themeMusic.spatialBlend = 0f;
                themeMusic.volume = 0f;
                themeMusic.Play();
            }

            PlayIdle(guardianAnimator, 0f);
            if (woodcutterAnimators != null)
                for (int i = 0; i < woodcutterAnimators.Length; i++) PlayIdle(woodcutterAnimators[i], .18f * (i + 1));
        }

        private void Update()
        {
            float time = Time.unscaledTime - startedAt;
            // Less than 16 cm of travel and less than one degree of rotation: atmosphere, not camera movement.
            transform.position = basePosition + new Vector3(Mathf.Sin(time * .23f) * .11f, Mathf.Sin(time * .37f) * .035f, Mathf.Cos(time * .19f) * .07f);
            transform.rotation = baseRotation * Quaternion.Euler(Mathf.Sin(time * .29f) * .22f, Mathf.Sin(time * .21f) * .65f, 0f);

            if (themeMusic != null)
                themeMusic.volume = Mathf.SmoothStep(0f, themeVolume, Mathf.Clamp01(time / themeFadeSeconds));

            if (woodcutterRoots != null)
                for (int i = 0; i < woodcutterRoots.Length; i++)
                    if (woodcutterRoots[i] != null)
                        woodcutterRoots[i].rotation = woodcutterBaseRotations[i] * Quaternion.Euler(0f, Mathf.Sin(time * (.38f + i * .07f) + i) * 1.8f, 0f);
        }

        private static void PlayIdle(Animator animator, float normalizedOffset)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.speed = .9f;
            animator.SetFloat("Speed", 0f);
            animator.Play("Base Layer.ITHappy Idle", 0, normalizedOffset);
            animator.Update(0f);
        }
    }
}
