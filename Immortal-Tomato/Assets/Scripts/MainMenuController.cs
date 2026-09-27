using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds menu interaction around artwork containing the navigation labels.</summary>
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] Texture2D background;
    [SerializeField] Texture2D infoBackground;
    [SerializeField] AudioClip music;
    [SerializeField] Shader wallpaperLightingShader;
    [SerializeField] string gameplayScene = "Main";

    const string MutedKey = "ImmortalTomato.MenuMuted";

    AudioSource audioSource;
    Image muteIndicator;
    GameObject activeModal;
    RawImage backdrop;
    Button[] menuButtons;
    Button playButton;
    Button muteButton;
    Text loadingQuote;
    bool isLoading;
    Material wallpaperLightingMaterial;
    MenuSelectionFeedback[] menuSelections;
    MenuSelectionFeedback currentMenuSelection;
    static Sprite roundedUiSprite;

    void Awake()
    {
        audioSource = MenuMusicSession.Ensure(music);
        CreateMenuCamera();
        BuildUi();
        ApplySavedSettings();
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            TryStartMusic();
    }

    static void CreateMenuCamera()
    {
        if (Camera.main != null && Camera.main.isActiveAndEnabled) return;
        var cameraObject = new GameObject("Main Menu Camera");
        cameraObject.tag = "MainCamera";
        var menuCamera = cameraObject.AddComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = Color.black;
        menuCamera.cullingMask = 0;
        menuCamera.orthographic = true;
    }

    void Update()
    {
        // WebGL permits music only after a gesture, so retry there on input.
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            TryStartMusic();
        if (!isLoading && activeModal != null && Input.GetKeyDown(KeyCode.Escape))
            CloseModal();
        UpdateMenuSelectionFeedback();
    }

    void OnDestroy()
    {
        SetLoadingPlateProgress(0f, false);
        if (wallpaperLightingMaterial != null) Destroy(wallpaperLightingMaterial);
    }

    void TryStartMusic()
    {
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    void Play()
    {
        TryStartMusic();
        if (!isLoading)
            StartCoroutine(LoadGameplay());
    }

    void ToggleMuted()
    {
        if (audioSource == null) return;
        audioSource.mute = !audioSource.mute;
        PlayerPrefs.SetInt(MutedKey, audioSource.mute ? 1 : 0);
        PlayerPrefs.Save();
        UpdateMuteIndicator();
        TryStartMusic();
    }

    void ApplySavedSettings()
    {
        if (audioSource == null) return;
        // The wall radio is deliberately a mute switch, not a volume control.
        // Its painted dial remains static and there is no hidden slider state.
        audioSource.volume = 1f;
        audioSource.mute = PlayerPrefs.GetInt(MutedKey, 0) != 0;
        UpdateMuteIndicator();
    }

    void UpdateMuteIndicator()
    {
        if (muteIndicator != null)
            muteIndicator.enabled = audioSource != null && audioSource.mute;
    }

    void BuildUi()
    {
        if (background != null)
            background.wrapMode = TextureWrapMode.Clamp;
        var canvasObject = new GameObject("Main Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;

        var eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
            eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).GetComponent<EventSystem>();

        backdrop = CreateRawImage("Background", canvasObject.transform, Color.white, background);
        backdrop.raycastTarget = false;
        Stretch(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        wallpaperLightingMaterial = CreateWallpaperLightingMaterial();
        if (wallpaperLightingMaterial != null)
            backdrop.material = wallpaperLightingMaterial;

        // The labels are painted into the three brass signs. These are transparent
        // hit areas; their only visible feedback remains the wallpaper shader.
        playButton = CreateWallpaperHotspot("Play Game Hotspot", canvasObject.transform,
            new Vector2(.060f, .705f), new Vector2(.322f, .892f),
            new Vector2(.063f, .711f), new Vector2(.318f, .889f), new Vector2(.190f, .800f), Play);
        var howToPlayButton = CreateWallpaperHotspot("How To Play Hotspot", canvasObject.transform,
            new Vector2(.060f, .570f), new Vector2(.322f, .746f),
            new Vector2(.063f, .580f), new Vector2(.318f, .739f), new Vector2(.190f, .659f), ShowHowToPlay);
        var creditsButton = CreateWallpaperHotspot("Credits Hotspot", canvasObject.transform,
            new Vector2(.060f, .430f), new Vector2(.322f, .595f),
            new Vector2(.063f, .441f), new Vector2(.318f, .586f), new Vector2(.190f, .514f), ShowCredits);
        ConfigureMenuNavigation(playButton, howToPlayButton, creditsButton);
        menuButtons = new[] { playButton, howToPlayButton, creditsButton };
        SelectMenuItem(playButton.GetComponent<MenuSelectionFeedback>());
        (EventSystem.current ?? eventSystem).SetSelectedGameObject(playButton.gameObject);
        BuildMusicControls(canvasObject.transform);
    }

    void BuildMusicControls(Transform parent)
    {
        // The radio is baked into the wallpaper. Only its speaker is clickable,
        // acting as a simple mute/unmute switch; the painted dial is ornamental.
        var muteHotspot = CreateImage("Baked Radio Mute Hotspot", parent, new Color(1f, 1f, 1f, .001f));
        Stretch(muteHotspot.rectTransform, new Vector2(.354f, .665f), new Vector2(.379f, .711f), Vector2.zero, Vector2.zero);
        muteButton = muteHotspot.gameObject.AddComponent<Button>();
        muteButton.targetGraphic = muteHotspot;
        var muteColors = muteButton.colors;
        muteColors.normalColor = Color.white;
        muteColors.highlightedColor = new Color(.72f, .96f, 1f, 1f);
        muteColors.pressedColor = new Color(.42f, .75f, .9f, 1f);
        muteColors.fadeDuration = .08f;
        muteButton.colors = muteColors;
        muteButton.onClick.AddListener(ToggleMuted);
        // This appears only while muted and remains inside the baked speaker.
        muteIndicator = CreateImage("Baked Speaker Mute Slash", parent, new Color(.48f, .045f, .018f, .88f));
        muteIndicator.raycastTarget = false;
        Stretch(muteIndicator.rectTransform, new Vector2(.357f, .680f), new Vector2(.376f, .687f), Vector2.zero, Vector2.zero);
        muteIndicator.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -42f);
    }

    Button CreateWallpaperHotspot(string name, Transform parent, Vector2 hitMin, Vector2 hitMax, Vector2 visualMin, Vector2 visualMax, Vector2 lampAnchor, UnityEngine.Events.UnityAction action)
    {
        var image = CreateImage(name, parent, new Color(1f, .73f, .28f, .012f));
        Stretch(image.rectTransform, hitMin, hitMax, Vector2.zero, Vector2.zero);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0f;
        button.colors = colors;
        button.onClick.AddListener(action);
        image.gameObject.AddComponent<MenuSelectionFeedback>().Initialize(this, image, visualMin, visualMax, lampAnchor);
        return button;
    }

    void ConfigureMenuNavigation(Button playButton, Button howToPlayButton, Button creditsButton)
    {
        ConfigureVerticalNavigation(playButton, creditsButton, howToPlayButton);
        ConfigureVerticalNavigation(howToPlayButton, playButton, creditsButton);
        ConfigureVerticalNavigation(creditsButton, howToPlayButton, playButton);
        menuSelections = new[]
        {
            playButton.GetComponent<MenuSelectionFeedback>(),
            howToPlayButton.GetComponent<MenuSelectionFeedback>(),
            creditsButton.GetComponent<MenuSelectionFeedback>()
        };
    }

    static void ConfigureVerticalNavigation(Button button, Button up, Button down)
    {
        var navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = up,
            selectOnDown = down,
            selectOnLeft = null,
            selectOnRight = null
        };
        button.navigation = navigation;
    }

    /// <summary>Called by each wallpaper hotspot for pointer and EventSystem selection alike.</summary>
    public void SelectMenuItem(MenuSelectionFeedback selection)
    {
        if (selection == null) return;
        currentMenuSelection = selection;
        if (menuSelections == null) return;
        for (int i = 0; i < menuSelections.Length; i++)
            menuSelections[i].SetSelected(menuSelections[i] == currentMenuSelection);
    }

    void UpdateMenuSelectionFeedback()
    {
        if (menuSelections == null) return;
        float time = Time.unscaledTime;
        if (activeModal != null)
        {
            ApplyWallpaperLighting(null, 0f, time);
            return;
        }
        MenuSelectionFeedback strongest = null;
        float strongestStrength = 0f;
        for (int i = 0; i < menuSelections.Length; i++)
        {
            var selection = menuSelections[i];
            selection.UpdateVisual(time);
            if (selection.LightingStrength > strongestStrength)
            {
                strongest = selection;
                strongestStrength = selection.LightingStrength;
            }
        }
        ApplyWallpaperLighting(strongest, strongestStrength, time);
    }

    Material CreateWallpaperLightingMaterial()
    {
        var material = wallpaperLightingShader == null
            ? null
            : new Material(wallpaperLightingShader) { name = "Menu Wallpaper Lighting" };
        return material;
    }

    void ApplyWallpaperLighting(MenuSelectionFeedback selection, float strength, float time)
    {
        if (wallpaperLightingMaterial == null) return;
        wallpaperLightingMaterial.SetFloat("_AmbientTime", time);
        if (selection == null || strength <= .001f)
        {
            wallpaperLightingMaterial.SetFloat("_SelectionStrength", 0f);
            wallpaperLightingMaterial.SetFloat("_PressFlash", 0f);
            return;
        }
        wallpaperLightingMaterial.SetVector("_SelectionRect", selection.VisualRect);
        wallpaperLightingMaterial.SetVector("_LampAnchor", selection.LampAnchor);
        wallpaperLightingMaterial.SetFloat("_SelectionStrength", strength);
        wallpaperLightingMaterial.SetFloat("_PressFlash", selection.PressFlash);
        // A deliberate, slow pulse makes the selected brass sign feel powered,
        // while leaving the rest of the room completely still.
        wallpaperLightingMaterial.SetFloat("_Pulse", .5f + .5f * Mathf.Sin(time * 3.35f));
    }

    void ShowHowToPlay()
    {
        ShowModal("HOW TO PLAY", "WASD  MOVE\n\nSPACE  JUMP\n\nLEFT CLICK / J  SHOOT");
    }
    void ShowCredits() => ShowModal("CREDITS", "Made by thegm26\n\nMusic: menu theme");

    IEnumerator LoadGameplay()
    {
        isLoading = true;
        SetLoadingPlateProgress(0f, true);
        SetMenuHotspotsEnabled(false);
        if (muteButton != null) muteButton.interactable = false;
        if (infoBackground != null) backdrop.texture = infoBackground;
        ApplyWallpaperLighting(null, 0f, Time.unscaledTime);

        activeModal = new GameObject("Loading Board", typeof(RectTransform));
        activeModal.GetComponent<RectTransform>().SetParent(FindFirstObjectByType<Canvas>().transform, false);
        Stretch(activeModal.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        BuildLoadingBoard(activeModal.transform);

        // Let the finished board render once before the scene begins preparing.
        yield return null;
        if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
        {
            Debug.LogError("Main menu could not load gameplay scene: " + gameplayScene);
            RestoreMenuAfterLoadFailure();
            yield break;
        }

        AsyncOperation loadOperation;
        try
        {
            loadOperation = SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Single);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            RestoreMenuAfterLoadFailure();
            yield break;
        }
        if (loadOperation == null)
        {
            Debug.LogError("Main menu received no async operation for scene: " + gameplayScene);
            RestoreMenuAfterLoadFailure();
            yield break;
        }

        loadOperation.allowSceneActivation = false;
        const float minimumDisplayTime = 1.85f;
        const float quoteInterval = 1.12f;
        float elapsed = 0f;
        float displayedProgress = 0f;
        string[] quotes =
        {
            "Reloading the sauce...",
            "Ketchup is just tomato armor.",
            "A balanced diet includes two Uzis.",
            "Serving justice, extra spicy.",
            "No vegetables were harmed. They fought back."
        };
        int quoteIndex = -1;

        while (elapsed < minimumDisplayTime || loadOperation.progress < .9f)
        {
            elapsed += Time.unscaledDeltaTime;
            float targetProgress = Mathf.Clamp01(loadOperation.progress / .9f) * .9f;
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * .72f);
            UpdateLoadingPlate(displayedProgress);

            int nextQuoteIndex = Mathf.FloorToInt(elapsed / quoteInterval) % quotes.Length;
            if (nextQuoteIndex != quoteIndex)
            {
                quoteIndex = nextQuoteIndex;
                if (loadingQuote != null) loadingQuote.text = quotes[quoteIndex];
            }
            yield return null;
        }

        // Finish the painted brass plate before the scene switch so it reads as a
        // deliberate transition even when Main is already warm in memory.
        UpdateLoadingPlate(1f);
        yield return new WaitForSecondsRealtime(.16f);
        loadOperation.allowSceneActivation = true;
    }

    void BuildLoadingBoard(Transform parent)
    {
        CreateBoardText("Loading Board Heading", parent, "LOADING...", 38, FontStyle.Bold,
            TextAnchor.UpperCenter, new Vector2(.080f, .754f), new Vector2(.298f, .844f), -8.5f);
        loadingQuote = CreateBoardText("Loading Board Quote", parent, "Reloading the sauce...", 25, FontStyle.Bold,
            TextAnchor.UpperCenter, new Vector2(.078f, .555f), new Vector2(.300f, .726f), -8.5f);

        // Progress is lit directly inside the existing painted brass plate by
        // MenuWallpaperLighting. There is intentionally no separate rail, fill,
        // frame, or other UI graphic above the illustration.
        UpdateLoadingPlate(0f);
    }

    void UpdateLoadingPlate(float progress)
    {
        SetLoadingPlateProgress(progress, true);
    }

    void SetLoadingPlateProgress(float progress, bool active)
    {
        if (wallpaperLightingMaterial == null) return;
        wallpaperLightingMaterial.SetFloat("_LoadingActive", active ? 1f : 0f);
        wallpaperLightingMaterial.SetFloat("_LoadingProgress", Mathf.Clamp01(progress));
    }

    void RestoreMenuAfterLoadFailure()
    {
        isLoading = false;
        SetLoadingPlateProgress(0f, false);
        if (activeModal != null) Destroy(activeModal);
        activeModal = null;
        loadingQuote = null;
        if (background != null) backdrop.texture = background;
        SetMenuHotspotsEnabled(true);
        if (muteButton != null) muteButton.interactable = true;
        if (playButton != null) (EventSystem.current)?.SetSelectedGameObject(playButton.gameObject);
    }

    void ShowModal(string heading, string body)
    {
        if (isLoading) return;
        if (activeModal != null) Destroy(activeModal);
        SetMenuHotspotsEnabled(false);
        if (infoBackground != null)
            backdrop.texture = infoBackground;
        ApplyWallpaperLighting(null, 0f, Time.unscaledTime);
        activeModal = new GameObject(heading + " Panel", typeof(RectTransform));
        var modalRect = activeModal.GetComponent<RectTransform>();
        modalRect.SetParent(FindFirstObjectByType<Canvas>().transform, false);
        // The left board and brass plate are already part of the illustration.
        Stretch(modalRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        // The steel board slopes down towards the right in the wallpaper, so the
        // lettering follows that slope rather than reading as a flat UI layer.
        CreateBoardText("Info Board Heading", activeModal.transform, heading, 37, FontStyle.Bold,
            TextAnchor.UpperCenter, new Vector2(.080f, .754f), new Vector2(.298f, .844f), -8.5f);
        CreateBoardText("Info Board Copy", activeModal.transform, body, 28,
            FontStyle.Bold, TextAnchor.UpperCenter, new Vector2(.078f, .535f), new Vector2(.300f, .742f), -8.5f);

        var back = CreateImage("Baked Brass Back Hotspot", activeModal.transform, new Color(1f, 1f, 1f, .001f));
        Stretch(back.rectTransform, new Vector2(.108f, .429f), new Vector2(.281f, .509f), Vector2.zero, Vector2.zero);
        var closeButton = back.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = back;
        closeButton.onClick.AddListener(CloseModal);
        CreateBoardText("Baked Brass Back Label", activeModal.transform, "BACK", 24, FontStyle.Bold,
            TextAnchor.MiddleCenter, new Vector2(.108f, .429f), new Vector2(.281f, .509f), 0f, false);
    }

    void SetMenuHotspotsEnabled(bool enabled)
    {
        if (menuButtons == null) return;
        for (int i = 0; i < menuButtons.Length; i++)
            menuButtons[i].interactable = enabled;
    }

    void CloseModal()
    {
        if (isLoading) return;
        if (activeModal == null) return;
        Destroy(activeModal);
        activeModal = null;
        if (background != null)
            backdrop.texture = background;
        SetMenuHotspotsEnabled(true);
        if (playButton != null)
            (EventSystem.current)?.SetSelectedGameObject(playButton.gameObject);
    }

    static RawImage CreateRawImage(string name, Transform parent, Color color, Texture texture)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.texture = texture;
        return image;
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        return image;
    }

    static Image CreateRoundedImage(string name, Transform parent, Color color)
    {
        var image = CreateImage(name, parent, color);
        if (roundedUiSprite == null)
        {
            const int textureSize = 64;
            const float radius = 18f;
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = "Menu Rounded UI Sprite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < textureSize; y++)
            for (int x = 0; x < textureSize; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - textureSize * .5f) - (textureSize * .5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - textureSize * .5f) - (textureSize * .5f - radius), 0f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float edge = Mathf.Clamp01((distance - (radius - 1.25f)) / 2f);
                float alpha = 1f - edge * edge * (3f - 2f * edge);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply();
            roundedUiSprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
        image.sprite = roundedUiSprite;
        image.type = Image.Type.Sliced;
        return image;
    }

    static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Color color)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        return text;
    }

    // Warm lettering sits directly on the left painted board; the dark offset
    // keeps it legible without adding a card or any door overlay.
    static Text CreateBoardText(string name, Transform parent, string value, int size, FontStyle style,
        TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, float rotation = 0f, bool agePaint = true)
    {
        // Keep the lettering deliberately dull and uneven, like paint that has
        // lived on the steel board, rather than a bright UI label on top of it.
        var shadow = CreateText(name + " Shadow", parent, value, size, style, new Color(.075f, .030f, .010f, .54f));
        shadow.alignment = alignment;
        shadow.raycastTarget = false;
        Stretch(shadow.rectTransform, anchorMin, anchorMax, new Vector2(.75f, -.9f), new Vector2(.75f, -.9f));
        shadow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        if (agePaint)
        {
            var shadowPaint = shadow.gameObject.AddComponent<BoardPaintEffect>();
            shadowPaint.isShadow = true;
        }

        var face = CreateText(name, parent, value, size, style, new Color(.96f, .73f, .34f, .98f));
        face.alignment = alignment;
        face.raycastTarget = false;
        Stretch(face.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        face.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        if (agePaint)
            face.gameObject.AddComponent<BoardPaintEffect>();
        return face;
    }

    static Button CreateTextButton(string name, Transform parent, string label, Color backgroundColor, Color labelColor)
    {
        var image = CreateImage(name, parent, backgroundColor);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var text = CreateText("Label", image.transform, label, 19, FontStyle.Bold, labelColor);
        text.alignment = TextAnchor.MiddleCenter;
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, .84f, .55f, 1f);
        colors.pressedColor = new Color(.78f, .48f, .22f, 1f);
        colors.fadeDuration = .08f;
        button.colors = colors;
        return button;
    }

    static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

}

