using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>
    /// Keeps the woodland loop warm during exploration, then crossfades to a slightly faster
    /// rendering of that same CC0 loop only while a cutter has direct sight and is chasing.
    /// Using one composition for both layers keeps the switch in the game's gentle low-poly
    /// storybook mood instead of suddenly becoming a horror or combat soundtrack.
    /// </summary>
    public sealed class AdaptiveForestMusic : MonoBehaviour
    {
        [SerializeField] private AudioSource calmSource;
        [SerializeField] private AudioSource chaseSource;
        [SerializeField, Range(0f, 1f)] private float calmVolume = .34f;
        [SerializeField, Range(0f, 1f)] private float chaseVolume = .42f;
        [SerializeField, Min(.05f)] private float crossfadeSeconds = .65f;
        [SerializeField, Range(1f, 1.35f)] private float chasePitch = 1.14f;

        private float chaseMix;
        private bool forcedChaseForEditor;
        private bool useForcedChaseForEditor;

        public bool IsChased { get; private set; }
        public float ChaseMix => chaseMix;
        public float CalmOutputVolume => calmSource == null ? 0f : calmSource.volume;
        public float ChaseOutputVolume => chaseSource == null ? 0f : chaseSource.volume;

        private void Awake()
        {
            ConfigureSource(calmSource, 1f);
            ConfigureSource(chaseSource, chasePitch);
            chaseMix = 0f;
            ApplyMix();
        }

        private void OnEnable()
        {
            StartIfNeeded(calmSource);
            StartIfNeeded(chaseSource);
        }

        private void Update()
        {
            IsChased = useForcedChaseForEditor ? forcedChaseForEditor : AnyCutterHasDirectChase();
            float targetMix = IsChased ? 1f : 0f;
            chaseMix = Mathf.MoveTowards(chaseMix, targetMix, Time.unscaledDeltaTime / crossfadeSeconds);
            ApplyMix();
        }

        private bool AnyCutterHasDirectChase()
        {
            WoodcutterAI[] cutters = FindObjectsByType<WoodcutterAI>();
            foreach (WoodcutterAI cutter in cutters)
            {
                if (cutter != null && cutter.isActiveAndEnabled &&
                    cutter.State == WoodcutterAI.AlertState.Chase && cutter.HasDirectSight)
                    return true;
            }
            return false;
        }

        private void ConfigureSource(AudioSource source, float pitch)
        {
            if (source == null) return;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.pitch = pitch;
        }

        private static void StartIfNeeded(AudioSource source)
        {
            if (source != null && source.clip != null && !source.isPlaying) source.Play();
        }

        private void ApplyMix()
        {
            if (calmSource != null) calmSource.volume = calmVolume * (1f - chaseMix);
            if (chaseSource != null) chaseSource.volume = chaseVolume * chaseMix;
        }

        // Deterministic verification seam. Production always derives this from WoodcutterAI.
        public void SetChaseForEditor(bool chased)
        {
            useForcedChaseForEditor = true;
            forcedChaseForEditor = chased;
        }

        public void ClearEditorChaseOverride()
        {
            useForcedChaseForEditor = false;
        }
    }
}
