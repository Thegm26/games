using System;
using System.Collections.Generic;

namespace WhoEnters.Audio
{
    public enum AudioCategory { Ambient, Interaction, Verdict, Feedback, Ui, Ending }
    public enum AudioRecipe
    {
        CastleWindDrone, DistantBell, GateCardArrival, CardPickup, CardRustle, CardSnapback,
        AdmitLock, DenyChainMoat, VerdictStamp, SealCrack, StreakChime, FallingEnding,
        HollowEnding, HeldEnding, SecretEnding, UiClick
    }

    public static class AudioCueIds
    {
        public const string AmbientWind = "ambient.castle_wind";
        public const string DayTransition = "day.transition";
        public const string GateOpen = "gate.open";
        public const string CardPickup = "card.pickup";
        public const string CardDrag = "card.drag";
        public const string CardSnapback = "card.snapback";
        public const string VerdictAdmit = "verdict.admit";
        public const string VerdictDeny = "verdict.deny";
        public const string VerdictStamp = "verdict.stamp";
        public const string IntegrityCrack = "integrity.seal_crack";
        public const string StreakChime = "streak.chime";
        public const string EndingFallen = "ending.fallen";
        public const string EndingHollow = "ending.hollow";
        public const string EndingHeld = "ending.held";
        public const string EndingSecret = "ending.secret";
        public const string UiClick = "ui.click";
    }

    public readonly struct AudioCueSpec
    {
        public readonly string Id;
        public readonly AudioCategory Category;
        public readonly AudioRecipe Recipe;
        public readonly float DurationSeconds;
        public readonly float Gain;
        public readonly float CooldownSeconds;
        public readonly int MaxPolyphony;
        public readonly bool Loop;

        public AudioCueSpec(string id, AudioCategory category, AudioRecipe recipe, float durationSeconds, float gain, float cooldownSeconds, int maxPolyphony, bool loop = false)
        {
            Id = id;
            Category = category;
            Recipe = recipe;
            DurationSeconds = durationSeconds;
            Gain = gain;
            CooldownSeconds = cooldownSeconds;
            MaxPolyphony = maxPolyphony;
            Loop = loop;
        }
    }

    /// <summary>Immutable original cue palette. All signal recipes are local procedural synthesis.</summary>
    public static class AudioCueCatalog
    {
        private static readonly Dictionary<string, AudioCueSpec> Specs = new Dictionary<string, AudioCueSpec>
        {
            { AudioCueIds.AmbientWind, new AudioCueSpec(AudioCueIds.AmbientWind, AudioCategory.Ambient, AudioRecipe.CastleWindDrone, 8f, .18f, 0f, 1, true) },
            { AudioCueIds.DayTransition, new AudioCueSpec(AudioCueIds.DayTransition, AudioCategory.Ambient, AudioRecipe.DistantBell, 2.8f, .30f, 1.2f, 1) },
            // A soft low gate resonance with a small parchment-like upper interval.  It is
            // intentionally quieter and shorter than verdict feedback, so a new card feels
            // placed at the gate rather than slammed into the scene.
            { AudioCueIds.GateOpen, new AudioCueSpec(AudioCueIds.GateOpen, AudioCategory.Interaction, AudioRecipe.GateCardArrival, .72f, .18f, .55f, 1) },
            { AudioCueIds.CardPickup, new AudioCueSpec(AudioCueIds.CardPickup, AudioCategory.Interaction, AudioRecipe.CardPickup, .16f, .16f, .18f, 1) },
            { AudioCueIds.CardDrag, new AudioCueSpec(AudioCueIds.CardDrag, AudioCategory.Interaction, AudioRecipe.CardRustle, .12f, .09f, .18f, 1) },
            { AudioCueIds.CardSnapback, new AudioCueSpec(AudioCueIds.CardSnapback, AudioCategory.Interaction, AudioRecipe.CardSnapback, .26f, .18f, .12f, 1) },
            { AudioCueIds.VerdictAdmit, new AudioCueSpec(AudioCueIds.VerdictAdmit, AudioCategory.Verdict, AudioRecipe.AdmitLock, .55f, .30f, .08f, 2) },
            { AudioCueIds.VerdictDeny, new AudioCueSpec(AudioCueIds.VerdictDeny, AudioCategory.Verdict, AudioRecipe.DenyChainMoat, .75f, .30f, .08f, 2) },
            { AudioCueIds.VerdictStamp, new AudioCueSpec(AudioCueIds.VerdictStamp, AudioCategory.Verdict, AudioRecipe.VerdictStamp, .32f, .38f, .10f, 1) },
            { AudioCueIds.IntegrityCrack, new AudioCueSpec(AudioCueIds.IntegrityCrack, AudioCategory.Feedback, AudioRecipe.SealCrack, .52f, .34f, .10f, 1) },
            { AudioCueIds.StreakChime, new AudioCueSpec(AudioCueIds.StreakChime, AudioCategory.Feedback, AudioRecipe.StreakChime, .72f, .27f, .18f, 1) },
            { AudioCueIds.EndingFallen, new AudioCueSpec(AudioCueIds.EndingFallen, AudioCategory.Ending, AudioRecipe.FallingEnding, 2.2f, .35f, .4f, 1) },
            { AudioCueIds.EndingHollow, new AudioCueSpec(AudioCueIds.EndingHollow, AudioCategory.Ending, AudioRecipe.HollowEnding, 2.4f, .32f, .4f, 1) },
            { AudioCueIds.EndingHeld, new AudioCueSpec(AudioCueIds.EndingHeld, AudioCategory.Ending, AudioRecipe.HeldEnding, 2.4f, .34f, .4f, 1) },
            { AudioCueIds.EndingSecret, new AudioCueSpec(AudioCueIds.EndingSecret, AudioCategory.Ending, AudioRecipe.SecretEnding, 2.7f, .35f, .4f, 1) },
            { AudioCueIds.UiClick, new AudioCueSpec(AudioCueIds.UiClick, AudioCategory.Ui, AudioRecipe.UiClick, .12f, .18f, .08f, 1) },
        };

        public static IReadOnlyDictionary<string, AudioCueSpec> All => Specs;
        public static bool TryGet(string cueId, out AudioCueSpec spec) => Specs.TryGetValue(cueId, out spec);
    }
}