/// <summary>
/// Gives the otherwise-clean legacy text a tiny amount of painted-metal age and
/// follows the board's perspective: its lower edge is fractionally narrower.
/// It never adds another graphic or a card above the wallpaper.
/// </summary>
public sealed class BoardPaintEffect : BaseMeshEffect
{
    [HideInInspector] public bool isShadow;

    public override void ModifyMesh(VertexHelper vertexHelper)
    {
        if (!IsActive()) return;

        int count = vertexHelper.currentVertCount;
        if (count == 0) return;

        // Text emits one quad per glyph. Work in its local bounds so the effect
        // remains correct at every screen resolution.
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        UIVertex vertex = default(UIVertex);
        for (int i = 0; i < count; i++)
        {
            vertexHelper.PopulateUIVertex(ref vertex, i);
            minY = Mathf.Min(minY, vertex.position.y);
            maxY = Mathf.Max(maxY, vertex.position.y);
        }
        float height = Mathf.Max(.001f, maxY - minY);

        for (int i = 0; i < count; i++)
        {
            vertexHelper.PopulateUIVertex(ref vertex, i);
            float fromBottom = Mathf.Clamp01((vertex.position.y - minY) / height);
            // The board is a touch broader at its upper edge. This is subtle
            // enough to retain legibility but binds the lettering to its face.
            float taper = Mathf.Lerp(.935f, 1.005f, fromBottom);
            vertex.position.x *= taper;

            int glyph = i / 4;
            float grain = Hash01(glyph * 17 + (i & 3) * 31);
            float shade = isShadow ? .90f + grain * .08f : .90f + grain * .10f;
            Color32 color = vertex.color;
            color.r = (byte)Mathf.Clamp(color.r * shade, 0f, 255f);
            color.g = (byte)Mathf.Clamp(color.g * shade, 0f, 255f);
            color.b = (byte)Mathf.Clamp(color.b * (.94f + grain * .06f), 0f, 255f);
            color.a = (byte)Mathf.Clamp(color.a * (isShadow ? .90f + grain * .07f : .95f + grain * .05f), 0f, 255f);
            vertex.color = color;
            vertexHelper.SetUIVertex(vertex, i);
        }
    }

