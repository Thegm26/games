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
        [SerializeField, Range(.35f, .6f)] private float caughtOverlayAlpha = .46f;

        public bool IsCaught { get; private set; }
        public bool IsGameplayFrozen { get; private set; }
        public bool IsReturnRequested { get; private set; }
        public int SoundTriggerCount { get; private set; }
        public int ReturnRequestCount { get; private set; }
        public event Action MainMenuReturnRequested;

        private Coroutine caughtRoutine;

        public void Catch()
        {
            if (IsCaught) return;
            IsCaught = true;

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

            Color previousColor = GUI.color;
            GUI.color = new Color(.025f, .055f, .035f, caughtOverlayAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            float panelWidth = Mathf.Min(Screen.width * .78f, 760f);
            float panelHeight = Mathf.Min(Screen.height * .31f, 230f);
            Rect panel = new Rect((Screen.width - panelWidth) * .5f, (Screen.height - panelHeight) * .5f, panelWidth, panelHeight);
            GUI.color = new Color(.16f, .075f, .035f, .96f);
            GUI.Box(panel, GUIContent.none);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * .10f, 42f, 82f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, .79f, .40f) }
            };
            GUIStyle detailStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * .027f, 16f, 24f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.91f, .95f, .79f) }
            };
            GUI.Label(new Rect(panel.x, panel.y + panel.height * .17f, panel.width, panel.height * .46f), "CAUGHT", titleStyle);
            GUI.Label(new Rect(panel.x, panel.y + panel.height * .65f, panel.width, panel.height * .18f), "Returning to the grove...", detailStyle);
            GUI.color = previousColor;
        }
    }
}
