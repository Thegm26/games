using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WhoEnters.Audio;
using WhoEnters.Core;
using WhoEnters.Gameplay;

namespace WhoEnters.Tests.Audio
{
    public sealed class AudioSystemTests
    {
        private static readonly string[] ExpectedCueIds =
        {
            AudioCueIds.AmbientWind, AudioCueIds.DayTransition, AudioCueIds.GateOpen,
            AudioCueIds.CardPickup, AudioCueIds.CardDrag, AudioCueIds.CardSnapback,
            AudioCueIds.VerdictAdmit, AudioCueIds.VerdictDeny, AudioCueIds.VerdictStamp,
            AudioCueIds.IntegrityCrack, AudioCueIds.StreakChime, AudioCueIds.EndingFallen,
            AudioCueIds.EndingHollow, AudioCueIds.EndingHeld, AudioCueIds.EndingSecret,
            AudioCueIds.UiClick
        };

        [SetUp]
        public void SetUp()
        {
            DebugTrace.Enabled = true;
            DebugTrace.Clear();
            PlayerPrefs.DeleteKey(GameAudio.MutePreferenceKey);
            PlayerPrefs.Save();
            AudioListener.pause = false;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(GameAudio.MutePreferenceKey);
            PlayerPrefs.Save();
            AudioListener.pause = false;
            DebugTrace.Clear();
        }

        [Test]
        public void CatalogHasEveryRequiredOriginalCueExactlyOnce()
        {
            Assert.That(AudioCueCatalog.All.Keys.OrderBy(value => value), Is.EqualTo(ExpectedCueIds.OrderBy(value => value)));
            Assert.That(AudioCueCatalog.All.Values.All(spec => spec.DurationSeconds > 0f && spec.Gain > 0f && spec.MaxPolyphony > 0), Is.True);
            Assert.That(AudioCueCatalog.All[AudioCueIds.AmbientWind].Loop, Is.True);
            Assert.That(AudioCueCatalog.All.Values.Count(spec => spec.Loop), Is.EqualTo(1));
        }