    static float Hash01(int value)
    {
        value = (value << 13) ^ value;
        return 1f - ((value * (value * value * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
    }
}

/// <summary>Routes selection state into the wallpaper material; it creates no visual overlay objects.</summary>
public sealed class MenuSelectionFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerDownHandler
{
    MainMenuController owner;
    Image hotspot;
    Vector4 visualRect;
    Vector4 lampAnchor;
    bool selected;
    float selectionBlend;
    float pressedUntil;

    public Vector4 VisualRect => visualRect;
    public Vector4 LampAnchor => lampAnchor;
    public float PressFlash => Mathf.Clamp01((pressedUntil - Time.unscaledTime) / .16f);
    public float LightingStrength => Mathf.Clamp01(selectionBlend + PressFlash * .35f);

    public void Initialize(MainMenuController menuOwner, Image hotspotImage, Vector2 visualMin, Vector2 visualMax, Vector2 lampPosition)
    {
        owner = menuOwner;
        hotspot = hotspotImage;
        visualRect = new Vector4(visualMin.x, visualMin.y, visualMax.x, visualMax.y);
        lampAnchor = new Vector4(lampPosition.x, lampPosition.y, 0f, 0f);
        UpdateVisual(Time.unscaledTime);
    }

    public void SetSelected(bool value)
    {
        selected = value;
    }

    public void UpdateVisual(float time)
    {
        if (hotspot == null) return;
        selectionBlend = Mathf.MoveTowards(selectionBlend, selected ? 1f : 0f, Time.unscaledDeltaTime * 10f);
        // The hit target stays effectively invisible: all feedback is baked into
        // the backdrop shader, so no edge can sit above the illustration.
        hotspot.color = new Color(1f, 1f, 1f, .001f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        owner.SelectMenuItem(this);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        owner.SelectMenuItem(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        owner.SelectMenuItem(this);
        pressedUntil = Time.unscaledTime + .14f;
    }
}

/// <summary>Persistent, menu-owned audio session. It deliberately has no UI.</summary>
public sealed class MenuMusicSession : MonoBehaviour
{
    static MenuMusicSession instance;
    AudioSource source;

    public static AudioSource Ensure(AudioClip clip)
    {
        if (instance == null)
        {
            var session = new GameObject("Menu Music Session");
            instance = session.AddComponent<MenuMusicSession>();
            UnityEngine.Object.DontDestroyOnLoad(session);
        }
        if (instance.source.clip != clip)
        {
            instance.source.clip = clip;
            instance.source.loop = true;
            instance.source.playOnAwake = false;
            instance.source.spatialBlend = 0f;
        }
        return instance.source;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        source = gameObject.AddComponent<AudioSource>();
        gameObject.AddComponent<AudioListener>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }
}
