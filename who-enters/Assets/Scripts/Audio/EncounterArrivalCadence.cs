using System.Collections.Generic;

namespace WhoEnters.Audio
{
    /// <summary>
    /// Pure encounter-level gate for the card-arrival cue. Rendering and caption continuation
    /// may legitimately revisit a UI surface, but an encounter gets one arrival announcement.
    /// </summary>
    public sealed class EncounterArrivalCadence
    {
        public const int DaysPerRun = 5;
        public const int EncountersPerDay = 8;

        private readonly HashSet<string> announcedEncounterKeys = new HashSet<string>();

        public int AnnouncedCount => announcedEncounterKeys.Count;
        public int RunGeneration { get; private set; }

        /// <summary>
        /// Starts a fresh gameplay run. Slot identity remains stable within a run, but the same
        /// forty slots must be eligible again after Play Again or an interrupted run restart.
        /// </summary>
        public void BeginRun()
        {
            announcedEncounterKeys.Clear();
            RunGeneration++;
        }

        /// <summary>
        /// Checks whether a slot may be recorded as having played its arrival cue. Callers that
        /// perform asynchronous or fallible work must not consume the slot until that work has
        /// actually succeeded.
        /// </summary>
        public bool CanAnnounce(int day, int encounterIndex, out string encounterKey)
        {
            encounterKey = KeyFor(day, encounterIndex);
            if (day < 1 || day > DaysPerRun || encounterIndex < 0 || encounterIndex >= EncountersPerDay)
                return false;
            return !announcedEncounterKeys.Contains(encounterKey);
        }

        /// <summary>Records one successfully played arrival cue for a valid, still-unannounced slot.</summary>
        public bool TryAnnounce(int day, int encounterIndex, out string encounterKey)
        {
            if (!CanAnnounce(day, encounterIndex, out encounterKey)) return false;
            return announcedEncounterKeys.Add(encounterKey);
        }

        public static string KeyFor(int day, int encounterIndex)
        {
            // Slot identity, not a visitor branch, caption, or text render, is the gameplay
            // unit of cadence. This remains exactly one announcement for each of 40 slots.
            return day + ":" + encounterIndex;
        }
    }
}
