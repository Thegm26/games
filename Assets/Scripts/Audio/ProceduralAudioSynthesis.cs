using System;
using UnityEngine;

namespace WhoEnters.Audio
{
    /// <summary>Deterministic mono PCM generator. It intentionally uses no sampled or external material.</summary>
    public static class ProceduralAudioSynthesis
    {
        public const int DefaultSampleRate = 22050;
        public const int Channels = 1;
        public const float MaximumPeak = .92f;

        public static float[] Generate(AudioCueSpec spec, int sampleRate = DefaultSampleRate)
        {
            if (sampleRate < 8000) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (spec.DurationSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(spec));
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(spec.DurationSeconds * sampleRate));
            var result = new float[sampleCount];
            for (var index = 0; index < sampleCount; index++)
            {
                var time = index / (float)sampleRate;
                var normalized = index / (float)Mathf.Max(1, sampleCount - 1);
                var raw = Signal(spec.Recipe, time, normalized, index, spec.DurationSeconds);
                result[index] = Mathf.Clamp(raw * spec.Gain * Envelope(spec, normalized), -MaximumPeak, MaximumPeak);
            }
            return result;
        }

        public static AudioClip CreateClip(AudioCueSpec spec, int sampleRate = DefaultSampleRate)
        {
            var samples = Generate(spec, sampleRate);
            var clip = AudioClip.Create("who-enters-" + spec.Id, samples.Length, Channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Signal(AudioRecipe recipe, float time, float n, int index, float duration)
        {
            switch (recipe)
            {
                case AudioRecipe.CastleWindDrone:
                    return .42f * Sine(46f, time) + .24f * Sine(59f, time) + .12f * Sine(92f, time) + .12f * Sine(4f, time) * Sine(184f, time);
                case AudioRecipe.DistantBell:
                    return Decay(n, 2.5f) * (.65f * Sine(523.25f, time) + .28f * Sine(1046.5f, time) + .16f * Sine(1569.75f, time));
                case AudioRecipe.GateCardArrival:
                    // Avoid the broad-band noise and abrupt low creak that made the former
                    // arrival sound read as a harsh impact.  These consonant, deterministic
                    // partials suggest a small wooden gate settling and a card arriving.
                    return Decay(n, 3.7f) * (
                        .46f * Sine(196f, time)
                        + .25f * Sine(293.66f, time)
                        + .10f * Sine(392f, time));
                case AudioRecipe.CardPickup:
                    return Decay(n, 7f) * (.56f * DeterministicNoise(index) + .18f * Sine(680f, time));
                case AudioRecipe.CardRustle:
                    return Decay(n, 8f) * (.62f * DeterministicNoise(index + 17) + .12f * Sine(390f, time));
                case AudioRecipe.CardSnapback:
                    return Decay(n, 7f) * (.48f * Sine(260f - 115f * n, time) + .14f * DeterministicNoise(index));
                case AudioRecipe.AdmitLock:
                    return Decay(n, 4f) * (.54f * Sine(330f, time) + .38f * Sine(494f, time) + .18f * Sine(660f, time));
                case AudioRecipe.DenyChainMoat:
                    return Decay(n, 3.8f) * (.44f * Sine(108f, time) + .32f * DeterministicNoise(index) + .16f * Sine(54f, time));
                case AudioRecipe.VerdictStamp:
                    return Decay(n, 12f) * (.64f * DeterministicNoise(index) + .28f * Sine(115f, time));
                case AudioRecipe.SealCrack:
                    return Decay(n, 6f) * (.60f * DeterministicNoise(index) + .33f * Sine(70f + 40f * n, time));
                case AudioRecipe.StreakChime:
                    return Decay(n, 3.4f) * (.47f * Sine(659.25f, time) + .32f * Sine(783.99f, time) + .18f * Sine(987.77f, time));
                case AudioRecipe.FallingEnding:
                    return Decay(n, 1.9f) * (.46f * Sine(160f - 90f * n, time) + .28f * Sine(80f - 35f * n, time) + .13f * DeterministicNoise(index));
                case AudioRecipe.HollowEnding:
                    return Decay(n, 1.3f) * (.48f * Sine(220f, time) + .27f * Sine(277.18f, time) + .16f * Sine(329.63f, time));
                case AudioRecipe.HeldEnding:
                    return Decay(n, 1.25f) * (.44f * Sine(261.63f, time) + .31f * Sine(329.63f, time) + .21f * Sine(392f, time));
                case AudioRecipe.SecretEnding:
                    return Decay(n, 1.15f) * (.35f * Sine(293.66f, time) + .28f * Sine(440f, time) + .19f * Sine(587.33f, time) + .10f * Sine(880f, time));
                case AudioRecipe.UiClick:
                    return Decay(n, 15f) * (.52f * Sine(740f, time) + .20f * Sine(1110f, time));
                default: return 0f;
            }
        }

        private static float Envelope(AudioCueSpec spec, float n)
        {
            if (spec.Loop) return 1f;
            // The arrival is intentionally eased in over a small beat.  Its consonant partials
            // otherwise reach audible level within 20ms, which reads as a click on phone speakers.
            var attack = spec.Recipe == AudioRecipe.GateCardArrival ? .12f : .025f;
            const float release = .07f;
            var rise = Mathf.Clamp01(n / attack);
            var fall = Mathf.Clamp01((1f - n) / release);
            return rise * fall;
        }

        private static float Decay(float n, float strength) => Mathf.Exp(-strength * n);
        private static float Sine(float frequency, float time) => Mathf.Sin(2f * Mathf.PI * frequency * time);
        private static float DeterministicNoise(int index)
        {
            unchecked
            {
                var value = (uint)(index * 747796405 + 2891336453);
                value = (value >> ((int)(value >> 28) + 4)) ^ value;
                value *= 277803737;
                value = (value >> 22) ^ value;
                return (value / (float)uint.MaxValue) * 2f - 1f;
            }
        }
    }
}
