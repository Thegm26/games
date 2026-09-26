using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BeforeTheAxes
{
    /// <summary>Small, scene-local defeat state used by the woodcutter.</summary>
    public sealed class PlayerCaught : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip caughtSound;
        [SerializeField, Range(0f, 1f)] private float caughtSoundVolume = .65f;
        [SerializeField] private AudioSource uiSoundSource;

        public bool IsCaught { get; private set; }

        public void Catch()
        {
            if (IsCaught) return;
            IsCaught = true;
            if (caughtSound != null && uiSoundSource != null)
                uiSoundSource.PlayOneShot(caughtSound, caughtSoundVolume);
            ForestGuardianController guardian = GetComponent<ForestGuardianController>();
            if (guardian != null) guardian.enabled = false;
        }

        private void Update()
        {
            if (IsCaught && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnGUI()
        {
            if (!IsCaught) return;
            GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28, fontStyle = FontStyle.Bold };
            Rect rect = new Rect(0f, Screen.height * .42f, Screen.width, 90f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(rect, "CAUGHT BY THE WOODCUTTER\nPress R to restart", style);
        }
    }
}
