using System.Collections.Generic;

namespace WhoEnters.Audio
{
    /// <summary>Pure cooldown/polyphony gate, independent of Unity audio state for deterministic tests.</summary>
    public sealed class AudioPlaybackPolicy
    {
        private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        private readonly Dictionary<string, List<float>> activeUntil = new Dictionary<string, List<float>>();

        public bool TryAuthorize(AudioCueSpec spec, float now, out string reason)
        {
            Prune(spec.Id, now);
            if (lastPlayed.TryGetValue(spec.Id, out var last) && now - last < spec.CooldownSeconds)
            {
                reason = "cooldown";
                return false;
            }
            if (activeUntil.TryGetValue(spec.Id, out var voices) && voices.Count >= spec.MaxPolyphony)
            {
                reason = "polyphony";
                return false;
            }
            if (!activeUntil.TryGetValue(spec.Id, out voices))
            {
                voices = new List<float>();
                activeUntil.Add(spec.Id, voices);
            }
            voices.Add(now + spec.DurationSeconds);
            lastPlayed[spec.Id] = now;
            reason = string.Empty;
            return true;
        }

        public int ActiveVoices(string cueId, float now)
        {
            Prune(cueId, now);
            return activeUntil.TryGetValue(cueId, out var voices) ? voices.Count : 0;
        }

        private void Prune(string cueId, float now)
        {
            if (!activeUntil.TryGetValue(cueId, out var voices)) return;
            voices.RemoveAll(until => until <= now);
        }
    }
}
