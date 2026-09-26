using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BeforeTheAxes
{
    /// <summary>Self-contained, WebGL-safe IMGUI menu over the scene's 3D forest backdrop.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        public enum MenuPage { Main, Help, Credits }

        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip clickSound;

        private static Texture2D panelTexture;
        private static Texture2D buttonTexture;
        private static Texture2D hoverTexture;
        private static Texture2D pressedTexture;
        private MenuPage page;

        public MenuPage ActivePage => page;
        public bool HasClickSound => uiAudioSource != null && clickSound != null;

        private void Awake() => page = MenuPage.Main;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && page != MenuPage.Main)
                ShowMain();
        }

        public void StartGame()
        {
            PlayClick();
            SceneManager.LoadScene("PlayableForest");
        }

        public void ShowHelp()
        {
            PlayClick();
            page = MenuPage.Help;
        }

        public void ShowCredits()
        {
            PlayClick();
            page = MenuPage.Credits;
        }

        public void ShowMain()
        {
            if (page != MenuPage.Main) PlayClick();
            page = MenuPage.Main;
        }

        private void OnGUI()
        {
            EnsureTextures();
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            if (page == MenuPage.Main) DrawMain(width, height);
            else DrawOverlay(width, height);
            GUI.matrix = previousMatrix;
        }

        private void DrawMain(float width, float height)
        {
            Rect panel = new Rect(74f, (height - 640f) * .5f, 478f, 640f);
            DrawPanel(panel, new Color(.025f, .10f, .06f, .90f));
            GUI.Label(new Rect(panel.x + 36f, panel.y + 50f, 400f, 65f), "BEFORE", TitleStyle(52, new Color(.94f, .82f, .49f)));
            GUI.Label(new Rect(panel.x + 34f, panel.y + 112f, 410f, 85f), "THE AXES", TitleStyle(72, new Color(.98f, .92f, .74f)));
            GUI.color = new Color(.88f, .67f, .28f, .9f);
            GUI.DrawTexture(new Rect(panel.x + 36f, panel.y + 211f, 380f, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 37f, panel.y + 226f, 390f, 30f), "A QUIET FOREST STEALTH TALE", LabelStyle(15, FontStyle.Bold, new Color(.69f, .82f, .67f), TextAnchor.MiddleLeft));
            if (DrawButton(new Rect(panel.x + 36f, panel.y + 315f, 386f, 58f), "START")) StartGame();
            if (DrawButton(new Rect(panel.x + 36f, panel.y + 393f, 386f, 58f), "HELP")) ShowHelp();
            if (DrawButton(new Rect(panel.x + 36f, panel.y + 471f, 386f, 58f), "CREDITS")) ShowCredits();
            GUI.Label(new Rect(panel.x + 37f, panel.y + 558f, 385f, 28f), "THE ROOTS REMEMBER", LabelStyle(13, FontStyle.Italic, new Color(.63f, .72f, .58f), TextAnchor.MiddleLeft));
        }

        private void DrawOverlay(float width, float height)
        {
            GUI.color = new Color(.008f, .025f, .017f, .79f);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            bool help = page == MenuPage.Help;
            Rect card = new Rect((width - 720f) * .5f, (height - (help ? 570f : 470f)) * .5f, 720f, help ? 570f : 470f);
            DrawPanel(card, new Color(.045f, .14f, .09f, .985f));
            GUI.Label(new Rect(card.x + 55f, card.y + 52f, 610f, 60f), help ? "HOW TO PLAY" : "CREDITS", TitleStyle(42, new Color(.95f, .83f, .51f), TextAnchor.MiddleCenter));
            if (help)
            {
                GUI.Label(new Rect(card.x + 65f, card.y + 140f, 590f, 300f), "WASD — Move\n\nHold Shift — Run\n\nPress Space — Become a tree\n\nIf woodcutters detect you, they will hunt you.\nBreak their sight, or stand still as a tree.", LabelStyle(22, FontStyle.Normal, new Color(.88f, .94f, .83f), TextAnchor.UpperLeft));
                if (DrawButton(new Rect(card.x + 205f, card.y + 475f, 310f, 58f), "BACK")) ShowMain();
            }
            else
            {
                GUI.Label(new Rect(card.x + 65f, card.y + 154f, 590f, 48f), "Made by Thegm26", LabelStyle(29, FontStyle.Bold, new Color(.90f, .95f, .83f), TextAnchor.MiddleCenter));
                GUI.Label(new Rect(card.x + 65f, card.y + 230f, 590f, 66f), "Supercyan Free Forest Sample\nCreative Characters FREE", LabelStyle(17, FontStyle.Normal, new Color(.66f, .78f, .64f), TextAnchor.MiddleCenter));
                if (DrawButton(new Rect(card.x + 205f, card.y + 375f, 310f, 58f), "BACK")) ShowMain();
            }
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, panelTexture);
            GUI.color = new Color(.88f, .67f, .28f, .82f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static bool DrawButton(Rect rect, string text)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { background = buttonTexture, textColor = new Color(.96f, .88f, .62f) },
                hover = { background = hoverTexture, textColor = Color.white },
                active = { background = pressedTexture, textColor = new Color(.97f, .80f, .38f) }
            };
            return GUI.Button(rect, text, style);
        }

        private static GUIStyle TitleStyle(int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft) => LabelStyle(size, FontStyle.Bold, color, alignment);

        private static GUIStyle LabelStyle(int size, FontStyle style, Color color, TextAnchor alignment)
        {
            return new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = style, normal = { textColor = color }, alignment = alignment, wordWrap = true };
        }

        private void PlayClick()
        {
            if (uiAudioSource != null && clickSound != null) uiAudioSource.PlayOneShot(clickSound, .32f);
        }

        private static void EnsureTextures()
        {
            if (panelTexture != null) return;
            panelTexture = MakeTexture(new Color(.035f, .12f, .075f, 1f));
            buttonTexture = MakeTexture(new Color(.13f, .31f, .18f, 1f));
            hoverTexture = MakeTexture(new Color(.25f, .52f, .27f, 1f));
            pressedTexture = MakeTexture(new Color(.07f, .18f, .10f, 1f));
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            return texture;
        }
    }
}
