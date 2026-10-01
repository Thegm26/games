using System.Collections.Generic;
using WhoEnters.Audio;
using WhoEnters.Core;

namespace WhoEnters.Integration
{
    public readonly struct ScheduledVerdictCue
    {
        public readonly string CueId;
        public readonly float OffsetSeconds;
        public ScheduledVerdictCue(string cueId, float offsetSeconds) { CueId = cueId; OffsetSeconds = offsetSeconds; }
    }

    /// <summary>Separates a verdict's tactile decision, stamp, and outcome feedback so they never pile up.</summary>
    public static class VerdictAudioCadence
    {
        public const float StampOffsetSeconds = .18f;
        public const float OutcomeOffsetSeconds = .42f;

        public static IReadOnlyList<ScheduledVerdictCue> Build(Decision decision, bool correct, int streak)
        {
            var cues = new List<ScheduledVerdictCue>
            {
                new ScheduledVerdictCue(decision == Decision.Admit ? AudioCueIds.VerdictAdmit : AudioCueIds.VerdictDeny, 0f),
                new ScheduledVerdictCue(AudioCueIds.VerdictStamp, StampOffsetSeconds),
            };
            if (!correct) cues.Add(new ScheduledVerdictCue(AudioCueIds.IntegrityCrack, OutcomeOffsetSeconds));
            else if (streak >= 2) cues.Add(new ScheduledVerdictCue(AudioCueIds.StreakChime, OutcomeOffsetSeconds));
            return cues;
        }
    }
}
