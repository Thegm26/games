using System;
using System.Collections.Generic;
using UnityEngine;
using WhoEnters.Audio;
using WhoEnters.Core;

namespace WhoEnters.Gameplay
{
    /// <summary>Original deterministic procedural audio. Clips and sources are created only after a deliberate interaction.</summary>
    public sealed class GameAudio : MonoBehaviour
    {
        public const string MutePreferenceKey = "who-enters.muted";
        private const int SfxVoiceCount = 4;
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.72f;
        [SerializeField, Range(0f, 1f)] private float interactionVolume = 0.86f;
        [SerializeField, Range(0f, 1f)] private float verdictVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float feedbackVolume = .94f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = .78f;
        [SerializeField, Range(0f, 1f)] private float endingVolume = 1f;

        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private readonly AudioPlaybackPolicy playbackPolicy = new AudioPlaybackPolicy();
        private AudioSource ambientSource;
        private AudioSource[] sfxSources;
        private int sourceCursor;
        private bool initialized;
        private bool traceSubscribed;
        public bool IsMuted { get; private set; }
        private bool interactionUnlocked;
        public bool IsInteractionUnlocked => interactionUnlocked;
        public int CachedClipCount => clips.Count;
        public AudioPlaybackPolicy PlaybackPolicy => playbackPolicy;

        private void Awake()
        {
            if (initialized) return;
            initialized = true;
            IsMuted = PlayerPrefs.GetInt(MutePreferenceKey, 0) == 1;
            AudioListener.pause = IsMuted;
            DebugTrace.Log("audio.init", $"catalog={AudioCueCatalog.All.Count};sampleRate={ProceduralAudioSynthesis.DefaultSampleRate};muted={IsMuted};activated=false");
            DebugTrace.Log("asset.audio_loaded", "count=0;provider=procedural-original;deferred=true");
            SubscribeToTrace();
        }

        private void OnEnable()
        {
            if (initialized) SubscribeToTrace();
        }

        private void OnDisable()
        {
            UnsubscribeFromTrace();
        }

        private void OnDestroy()
        {
            UnsubscribeFromTrace();
            if (AudioListener.pause == IsMuted) AudioListener.pause = false;
            foreach (var clip in clips.Values)
                if (clip != null)
                {
                    if (Application.isPlaying) Destroy(clip);
                    else DestroyImmediate(clip);
                }
            clips.Clear();
        }

        private void SubscribeToTrace()
        {
            if (traceSubscribed) return;
            DebugTrace.Recorded += HandleTrace;
            traceSubscribed = true;
        }

        private void UnsubscribeFromTrace()
        {
            if (!traceSubscribed) return;
            DebugTrace.Recorded -= HandleTrace;
            traceSubscribed = false;
        }

        /// <summary>Call only from a pointer/key/button action. This is the sole point ambient playback may begin.</summary>
        public void UnlockFromInteraction()
        {
            if (interactionUnlocked) return;
            interactionUnlocked = true;
            EnsureSources();
            DebugTrace.Log("audio.activation", "source=user_interaction;ambient=eligible");
            if (IsMuted)
            {
                DebugTrace.Log("audio.suppressed", "cue=ambient.castle_wind;reason=muted");
                return;
            }
            StartAmbient();
        }

