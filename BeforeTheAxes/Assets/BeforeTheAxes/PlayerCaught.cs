using UnityEngine;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

namespace BeforeTheAxes
{
    /// <summary>
    /// Owns the one-shot defeat transition.  It stops the forest immediately, lets the player
    /// read the result, and then returns to the title without ever reloading the forest.
    /// </summary>
    public sealed class PlayerCaught : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

        [Header("Audio")]
        [SerializeField] private AudioClip caughtSound;
        [SerializeField, Range(0f, 1f)] private float caughtSoundVolume = .65f;
        [SerializeField] private AudioSource uiSoundSource;
        [Header("Presentation")]
        [SerializeField, Min(.25f)] private float caughtScreenSeconds = 2.25f;
        [SerializeField, Range(.1f, .5f)] private float finalFadeSeconds = .22f;

        public bool IsCaught { get; private set; }
        public bool IsGameplayFrozen { get; private set; }
        public bool IsReturnRequested { get; private set; }
        public int SoundTriggerCount { get; private set; }
        public int ReturnRequestCount { get; private set; }
        public event Action MainMenuReturnRequested;

        private Coroutine caughtRoutine;
        private float caughtPresentationStartedAt;

        public void Catch()
        {
            if (IsCaught) return;
            IsCaught = true;
            // This is deliberately unscaled: Catch freezes the forest immediately.
            caughtPresentationStartedAt = Time.unscaledTime;

            PlayCaughtSoundOnce();
            FreezeGameplay();
            caughtRoutine = StartCoroutine(ReturnToMainMenuAfterPresentation());
        }

        private void PlayCaughtSoundOnce()
        {
            if (caughtSound == null || uiSoundSource == null) return;
            uiSoundSource.PlayOneShot(caughtSound, caughtSoundVolume);
            SoundTriggerCount++;
        }

        private void FreezeGameplay()
        {
            ForestGuardianController guardian = GetComponent<ForestGuardianController>();
            if (guardian != null) guardian.enabled = false;

            // Another cutter can be in its Update loop on this same rendered frame. Disabling
            // all of them prevents a second Catch call and freezes every patrol/search/chase.
            WoodcutterAI[] woodcutters = FindObjectsByType<WoodcutterAI>(FindObjectsSortMode.None);
            foreach (WoodcutterAI woodcutter in woodcutters)
            {
                if (woodcutter != null) woodcutter.enabled = false;
            }

            IsGameplayFrozen = true;
            Time.timeScale = 0f;
        }

        private IEnumerator ReturnToMainMenuAfterPresentation()
        {
            // Realtime keeps this sequence reliable after Time.timeScale freezes the game.
            yield return new WaitForSecondsRealtime(caughtScreenSeconds);
            if (IsReturnRequested) yield break;

            IsReturnRequested = true;
            ReturnRequestCount++;
            MainMenuReturnRequested?.Invoke();
            Time.timeScale = 1f;
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void OnDisable()
        {
            // Never leak the frozen time scale into a menu or another scene, including an
            // interrupted load in the editor.
            if (IsGameplayFrozen) Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            if (!IsCaught || Event.current.type != EventType.Repaint) return;

            float elapsed = Mathf.Max(0f, Time.unscaledTime - caughtPresentationStartedAt);
            float titleIn = Smooth01(elapsed / .18f);
            float finalFadeStart = Mathf.Max(.1f, caughtScreenSeconds - finalFadeSeconds);
            float finalFade = Smooth01((elapsed - finalFadeStart) / Mathf.Max(.01f, finalFadeSeconds));

            Color previousColor = GUI.color;
            float barHeight = Mathf.Clamp(Screen.height * .105f, 44f, 108f);
            GUI.color = new Color(.015f, .024f, .018f, .88f * titleIn);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, barHeight), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, Screen.height - barHeight, Screen.width, barHeight), Texture2D.whiteTexture);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * .09f, 40f, 76f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, .82f, .48f, titleIn) }
            };
            GUIStyle detailStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * .027f, 16f, 24f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.92f, .96f, .82f, titleIn) }
            };
            GUI.Label(new Rect(0f, Screen.height * .36f, Screen.width, Screen.height * .16f), "CAUGHT", titleStyle);
            GUI.Label(new Rect(0f, Screen.height * .56f, Screen.width, Screen.height * .06f), "The woodcutters found you.", detailStyle);

            // The scene remains visible during the result; black is used only for the handoff.
            if (finalFade > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, finalFade);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            }
            GUI.color = previousColor;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
