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
        [SerializeField, Range(.45f, .6f)] private float transitionDuration = .55f;
        [SerializeField, Range(15f, 80f)] private float charactersPerSecond = 38f;

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
        private Transform[] blueMushrooms;
        private Vector3[] mushroomBasePositions;
        private Quaternion[] mushroomBaseRotations;
        private Light[] mushroomLights;
        private static Texture2D darkTexture;

        public int CurrentShot => shot;
        public string CurrentCaption => captions[Mathf.Clamp(shot, 0, captions.Length - 1)];
        public bool IsTransitioning => transitioning;
        public float TransitionProgress => transitioning
            ? Mathf.Clamp01((Time.unscaledTime - transitionStarted) / transitionDuration)
            : 0f;
        public Vector3 CameraOffset => cameraOffset;
        public float CameraRotationOffsetDegrees => cameraRotationOffsetDegrees;
        public bool IsCaptionComplete => VisibleCharacterCount >= CurrentCaption.Length;
        public int VisibleCharacterCount => Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime - shotStarted) * charactersPerSecond), 0, CurrentCaption.Length);
        public int AnimatedMushroomCount => blueMushrooms == null ? 0 : blueMushrooms.Length;

        private void Awake()
        {
            if (cinematicCamera == null) cinematicCamera = Camera.main;
            CacheBlueMushrooms();
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
            UpdateMushroomPresentation(Time.unscaledTime);

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
            if (advance)
            {
                // A first press is always safe: it reveals the rest of the current sentence.
                // Only a second press advances to the next tableau.
                if (!IsCaptionComplete) { CompleteCaption(); return; }
                Advance();
            }
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

        private void CompleteCaption() => shotStarted = Time.unscaledTime - CurrentCaption.Length / charactersPerSecond;

        private void CacheBlueMushrooms()
        {
            var roots = new System.Collections.Generic.List<Transform>();
            foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                Transform mushroomRoot = null;
                for (Transform current = renderer.transform; current != null; current = current.parent)
                    if (current.name.IndexOf("Blue Root Mushroom", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        mushroomRoot = current;
                        break;
                    }

                // The cutscene props deliberately use these names.  Requiring that named ancestor
                // prevents a generic forest renderer from ever becoming an animated whole-shot root.
                if (mushroomRoot == null) continue;
                if (!roots.Contains(mushroomRoot)) roots.Add(mushroomRoot);
            }

            blueMushrooms = roots.ToArray();
            mushroomBasePositions = new Vector3[blueMushrooms.Length];
            mushroomBaseRotations = new Quaternion[blueMushrooms.Length];
            mushroomLights = new Light[blueMushrooms.Length];
            for (int i = 0; i < blueMushrooms.Length; i++)
            {
                mushroomBasePositions[i] = blueMushrooms[i].position;
                mushroomBaseRotations[i] = blueMushrooms[i].rotation;
                var lightObject = new GameObject("Blue Mushroom Glow") { hideFlags = HideFlags.DontSave };
                lightObject.transform.SetParent(blueMushrooms[i], false);
                lightObject.transform.localPosition = new Vector3(0f, .55f, 0f);
                mushroomLights[i] = lightObject.AddComponent<Light>();
                mushroomLights[i].type = LightType.Point;
                mushroomLights[i].color = new Color(.23f, .72f, 1f);
                mushroomLights[i].range = 2.2f;
                mushroomLights[i].intensity = .45f;
            }
        }

        private void UpdateMushroomPresentation(float time)
        {
            if (blueMushrooms == null) return;
            for (int i = 0; i < blueMushrooms.Length; i++)
            {
                Transform mushroom = blueMushrooms[i];
                if (mushroom == null) continue;
                float phase = time * 1.35f + i * 1.73f;
                mushroom.position = mushroomBasePositions[i] + Vector3.up * ((Mathf.Sin(phase) + 1f) * .045f);
                mushroom.rotation = mushroomBaseRotations[i] * Quaternion.Euler(0f, time * 16f, Mathf.Sin(phase) * 1.4f);
                if (mushroomLights[i] != null) mushroomLights[i].intensity = .38f + (Mathf.Sin(phase) + 1f) * .14f;
            }
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
            float captionFade = Mathf.Clamp01((Time.unscaledTime - shotStarted) / .22f);
            float blackAlpha = GetBlackOverlayAlpha();
            GUI.color = new Color(1f, 1f, 1f, captionFade * (1f - blackAlpha));
            Rect card = new Rect((width - 1160f) * .5f, height - 230f, 1160f, 170f);
            GUI.DrawTexture(card, darkTexture);
            GUI.color = new Color(.28f, .58f, .34f, captionFade * (1f - blackAlpha));
            GUI.DrawTexture(new Rect(card.x, card.y, 9f, card.height), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, captionFade * (1f - blackAlpha));
            GUI.Label(new Rect(card.x + 38f, card.y + 17f, 270f, 27f), "FOREST GUARDIAN",
                new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(.54f, .86f, .57f) } });
            string visibleCaption = CurrentCaption.Substring(0, VisibleCharacterCount);
            GUI.Label(new Rect(card.x + 38f, card.y + 45f, card.width - 76f, 78f), visibleCaption,
                new GUIStyle(GUI.skin.label) { fontSize = 27, fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.UpperLeft, normal = { textColor = new Color(.96f, .91f, .75f) } });
            string prompt = IsCaptionComplete ? "CLICK / SPACE / ENTER — CONTINUE" : "CLICK / SPACE / ENTER — REVEAL TEXT";
            GUI.Label(new Rect(card.x + 38f, card.y + 132f, card.width - 76f, 24f), prompt,
                new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(.65f, .81f, .66f) } });
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