        public void NotifyUiInteraction()
        {
            UnlockFromInteraction();
            Play(AudioCueIds.UiClick);
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            AudioListener.pause = IsMuted;
            PlayerPrefs.SetInt(MutePreferenceKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            DebugTrace.Log("persistence.saved", $"key={MutePreferenceKey};value={IsMuted}");
            DebugTrace.Log("audio.mute", $"muted={IsMuted}");
            if (!IsMuted && interactionUnlocked) StartAmbient();
        }

        public void Play(string cueId)
        {
            if (!AudioCueCatalog.TryGet(cueId, out var spec))
            {
                DebugTrace.Error("audio.missing", "cue=" + cueId);
                return;
            }
            if (!interactionUnlocked)
            {
                DebugTrace.Log("audio.suppressed", $"cue={cueId};reason=not_activated");
                return;
            }
            if (IsMuted)
            {
                DebugTrace.Log("audio.suppressed", $"cue={cueId};reason=muted");
                return;
            }
            if (!playbackPolicy.TryAuthorize(spec, Time.unscaledTime, out var reason))
            {
                DebugTrace.Log("audio.suppressed", $"cue={cueId};reason={reason}");
                return;
            }
            EnsureSources();
            if (spec.Loop)
            {
                StartAmbient();
                return;
            }
            var source = NextSource();
            source.clip = GetOrCreateClip(spec);
            source.loop = false;
            source.volume = CategoryVolume(spec.Category);
            source.Play();
            DebugTrace.Log("audio.play", $"cue={cueId};category={spec.Category};voices={playbackPolicy.ActiveVoices(cueId, Time.unscaledTime)}");
        }

        public void PlayEnding(EndingKind ending)
        {
            switch (ending)
            {
                case EndingKind.CastleFallen: Play(AudioCueIds.EndingFallen); break;
                case EndingKind.HollowVictory: Play(AudioCueIds.EndingHollow); break;
                case EndingKind.GateHeld: Play(AudioCueIds.EndingHeld); break;
                case EndingKind.TheGateRemembers: Play(AudioCueIds.EndingSecret); break;
                default: DebugTrace.Error("audio.missing", "ending=" + ending); break;
            }
        }

        private void HandleTrace(TraceEvent trace)
        {
            // SwipeCard writes input.drag_start synchronously from IPointerDownHandler, so this
            // remains within the browser/mobile user gesture that is allowed to start audio.
            if (AudioCueRouter.IsGestureActivationEvent(trace.EventId)) UnlockFromInteraction();
            foreach (var cueId in AudioCueRouter.Route(trace.EventId, trace.Payload)) Play(cueId);
        }

        private void StartAmbient()
        {
            EnsureSources();
            if (ambientSource.isPlaying) return;
            if (!AudioCueCatalog.TryGet(AudioCueIds.AmbientWind, out var spec))
            {
                DebugTrace.Error("audio.missing", "cue=" + AudioCueIds.AmbientWind);
                return;
            }
            ambientSource.clip = GetOrCreateClip(spec);
            ambientSource.loop = true;
            ambientSource.volume = CategoryVolume(spec.Category);
            ambientSource.Play();
            DebugTrace.Log("audio.play", "cue=ambient.castle_wind;category=Ambient;loop=true");
        }

        private void EnsureSources()
        {
            if (ambientSource != null) return;
            ambientSource = CreateSource("Ambient", true);
            sfxSources = new AudioSource[SfxVoiceCount];
            for (var index = 0; index < sfxSources.Length; index++) sfxSources[index] = CreateSource("Sfx " + index, false);
            DebugTrace.Log("audio.sources_ready", $"ambient=1;sfx={SfxVoiceCount}");
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.name = "WhoEnters " + sourceName;
            return source;
        }

        private AudioSource NextSource()
        {
            for (var attempt = 0; attempt < sfxSources.Length; attempt++)
            {
                var source = sfxSources[(sourceCursor + attempt) % sfxSources.Length];
                if (!source.isPlaying)
                {
                    sourceCursor = (sourceCursor + attempt + 1) % sfxSources.Length;
                    return source;
                }
            }
            var fallback = sfxSources[sourceCursor];
            sourceCursor = (sourceCursor + 1) % sfxSources.Length;
            return fallback;
        }

        private AudioClip GetOrCreateClip(AudioCueSpec spec)
        {
            if (clips.TryGetValue(spec.Id, out var clip) && clip != null) return clip;
            clip = ProceduralAudioSynthesis.CreateClip(spec);
            clips[spec.Id] = clip;
            DebugTrace.Log("audio.clip_generated", $"cue={spec.Id};samples={clip.samples};channels={clip.channels};frequency={clip.frequency};loop={spec.Loop}");
            return clip;
        }

        private float CategoryVolume(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Ambient: return ambientVolume;
                case AudioCategory.Interaction: return interactionVolume;
                case AudioCategory.Verdict: return verdictVolume;
                case AudioCategory.Feedback: return feedbackVolume;
                case AudioCategory.Ui: return uiVolume;
                case AudioCategory.Ending: return endingVolume;
                default: return 1f;
            }
        }
    }
}
