using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BeforeTheAxes
{
    /// <summary>A short, skippable opening story told through three staged forest tableaux.</summary>
    public sealed class IntroCutsceneController : MonoBehaviour
    {
        [SerializeField] private Camera cinematicCamera;
        [SerializeField] private Transform[] shotAnchors;
        [SerializeField] private AudioSource themeMusic;
        [SerializeField] private AudioSource uiAudio;
        [SerializeField] private AudioClip clickSound;
        [SerializeField, Min(1f)] private float secondsPerShot = 4.7f;
        [SerializeField, Range(.45f, .6f)] private float transitionDuration = .55f;
        [SerializeField, Range(0f, .2f)] private float cameraPushDistance = .18f;
        [SerializeField, Range(0f, .35f)] private float cameraYawDegrees = .28f;

        private readonly string[] captions =
        {
            "The woodcutters stole the Blue Root Mushroom — and learned to wield its ancient power.",
            "Now they use it to cut down our tree villages, one grove at a time.",
            "I must take it back and carry it home before our roots are lost forever."
        };

        private int shot;
        private float shotStarted;
        private bool loading;
        private bool transitioning;
        private bool transitionApplied;
        private bool transitionLoadsGame;
        private int pendingShot;
        private float transitionStarted;
        private float initialFadeStarted;
        private Vector3 cameraOffset;
        private float cameraRotationOffsetDegrees;
        private static Texture2D darkTexture;

        public int CurrentShot => shot;
        public string CurrentCaption => captions[Mathf.Clamp(shot, 0, captions.Length - 1)];
        public bool IsTransitioning => transitioning;
        public float TransitionProgress => transitioning
            ? Mathf.Clamp01((Time.unscaledTime - transitionStarted) / transitionDuration)
            : 0f;
        public Vector3 CameraOffset => cameraOffset;
        public float CameraRotationOffsetDegrees => cameraRotationOffsetDegrees;

        private void Awake()
        {
            if (cinematicCamera == null) cinematicCamera = Camera.main;
            ShowShot(0, true);
            initialFadeStarted = Time.unscaledTime;
        }

        private void Start()
        {
            if (themeMusic != null)
            {
                themeMusic.loop = true;
                themeMusic.spatialBlend = 0f;
                themeMusic.volume = .15f;
                if (!themeMusic.isPlaying) themeMusic.Play();
            }
        }

        private void Update()
        {
            if (loading) return;
            UpdateCameraMotion();

            if (transitioning)
            {
                UpdateTransition();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            bool skip = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            bool advance = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame ||
                           keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
            if (skip) { BeginTransitionToGame(); return; }
            if (advance || Time.unscaledTime - shotStarted >= secondsPerShot) Advance();
        }

        private void Advance()
        {
            PlayClick();
            if (shot >= captions.Length - 1) { BeginTransitionToGame(); return; }
            BeginShotTransition(shot + 1);
        }

        private void BeginShotTransition(int nextShot)
        {
            if (transitioning || loading) return;
            transitioning = true;
            transitionApplied = false;
            transitionLoadsGame = false;
            pendingShot = Mathf.Clamp(nextShot, 0, captions.Length - 1);
            transitionStarted = Time.unscaledTime;
        }

        private void BeginTransitionToGame()
        {
            if (transitioning || loading) return;
            transitioning = true;
            transitionApplied = false;
            transitionLoadsGame = true;
            transitionStarted = Time.unscaledTime;
        }

        private void UpdateTransition()
        {
            float progress = TransitionProgress;
            if (!transitionApplied && progress >= .5f)
            {
                transitionApplied = true;
                if (!transitionLoadsGame) ShowShot(pendingShot, true);
            }

            if (progress < 1f) return;
            transitioning = false;
            if (transitionLoadsGame) LoadGame();
            else shotStarted = Time.unscaledTime;
        }

        private void ShowShot(int index, bool instant)
        {
            shot = Mathf.Clamp(index, 0, captions.Length - 1);
            shotStarted = Time.unscaledTime;
            if (shotAnchors == null || shot >= shotAnchors.Length || shotAnchors[shot] == null || cinematicCamera == null) return;
            Transform anchor = shotAnchors[shot];
            cinematicCamera.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            cameraOffset = Vector3.zero;
            cameraRotationOffsetDegrees = 0f;
        }

        private void UpdateCameraMotion()
        {
            if (cinematicCamera == null || shotAnchors == null || shot >= shotAnchors.Length || shotAnchors[shot] == null) return;
            Transform anchor = shotAnchors[shot];
            float normalizedShotTime = Mathf.Clamp01((Time.unscaledTime - shotStarted) / secondsPerShot);
            float eased = normalizedShotTime * normalizedShotTime * (3f - 2f * normalizedShotTime);
            float drift = Mathf.Sin(normalizedShotTime * Mathf.PI) * .018f;
            Vector3 localOffset = new Vector3(drift, 0f, cameraPushDistance * eased);
            cameraOffset = anchor.TransformVector(localOffset);
            cameraRotationOffsetDegrees = cameraYawDegrees * Mathf.Sin(normalizedShotTime * Mathf.PI * .72f);
            Quaternion rotation = anchor.rotation * Quaternion.Euler(0f, cameraRotationOffsetDegrees, 0f);
            cinematicCamera.transform.SetPositionAndRotation(anchor.position + cameraOffset, rotation);
        }

        private void LoadGame()
        {
            if (loading) return;
            loading = true;
            SceneManager.LoadScene("PlayableForest");
        }

        private void OnGUI()
        {
            EnsureTexture();
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float captionFade = Mathf.Clamp01((Time.unscaledTime - shotStarted) / .35f);
            float blackAlpha = GetBlackOverlayAlpha();
            GUI.color = new Color(1f, 1f, 1f, captionFade * (1f - blackAlpha));
            Rect card = new Rect((width - 1080f) * .5f, height - 214f, 1080f, 154f);
            GUI.DrawTexture(card, darkTexture);
            GUI.Label(new Rect(card.x + 38f, card.y + 22f, card.width - 76f, 72f), CurrentCaption,
                new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.96f, .91f, .75f) } });
            GUI.Label(new Rect(card.x, card.y + 108f, card.width, 28f), $"{shot + 1} / {captions.Length}     CLICK OR PRESS SPACE",
                new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.65f, .81f, .66f) } });
            // Draw the transition last so neither the old nor new caption leaks through it.
            if (blackAlpha > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, blackAlpha);
                GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            GUI.matrix = old;
        }

        private float GetBlackOverlayAlpha()
        {
            if (loading) return 1f;
            if (transitioning)
            {
                float p = TransitionProgress;
                return p <= .5f ? p * 2f : (1f - p) * 2f;
            }
            return 1f - Mathf.Clamp01((Time.unscaledTime - initialFadeStarted) / (transitionDuration * .5f));
        }

        private void PlayClick()
        {
            if (uiAudio != null && clickSound != null) uiAudio.PlayOneShot(clickSound, .25f);
        }

        private static void EnsureTexture()
        {
            if (darkTexture != null) return;
            darkTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            darkTexture.SetPixel(0, 0, new Color(.015f, .055f, .032f, .9f));
            darkTexture.Apply(false, true);
        }
    }
}
