using UnityEngine;
using WhoEnters.Core;

namespace WhoEnters.Gameplay
{
    /// <summary>Lifecycle hook for content-owned clips; does not create audio before an interaction.</summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private const string MuteKey = "who-enters.muted";
        public bool IsMuted { get; private set; }
        private bool interactionUnlocked;

        private void Awake()
        {
            IsMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
            AudioListener.pause = IsMuted;
            DebugTrace.Log("asset.audio_loaded", "count=0;provider=pending-content");
            DebugTrace.Log("audio.lifecycle", $"event=awake;muted={IsMuted}");
        }

        public void UnlockFromInteraction()
        {
            if (interactionUnlocked) return;
            interactionUnlocked = true;
            DebugTrace.Log("audio.lifecycle", "event=interaction_unlocked");
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            AudioListener.pause = IsMuted;
            PlayerPrefs.SetInt(MuteKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            DebugTrace.Log("persistence.saved", $"key={MuteKey};value={IsMuted}");
            DebugTrace.Log("audio.lifecycle", $"event=mute_toggled;muted={IsMuted}");
        }

        public void Play(string cueId)
        {
            if (!interactionUnlocked || IsMuted)
            {
                DebugTrace.Log("audio.play_skipped", $"cue={cueId};unlocked={interactionUnlocked};muted={IsMuted}");
                return;
            }
            DebugTrace.Log("audio.play_requested", $"cue={cueId};asset=content-owned");
        }
    }
}