        [Test]
        public void EveryCueHasDeterministicSafeMonoPcmAndExpectedClipMetadata()
        {
            foreach (var spec in AudioCueCatalog.All.Values)
            {
                var first = ProceduralAudioSynthesis.Generate(spec);
                var second = ProceduralAudioSynthesis.Generate(spec);
                var expectedSamples = Mathf.RoundToInt(spec.DurationSeconds * ProceduralAudioSynthesis.DefaultSampleRate);
                var peak = first.Max(sample => Mathf.Abs(sample));
                var rms = Mathf.Sqrt(first.Average(sample => sample * sample));
                var dc = Mathf.Abs(first.Average());

                Assert.That(first.Length, Is.EqualTo(expectedSamples), spec.Id + " sample count");
                Assert.That(first, Is.EqualTo(second), spec.Id + " deterministic PCM");
                Assert.That(first.All(sample => !float.IsNaN(sample) && !float.IsInfinity(sample)), Is.True, spec.Id + " finite PCM");
                Assert.That(peak, Is.LessThanOrEqualTo(ProceduralAudioSynthesis.MaximumPeak + .0001f), spec.Id + " peak ceiling");
                Assert.That(rms, Is.GreaterThan(.0005f).And.LessThan(.5f), spec.Id + " usable RMS");
                Assert.That(dc, Is.LessThan(.15f), spec.Id + " bounded DC");
                if (!spec.Loop)
                {
                    Assert.That(Mathf.Abs(first[0]), Is.LessThan(.0001f), spec.Id + " attack fade");
                    Assert.That(Mathf.Abs(first[first.Length - 1]), Is.LessThan(.0001f), spec.Id + " release fade");
                }

                var clip = ProceduralAudioSynthesis.CreateClip(spec);
                Assert.That(clip.samples, Is.EqualTo(expectedSamples), spec.Id + " clip samples");
                Assert.That(clip.frequency, Is.EqualTo(ProceduralAudioSynthesis.DefaultSampleRate), spec.Id + " clip rate");
                Assert.That(clip.channels, Is.EqualTo(ProceduralAudioSynthesis.Channels), spec.Id + " clip channels");
                Assert.That(clip.length, Is.EqualTo(spec.DurationSeconds).Within(.001f), spec.Id + " clip duration");
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void AmbientLoopHasAQuietContinuousSeam()
        {
            var samples = ProceduralAudioSynthesis.Generate(AudioCueCatalog.All[AudioCueIds.AmbientWind]);
            var seamDelta = Mathf.Abs(samples[0] - samples[samples.Length - 1]);
            var startSlope = Mathf.Abs(samples[1] - samples[0]);
            var endSlope = Mathf.Abs(samples[samples.Length - 1] - samples[samples.Length - 2]);
            Assert.That(seamDelta, Is.LessThan(.02f));
            Assert.That(Mathf.Abs(startSlope - endSlope), Is.LessThan(.02f));
        }

        [Test]
        public void CardArrivalIsSoftFadedAndConsonantRatherThanAHarshCreak()
        {
            var spec = AudioCueCatalog.All[AudioCueIds.GateOpen];
            var samples = ProceduralAudioSynthesis.Generate(spec);
            var peak = samples.Max(sample => Mathf.Abs(sample));
            var rms = Mathf.Sqrt(samples.Average(sample => sample * sample));
            var dc = Mathf.Abs(samples.Average());
            var attackSamples = Mathf.RoundToInt(.02f * ProceduralAudioSynthesis.DefaultSampleRate);
            var firstTwentyMillisecondsPeak = samples.Take(attackSamples).Max(sample => Mathf.Abs(sample));
            var centralStart = Mathf.RoundToInt(.08f * ProceduralAudioSynthesis.DefaultSampleRate);
            var centralEnd = Mathf.RoundToInt(.28f * ProceduralAudioSynthesis.DefaultSampleRate);
            var zeroCrossings = 0;
            for (var index = centralStart + 1; index < centralEnd; index++)
                if ((samples[index - 1] < 0f && samples[index] >= 0f) || (samples[index - 1] >= 0f && samples[index] < 0f)) zeroCrossings++;
            var estimatedFrequency = zeroCrossings / (2f * (centralEnd - centralStart) / ProceduralAudioSynthesis.DefaultSampleRate);

            Assert.That(spec.Recipe, Is.EqualTo(AudioRecipe.GateCardArrival));
            Assert.That(spec.DurationSeconds, Is.EqualTo(.72f).Within(.001f));
            Assert.That(spec.Gain, Is.LessThanOrEqualTo(.18f));
            Assert.That(peak, Is.LessThan(.15f), "arrival peak remains below tactile verdict/stamp impacts");
            Assert.That(rms, Is.InRange(.01f, .07f), "arrival remains audible without crowding the ambient bed");
            Assert.That(dc, Is.LessThan(.005f), "paired tonal partials have no audible DC bias");
            Assert.That(firstTwentyMillisecondsPeak, Is.LessThan(.03f), "envelope prevents a click-like transient");
            Assert.That(Mathf.Abs(samples[samples.Length - 1]), Is.LessThan(.0001f), "arrival ends quietly");
            Assert.That(estimatedFrequency, Is.InRange(160f, 420f), "the settled storybook interval avoids a piercing pitch band");
        }

        [Test]
        public void CardArrivalCadenceIsOncePerEncounterAcrossTheFortySlotRun()
        {
            var cadence = new EncounterArrivalCadence();
            cadence.BeginRun();
            for (var day = 1; day <= EncounterArrivalCadence.DaysPerRun; day++)
            for (var encounter = 0; encounter < EncounterArrivalCadence.EncountersPerDay; encounter++)
            {
                Assert.That(cadence.TryAnnounce(day, encounter, out var key), Is.True, key);
                Assert.That(cadence.TryAnnounce(day, encounter, out _), Is.False,
                    "caption/decree/continuation rerenders cannot announce an already-entered slot");
            }

            Assert.That(cadence.AnnouncedCount, Is.EqualTo(40));
            Assert.That(cadence.TryAnnounce(0, 0, out _), Is.False);
            Assert.That(cadence.TryAnnounce(1, 8, out _), Is.False);
        }

        [Test]
        public void CardArrivalCadenceAllowsEverySlotAgainAfterANewRun()
        {
            var cadence = new EncounterArrivalCadence();
            cadence.BeginRun();
            for (var day = 1; day <= EncounterArrivalCadence.DaysPerRun; day++)
            for (var encounter = 0; encounter < EncounterArrivalCadence.EncountersPerDay; encounter++)
                Assert.That(cadence.TryAnnounce(day, encounter, out _), Is.True, "run A " + day + ":" + encounter);

            cadence.BeginRun();
            Assert.That(cadence.RunGeneration, Is.EqualTo(2));
            Assert.That(cadence.AnnouncedCount, Is.EqualTo(0));
            for (var day = 1; day <= EncounterArrivalCadence.DaysPerRun; day++)
            for (var encounter = 0; encounter < EncounterArrivalCadence.EncountersPerDay; encounter++)
                Assert.That(cadence.TryAnnounce(day, encounter, out _), Is.True, "run B " + day + ":" + encounter);

            Assert.That(cadence.AnnouncedCount, Is.EqualTo(40));
        }

        [Test]
        public void CardArrivalCadenceRestartReenablesSlotsVisitedBeforeAnInterruptedRun()
        {
            var cadence = new EncounterArrivalCadence();
            cadence.BeginRun();
            Assert.That(cadence.TryAnnounce(1, 0, out _), Is.True);
            Assert.That(cadence.TryAnnounce(3, 5, out _), Is.True);
            Assert.That(cadence.TryAnnounce(1, 0, out _), Is.False);

            cadence.BeginRun();

            Assert.That(cadence.TryAnnounce(1, 0, out _), Is.True);
            Assert.That(cadence.TryAnnounce(3, 5, out _), Is.True);
            Assert.That(cadence.TryAnnounce(1, 0, out _), Is.False);
        }

        [Test]
        public void PlaybackPolicyEnforcesCooldownAndPolyphonyWithoutUnityAudioState()
        {
            var cooldown = new AudioCueSpec("cooldown", AudioCategory.Ui, AudioRecipe.UiClick, 1f, 1f, .5f, 2);
            var policy = new AudioPlaybackPolicy();
            Assert.That(policy.TryAuthorize(cooldown, 10f, out var firstReason), Is.True);
            Assert.That(firstReason, Is.Empty);
            Assert.That(policy.TryAuthorize(cooldown, 10.2f, out var cooldownReason), Is.False);
            Assert.That(cooldownReason, Is.EqualTo("cooldown"));
            Assert.That(policy.TryAuthorize(cooldown, 10.6f, out _), Is.True);
            Assert.That(policy.ActiveVoices("cooldown", 10.6f), Is.EqualTo(2));
            var polyphonic = new AudioCueSpec("polyphonic", AudioCategory.Ui, AudioRecipe.UiClick, 2f, 1f, .1f, 2);
            Assert.That(policy.TryAuthorize(polyphonic, 20f, out _), Is.True);
            Assert.That(policy.TryAuthorize(polyphonic, 20.2f, out _), Is.True);
            Assert.That(policy.TryAuthorize(polyphonic, 20.4f, out var polyphonyReason), Is.False);
            Assert.That(polyphonyReason, Is.EqualTo("polyphony"));
            Assert.That(policy.ActiveVoices("cooldown", 11.7f), Is.EqualTo(0));
            Assert.That(policy.TryAuthorize(cooldown, 11.7f, out _), Is.True);
        }

        [Test]
        public void RouterOnlyMakesInteractionSoundsForTheirMatchingStructuredTrace()
        {
            Assert.That(AudioCueRouter.Route("input.drag_start", "x=1;y=2"), Is.EqualTo(new[] { AudioCueIds.CardPickup }));
            Assert.That(AudioCueRouter.Route("input.drag_move", "distance=48;sampled=true"), Is.EqualTo(new[] { AudioCueIds.CardDrag }));
            Assert.That(AudioCueRouter.Route("input.swipe_evaluated", "commit=False;decision=Admit"), Is.EqualTo(new[] { AudioCueIds.CardSnapback }));
            Assert.That(AudioCueRouter.Route("input.swipe_evaluated", "commit=True;decision=Deny"), Is.Empty);
            Assert.That(AudioCueRouter.Route("input.drag_move", "distance=48;sampled=not-a-bool"), Is.EqualTo(new[] { AudioCueIds.CardDrag }));
            Assert.That(AudioCueRouter.HasBoolean("commit=True;decision=Deny", "commit", true), Is.True);
            Assert.That(AudioCueRouter.HasBoolean("commit=not-a-bool", "commit", false), Is.False);
        }

        [Test]
        public void GameAudioDefersGenerationUntilGestureAndPersistsMuteWithTraceEvidence()
        {
            var host = new GameObject("audio-test-host");
            var audio = host.AddComponent<GameAudio>();
            InvokeAwake(audio);
            Assert.That(audio.IsInteractionUnlocked, Is.False);
            Assert.That(audio.IsMuted, Is.False);
            Assert.That(audio.CachedClipCount, Is.EqualTo(0));
            Assert.That(DebugTrace.Recent.Any(trace => trace.EventId == "audio.init"), Is.True);

            audio.Play(AudioCueIds.GateOpen);
            Assert.That(audio.CachedClipCount, Is.EqualTo(0));
            Assert.That(DebugTrace.Recent.Any(trace => trace.EventId == "audio.suppressed" && trace.Payload.Contains("not_activated")), Is.True);

            audio.UnlockFromInteraction();
            Assert.That(audio.IsInteractionUnlocked, Is.True);
            Assert.That(audio.CachedClipCount, Is.EqualTo(1), "only ambient is generated at activation");
            audio.Play(AudioCueIds.GateOpen);
            Assert.That(audio.CachedClipCount, Is.EqualTo(2));
            Assert.That(DebugTrace.Recent.Any(trace => trace.EventId == "audio.play" && trace.Payload.Contains(AudioCueIds.GateOpen)), Is.True);

            audio.ToggleMute();
            Assert.That(audio.IsMuted, Is.True);
            Assert.That(PlayerPrefs.GetInt(GameAudio.MutePreferenceKey, 0), Is.EqualTo(1));
            audio.Play(AudioCueIds.VerdictStamp);
            Assert.That(DebugTrace.Recent.Any(trace => trace.EventId == "audio.suppressed" && trace.Payload.Contains("reason=muted")), Is.True);
            audio.ToggleMute();
            Assert.That(audio.IsMuted, Is.False);
            Assert.That(PlayerPrefs.GetInt(GameAudio.MutePreferenceKey, 1), Is.EqualTo(0));
            Assert.That(AudioListener.pause, Is.False, "unmuting must resume the retained ambient source");
            Assert.That(audio.CachedClipCount, Is.EqualTo(2), "unmuting keeps the existing ambient clip rather than creating an autoplay duplicate");
            Assert.That(DebugTrace.Recent.Any(trace => trace.EventId == "audio.mute"), Is.True);

            DestroyAudioHost(host, audio);
        }

        [Test]
        public void GameAudioRequestsOneArrivalPerEncounterEvenWhenTheRenderPathRepeats()
        {
            var host = new GameObject("arrival-cadence-host");
            var audio = host.AddComponent<GameAudio>();
            InvokeAwake(audio);
            audio.UnlockFromInteraction();
            audio.BeginRun();

            Assert.That(audio.PlayEncounterArrival(1, 0, "guard"), Is.True);
            Assert.That(audio.PlayEncounterArrival(1, 0, "guard"), Is.False);
            Assert.That(DebugTrace.Recent.Count(trace => trace.EventId == "audio.arrival"), Is.EqualTo(1));
            Assert.That(DebugTrace.Recent.Count(trace => trace.EventId == "audio.arrival_suppressed"), Is.EqualTo(1));
            Assert.That(DebugTrace.Recent.Last(trace => trace.EventId == "audio.arrival").Payload,
                Does.Contain("cue=" + AudioCueIds.GateOpen).And.Contain("once=encounter").And.Contain("actual=true"));
            Assert.That(DebugTrace.Recent.Count(trace => trace.EventId == "audio.play" && trace.Payload.Contains("cue=" + AudioCueIds.GateOpen)), Is.EqualTo(1),
                "the accepted arrival must correspond to an actual cue play, not merely a request");
            DestroyAudioHost(host, audio);
        }

        [Test]
        public void GameAudioActuallyPlaysEveryArrivalAcrossFastFortySlotRunAndReplay()
        {
            var events = new List<TraceEvent>();
            Action<TraceEvent> record = trace => events.Add(trace);
            DebugTrace.Recorded += record;
            var host = new GameObject("arrival-actual-playback-host");
            var audio = host.AddComponent<GameAudio>();
            try
            {
                InvokeAwake(audio);
                audio.UnlockFromInteraction();

                for (var generation = 1; generation <= 2; generation++)
                {
                    audio.BeginRun();
                    for (var day = 1; day <= EncounterArrivalCadence.DaysPerRun; day++)
                    for (var encounter = 0; encounter < EncounterArrivalCadence.EncountersPerDay; encounter++)
                    {
                        Assert.That(audio.PlayEncounterArrival(day, encounter, "visitor-" + day + "-" + encounter), Is.True,
                            "generation " + generation + " slot " + day + ":" + encounter);
                        Assert.That(audio.PlayEncounterArrival(day, encounter, "visitor-" + day + "-" + encounter), Is.False,
                            "same slot remains duplicate-suppressed after its actual cue plays");
                    }

                    Assert.That(audio.EncounterArrivalCadence.AnnouncedCount, Is.EqualTo(40), "generation " + generation);
                    var expectedActualPlays = generation * 40;
                    Assert.That(events.Count(trace => trace.EventId == "audio.play" && trace.Payload.Contains("cue=" + AudioCueIds.GateOpen)
                        && trace.Payload.Contains("policy=arrival_slot")), Is.EqualTo(expectedActualPlays),
                        "fast progression must count real source.Play calls rather than only arrival requests");
                    Assert.That(events.Count(trace => trace.EventId == "audio.arrival"), Is.EqualTo(expectedActualPlays));
                }

                Assert.That(events.Count(trace => trace.EventId == "audio.arrival_suppressed" && trace.Payload.Contains("duplicate_or_invalid")), Is.EqualTo(80));
            }
            finally
            {
                DebugTrace.Recorded -= record;
                DestroyAudioHost(host, audio);
            }
        }

        [Test]
        public void GameAudioStartsANewArrivalGenerationWithoutChangingUnlockMuteOrPlaybackPolicy()
        {
            var host = new GameObject("arrival-run-host");
            var audio = host.AddComponent<GameAudio>();
            InvokeAwake(audio);
            audio.UnlockFromInteraction();
            audio.BeginRun();
            Assert.That(audio.PlayEncounterArrival(1, 0, "guard"), Is.True);
            var policy = audio.PlaybackPolicy;

            audio.BeginRun();

            Assert.That(audio.EncounterArrivalCadence.RunGeneration, Is.EqualTo(2));
            Assert.That(audio.PlayEncounterArrival(1, 0, "guard"), Is.True);
            Assert.That(audio.IsInteractionUnlocked, Is.True);
            Assert.That(audio.IsMuted, Is.False);
            Assert.That(audio.PlaybackPolicy, Is.SameAs(policy));
            Assert.That(DebugTrace.Recent.Count(trace => trace.EventId == "audio.arrival_run_started"), Is.EqualTo(2));
            DestroyAudioHost(host, audio);
        }

        [Test]
        public void FirstPointerDragUnlocksAndPlaysTheFullNonCommittingSwipeFeedback()
        {
            var host = new GameObject("audio-trace-host");
            var audio = host.AddComponent<GameAudio>();
            InvokeAwake(audio);
            Assert.That(audio.IsInteractionUnlocked, Is.False);
            Assert.That(audio.CachedClipCount, Is.EqualTo(0));
            DebugTrace.Log("input.drag_start", "x=1;y=2");
            Assert.That(audio.IsInteractionUnlocked, Is.True, "pointer down is a deliberate primary-control gesture");
            Assert.That(audio.CachedClipCount, Is.EqualTo(2), "gesture creates ambient and pickup only");
            DebugTrace.Log("input.drag_move", "distance=48;sampled=true");
            DebugTrace.Log("input.drag_move", "distance=96;sampled=true");
            DebugTrace.Log("input.swipe_evaluated", "commit=False;decision=Admit");
            Assert.That(audio.CachedClipCount, Is.GreaterThanOrEqualTo(4));
            Assert.That(DebugTrace.Recent.Any(trace => trace.EventId == "audio.suppressed" && trace.Payload.Contains("reason=cooldown")), Is.True);
            DestroyAudioHost(host, audio);
        }

        [Test]
        public void DisabledAudioDoesNotReceiveTraceAndReenableSubscribesOnlyOnce()
        {
            var host = new GameObject("audio-subscription-host");
            var audio = host.AddComponent<GameAudio>();
            InvokeAwake(audio);
            InvokeLifecycle(audio, "OnDisable");
            DebugTrace.Log("input.drag_start", "x=1;y=2");
            Assert.That(audio.IsInteractionUnlocked, Is.False);
            Assert.That(audio.CachedClipCount, Is.EqualTo(0));

            InvokeLifecycle(audio, "OnEnable");
            InvokeLifecycle(audio, "OnEnable");
            DebugTrace.Log("input.drag_start", "x=1;y=2");
            DebugTrace.Log("input.drag_move", "distance=48;sampled=true");
            Assert.That(audio.IsInteractionUnlocked, Is.True);
            Assert.That(audio.CachedClipCount, Is.EqualTo(3));
            Assert.That(DebugTrace.Recent.Count(trace => trace.EventId == "audio.play" && trace.Payload.Contains(AudioCueIds.CardDrag)), Is.EqualTo(1));
            Assert.That(DebugTrace.Recent.Any(trace => trace.EventId == "audio.suppressed" && trace.Payload.Contains("reason=cooldown")), Is.False);
            DestroyAudioHost(host, audio);
        }

        private static void InvokeAwake(GameAudio audio)
        {
            InvokeLifecycle(audio, "Awake");
        }

        private static void InvokeLifecycle(GameAudio audio, string method)
        {
            typeof(GameAudio).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(audio, null);
        }

        private static void DestroyAudioHost(GameObject host, GameAudio audio)
        {
            InvokeLifecycle(audio, "OnDestroy");
            UnityEngine.Object.DestroyImmediate(host);
        }
    }
}
