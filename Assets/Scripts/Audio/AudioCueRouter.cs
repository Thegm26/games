using System;
using System.Collections.Generic;

namespace WhoEnters.Audio
{
    /// <summary>Pure bridge from stable gameplay traces to low-level interaction cues.</summary>
    public static class AudioCueRouter
    {
        /// <summary>Only pointer-down is a gesture activation event; sampled drags are never allowed to activate audio.</summary>
        public static bool IsGestureActivationEvent(string eventId) => string.Equals(eventId, "input.drag_start", StringComparison.Ordinal);

        public static IReadOnlyList<string> Route(string eventId, string payload)
        {
            if (string.Equals(eventId, "input.drag_start", StringComparison.Ordinal)) return new[] { AudioCueIds.CardPickup };
            if (string.Equals(eventId, "input.drag_move", StringComparison.Ordinal)) return new[] { AudioCueIds.CardDrag };
            // SwipeCard emits this once per release. Only a non-committing release should make
            // the return-to-centre sound; verdict audio is deliberately owned by GameDirector.
            if (string.Equals(eventId, "input.swipe_evaluated", StringComparison.Ordinal) && HasBoolean(payload, "commit", false)) return new[] { AudioCueIds.CardSnapback };
            return Array.Empty<string>();
        }

        public static bool HasBoolean(string payload, string key, bool expected)
        {
            if (string.IsNullOrEmpty(payload)) return false;
            foreach (var pair in payload.Split(';'))
            {
                var parts = pair.Split('=');
                if (parts.Length != 2 || !string.Equals(parts[0], key, StringComparison.Ordinal)) continue;
                if (bool.TryParse(parts[1], out var actual) && actual == expected) return true;
            }
            return false;
        }
    }
}
