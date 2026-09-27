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
        private float mushroomPresentationStarted;
        private Vector3 cameraOffset;
        private float cameraRotationOffsetDegrees;
        private Transform[] blueMushrooms;
        private Vector3[] mushroomBasePositions;
        private Quaternion[] mushroomBaseRotations;
        private Light[] mushroomLights;
        private static Texture2D darkTexture;
        private static Texture2D portraitBackdropTexture;
        private static Texture2D dialogueTextBackdropTexture;
        private Texture2D guardianPortrait;
        private Camera portraitCamera;
        private GameObject portraitGuardian;
        private readonly System.Collections.Generic.List<Camera> portraitExcludedCameras = new System.Collections.Generic.List<Camera>();
        private readonly System.Collections.Generic.List<int> portraitExcludedCameraMasks = new System.Collections.Generic.List<int>();

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
            // Cutscene props must begin from the same phase every time the scene is entered.
            mushroomPresentationStarted = Time.unscaledTime;
            CacheBlueMushrooms();
            UpdateMushroomPresentation(0f);
            IsolatePortraitLayer();
            CreateGuardianPortrait();
            ShowShot(0);
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
            UpdateMushroomPresentation(Time.unscaledTime - mushroomPresentationStarted);

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
                // Start the replacement caption once, while the screen is fully black.
                // Do not restart it when the fade finishes or the first letters visibly flash
                // and then begin again as the new tableau is revealed.
                if (!transitionLoadsGame) ShowShot(pendingShot);
            }

            if (progress < 1f) return;
            transitioning = false;
            if (transitionLoadsGame) LoadGame();
            // ShowShot already set shotStarted at the opaque midpoint. Leaving it alone here
            // keeps the replacement typewriter continuously progressing through the reveal.
        }

        private void ShowShot(int index)
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

        private void CreateGuardianPortrait()
        {
            Transform source = null;
            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == "Shot1 Guardian")
                {
                    source = candidate;
                    break;
                }
            }

            if (source == null) return;

            RenderTexture portraitTarget = null;
            Texture2D capturedPortrait = null;
            try
            {
                portraitGuardian = Instantiate(source.gameObject, new Vector3(0f, -10000f, 0f), source.rotation);
                portraitGuardian.name = "Guardian Dialogue Portrait";
                SetLayerRecursively(portraitGuardian.transform, 31);
                SampleGuardianIdlePose();
                foreach (Behaviour behaviour in portraitGuardian.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
                foreach (AudioSource sourceAudio in portraitGuardian.GetComponentsInChildren<AudioSource>(true)) sourceAudio.mute = true;

                Bounds bounds = GetRendererBounds(portraitGuardian);
                if (bounds.size.sqrMagnitude <= 0f) return;

                // Capture display colour data in the active project colour space, then mark the
                // sampled texture as colour data. This preserves the Gamma project today and
                // avoids a second gamma conversion if the project later switches to Linear.
                var descriptor = new RenderTextureDescriptor(256, 256, RenderTextureFormat.ARGB32, 16)
                {
                    sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
                };
                portraitTarget = new RenderTexture(descriptor)
                {
                    name = "Guardian Dialogue Portrait Capture",
                    hideFlags = HideFlags.DontSave
                };
                portraitTarget.Create();

                GameObject cameraObject = new GameObject("Guardian Dialogue Portrait Camera") { hideFlags = HideFlags.DontSave };
                portraitCamera = cameraObject.AddComponent<Camera>();
                portraitCamera.enabled = false;
                portraitCamera.cullingMask = 1 << 31;
                portraitCamera.clearFlags = CameraClearFlags.SolidColor;
                portraitCamera.backgroundColor = new Color(.025f, .12f, .08f, 0f);
                portraitCamera.orthographic = true;
                portraitCamera.targetTexture = portraitTarget;
                portraitCamera.nearClipPlane = .01f;
                portraitCamera.farClipPlane = 50f;
                portraitCamera.transform.position = bounds.center + new Vector3(-1.4f, 1.15f, 4.5f);
                portraitCamera.transform.LookAt(bounds.center + Vector3.up * .15f);
                portraitCamera.orthographicSize = Mathf.Max(bounds.size.y * .66f, bounds.size.x * .92f);
                portraitCamera.Render();

                // RenderTextures can retain platform-dependent contents after their source scene
                // object is gone, notably on WebGL. Copy this one capture into persistent CPU
                // backed texture data while its target is valid.
                capturedPortrait = new Texture2D(256, 256, TextureFormat.RGBA32, false, false)
                {
                    name = "Guardian Dialogue Portrait",
                    hideFlags = HideFlags.DontSave
                };
                RenderTexture previousActive = RenderTexture.active;
                try
                {
                    RenderTexture.active = portraitTarget;
                    capturedPortrait.ReadPixels(new Rect(0f, 0f, portraitTarget.width, portraitTarget.height), 0, 0, false);
                    capturedPortrait.Apply(false, false);
                }
                finally
                {
                    RenderTexture.active = previousActive;
                }
                guardianPortrait = capturedPortrait;
                capturedPortrait = null;
            }
            finally
            {
                // The staged guardian is only a source for the dialogue portrait. Keeping the
                // scene object active puts a large back-facing body in the first shot.
                source.gameObject.SetActive(false);

                if (portraitCamera != null)
                {
                    portraitCamera.targetTexture = null;
                    Destroy(portraitCamera.gameObject);
                    portraitCamera = null;
                }
                if (portraitTarget != null)
                {
                    portraitTarget.Release();
                    Destroy(portraitTarget);
                }
                if (capturedPortrait != null) Destroy(capturedPortrait);
                if (portraitGuardian != null)
                {
                    portraitGuardian.SetActive(false);
                    Destroy(portraitGuardian);
                    portraitGuardian = null;
                }
            }
        }

        private void SampleGuardianIdlePose()
        {
            foreach (Animator animator in portraitGuardian.GetComponentsInChildren<Animator>(true))
            {
                animator.applyRootMotion = false;
                animator.Rebind();
                animator.Play("Base Layer.ITHappy Idle", 0, .22f);
                animator.Update(0f);
            }
        }

        private static Bounds GetRendererBounds(GameObject target)
        {
            // Shot1 Guardian contains an inactive Tree Form Visual with a much larger bound than
            // the active child guardian.  It must not influence the portrait camera framing.
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(target.transform.position, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void SetLayerRecursively(Transform target, int layer)
        {
            target.gameObject.layer = layer;
            for (int i = 0; i < target.childCount; i++) SetLayerRecursively(target.GetChild(i), layer);
        }

        private void IsolatePortraitLayer()
        {
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                portraitExcludedCameras.Add(camera);
                portraitExcludedCameraMasks.Add(camera.cullingMask);
                camera.cullingMask &= ~(1 << 31);
            }
        }

        private void OnGUI()
        {
            EnsureTexture();
            float width = Screen.width;
            float height = Screen.height;
            float uiScale = Mathf.Clamp(Mathf.Min(width / 1280f, height / 720f), .72f, 1.55f);
            float captionFade = Mathf.Clamp01((Time.unscaledTime - shotStarted) / .22f);
            float blackAlpha = GetBlackOverlayAlpha();
            GUI.color = new Color(1f, 1f, 1f, captionFade * (1f - blackAlpha));
            float cardHeight = 220f * uiScale;
            Rect card = new Rect(0f, height - cardHeight, width, cardHeight);
            GUI.DrawTexture(card, darkTexture);
            GUI.color = new Color(.36f, .82f, .47f, captionFade * (1f - blackAlpha));
            GUI.DrawTexture(new Rect(card.x, card.y, card.width, 3f * uiScale), Texture2D.whiteTexture);

            float portraitSize = Mathf.Min(card.height - 28f * uiScale, 176f * uiScale);
            Rect portraitRect = new Rect(24f * uiScale, card.y + (card.height - portraitSize) * .5f, portraitSize, portraitSize);
            GUI.color = new Color(1f, 1f, 1f, captionFade * (1f - blackAlpha));
            GUI.DrawTexture(portraitRect, portraitBackdropTexture);
            if (guardianPortrait != null) GUI.DrawTexture(portraitRect, guardianPortrait, ScaleMode.ScaleToFit, true);
            GUI.color = new Color(.48f, .92f, .58f, captionFade * (1f - blackAlpha));
            GUI.DrawTexture(new Rect(portraitRect.x, portraitRect.y, portraitRect.width, 2f * uiScale), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(portraitRect.x, portraitRect.yMax - 2f * uiScale, portraitRect.width, 2f * uiScale), Texture2D.whiteTexture);

            float textLeft = portraitRect.xMax + 28f * uiScale;
            float textRight = 30f * uiScale;
            Rect textSafeArea = new Rect(textLeft - 12f * uiScale, card.y + 8f * uiScale,
                card.width - textLeft - textRight + 12f * uiScale, card.height - 16f * uiScale);
            // Keep the cutscene visible behind the dialogue while a shadow maintains contrast.
            GUI.color = new Color(1f, 1f, 1f, captionFade * (1f - blackAlpha));
            GUI.DrawTexture(textSafeArea, dialogueTextBackdropTexture);
            GUIStyle captionStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(27f * uiScale), fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.UpperLeft, normal = { textColor = new Color(.98f, .95f, .82f) } };
            GUIStyle captionShadowStyle = new GUIStyle(captionStyle) { normal = { textColor = new Color(0f, 0f, 0f, .9f) } };
            GUIStyle promptStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(13f * uiScale), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(.69f, .86f, .7f) } };
            GUIStyle promptShadowStyle = new GUIStyle(promptStyle) { normal = { textColor = new Color(0f, 0f, 0f, .9f) } };
            GUI.color = new Color(1f, 1f, 1f, captionFade * (1f - blackAlpha));
            string visibleCaption = CurrentCaption.Substring(0, VisibleCharacterCount);
            // The portrait already establishes the speaker, so use the recovered title space
            // for the story itself rather than repeating a name label.
            Rect captionRect = new Rect(textLeft, card.y + 27f * uiScale, card.width - textLeft - textRight, card.height - 73f * uiScale);
            GUI.Label(new Rect(captionRect.x + 2f, captionRect.y + 2f, captionRect.width, captionRect.height), visibleCaption, captionShadowStyle);
            GUI.Label(captionRect, visibleCaption, captionStyle);
            string prompt = IsCaptionComplete ? "CLICK / SPACE / ENTER — CONTINUE" : "CLICK / SPACE / ENTER — REVEAL TEXT";
            Rect promptRect = new Rect(textLeft, card.yMax - 31f * uiScale, card.width - textLeft - textRight, 21f * uiScale);
            GUI.Label(new Rect(promptRect.x + 1f, promptRect.y + 1f, promptRect.width, promptRect.height), prompt, promptShadowStyle);
            GUI.Label(promptRect, prompt, promptStyle);
            // Draw the transition last so neither the old nor new caption leaks through it.
            if (blackAlpha > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, blackAlpha);
                GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        private float GetBlackOverlayAlpha()
        {
            if (loading) return 1f;
            if (transitioning)
            {
                float p = TransitionProgress;
                // A shot change uses the second half to reveal its replacement.  The final
                // transition has no replacement in this scene: revealing it again for that
                // half-frame flashes the final tableau just before PlayableForest loads.
                // Reach black at the normal midpoint, then keep it there until scene loading.
                if (transitionLoadsGame) return Mathf.Clamp01(p * 2f);
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
            // Layered with the .6 dialogue backing this lands at roughly .64 effective opacity.
            darkTexture.SetPixel(0, 0, new Color(.008f, .04f, .022f, .1f));
            darkTexture.Apply(false, true);
            portraitBackdropTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            portraitBackdropTexture.SetPixel(0, 0, new Color(.035f, .17f, .095f, .95f));
            portraitBackdropTexture.Apply(false, true);
            dialogueTextBackdropTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            dialogueTextBackdropTexture.SetPixel(0, 0, new Color(.008f, .04f, .022f, .6f));
            dialogueTextBackdropTexture.Apply(false, true);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < portraitExcludedCameras.Count; i++)
                if (portraitExcludedCameras[i] != null) portraitExcludedCameras[i].cullingMask = portraitExcludedCameraMasks[i];
            if (portraitCamera != null) Destroy(portraitCamera.gameObject);
            if (portraitGuardian != null) Destroy(portraitGuardian);
            if (guardianPortrait != null) Destroy(guardianPortrait);
        }

    }
}
