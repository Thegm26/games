using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeforeTheAxes
{
    /// <summary>Completes the level when the guardian returns to the village after securing the mushroom.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class VillageExit : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const float WinScreenSeconds = 2.5f;

        [SerializeField] private RootMushroomPickup mushroom;
        [SerializeField] private VillageArrivalEffect arrivalEffect;

        private bool objectiveCollected;
        private bool won;
        private bool returnRequested;
        private float wonAt;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private Transform villageSign;
        private Renderer[] signRenderers;
        private Collider[] signColliders;
        private Vector3 signRestingLocalPosition;
        private Vector3 signRestingLocalScale;
        private Quaternion signRestingLocalRotation;
        private Light signGlow;
        private bool signShown;
        private float signRevealStartedAt;
        private Coroutine returnRoutine;

        public bool IsObjectiveCollected => objectiveCollected;
        public bool HasWon => won;
        public bool CanExit => objectiveCollected && !won;

        private void Awake()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            if (arrivalEffect == null)
                arrivalEffect = GetComponent<VillageArrivalEffect>();
            if (arrivalEffect == null)
                arrivalEffect = gameObject.AddComponent<VillageArrivalEffect>();
            FindMushroom();
            SetupVillageSign();
            SetSignVisible(objectiveCollected, true);
        }

        private void OnEnable()
        {
            RootMushroomPickup.Collected += OnMushroomCollected;
            FindMushroom();
        }

        private void OnDisable()
        {
            RootMushroomPickup.Collected -= OnMushroomCollected;
            if (returnRoutine != null) StopCoroutine(returnRoutine);
        }

        private void FindMushroom()
        {
            if (mushroom == null)
                mushroom = FindAnyObjectByType<RootMushroomPickup>(FindObjectsInactive.Include);
            objectiveCollected = mushroom != null && mushroom.IsCollected;
        }

        private void OnMushroomCollected(RootMushroomPickup collectedMushroom)
        {
            mushroom = collectedMushroom;
            objectiveCollected = true;
            SetSignVisible(true, false);
        }

        private void Update()
        {
            AnimateVillageSign();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryComplete(other.GetComponentInParent<ForestGuardianController>());
        }

        /// <summary>Returns false until the mushroom has been collected; also provides a narrow runtime verification seam.</summary>
        public bool TryComplete(ForestGuardianController guardian)
        {
            if (!CanExit || guardian == null) return false;

            won = true;
            wonAt = Time.unscaledTime;
            // The win effect should never inherit a paused time scale from another state.
            Time.timeScale = 1f;
            arrivalEffect?.Play(guardian.transform.position);
            guardian.enabled = false;
            CharacterController characterController = guardian.GetComponent<CharacterController>();
            if (characterController != null) characterController.enabled = false;
            FreezeWoodcutters();
            returnRoutine = StartCoroutine(ReturnToMainMenuAfterWin());
            return true;
        }

        private IEnumerator ReturnToMainMenuAfterWin()
        {
            // Unscaled time keeps this reliable if another presentation system has altered
            // timeScale. The arrival effect itself also uses unscaled timing.
            yield return new WaitForSecondsRealtime(WinScreenSeconds);
            if (returnRequested) yield break;

            returnRequested = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void FreezeWoodcutters()
        {
            WoodcutterAI[] woodcutters = FindObjectsByType<WoodcutterAI>();
            foreach (WoodcutterAI woodcutter in woodcutters)
            {
                if (woodcutter != null) woodcutter.enabled = false;
            }
        }

        private void SetupVillageSign()
        {
            if (villageSign != null) return;

            // The installed road-sign prefab is deliberately a child of the trigger root.
            // Its colliders must never compete with the root trigger or the player controller.
            foreach (Transform child in transform)
            {
                if (child.name.IndexOf("Village Road Sign", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    child.name.IndexOf("Road Sign", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    villageSign = child;
                    break;
                }
            }

            if (villageSign == null) return;

            signRestingLocalPosition = villageSign.localPosition;
            signRestingLocalScale = villageSign.localScale;
            signRestingLocalRotation = villageSign.localRotation;
            signRenderers = villageSign.GetComponentsInChildren<Renderer>(true);
            signColliders = villageSign.GetComponentsInChildren<Collider>(true);
            foreach (Collider signCollider in signColliders)
            {
                if (signCollider != null) signCollider.enabled = false;
            }

            GameObject glowObject = new GameObject("Village Sign Glow");
            glowObject.transform.SetParent(villageSign, false);
            glowObject.transform.localPosition = new Vector3(0f, .85f, 0f);
            signGlow = glowObject.AddComponent<Light>();
            signGlow.type = LightType.Point;
            signGlow.color = new Color(.72f, 1f, .44f);
            signGlow.range = 2.8f;
            signGlow.intensity = 0f;
            signGlow.shadows = LightShadows.None;
            signGlow.enabled = false;
        }

        private void SetSignVisible(bool visible, bool immediate)
        {
            SetupVillageSign();
            if (villageSign == null) return;

            signShown = visible;
            if (!visible)
            {
                villageSign.localScale = Vector3.zero;
                if (signRenderers != null)
                {
                    foreach (Renderer signRenderer in signRenderers)
                    {
                        if (signRenderer != null) signRenderer.enabled = false;
                    }
                }
                if (signGlow != null) signGlow.enabled = false;
                return;
            }

            if (signRenderers != null)
            {
                foreach (Renderer signRenderer in signRenderers)
                {
                    if (signRenderer != null) signRenderer.enabled = true;
                }
            }
            signRevealStartedAt = immediate ? -10f : Time.unscaledTime;
            villageSign.localScale = immediate ? signRestingLocalScale : Vector3.zero;
            villageSign.localPosition = signRestingLocalPosition;
            villageSign.localRotation = signRestingLocalRotation;
            if (signGlow != null) signGlow.enabled = true;
        }

        private void AnimateVillageSign()
        {
            if (!signShown || villageSign == null) return;

            float elapsed = Mathf.Max(0f, Time.unscaledTime - signRevealStartedAt);
            const float revealSeconds = .52f;
            float reveal = Mathf.Clamp01(elapsed / revealSeconds);
            // A restrained back-ease gives the wooden sign a warm, physical pop rather than a
            // mechanical scale-up; it settles into a tiny forest-like sway afterwards.
            float eased = 1f + 2.7f * Mathf.Pow(reveal - 1f, 3f) + 1.7f * Mathf.Pow(reveal - 1f, 2f);
            float idle = Mathf.Sin(Time.unscaledTime * 1.55f + transform.position.x) * .028f;
            villageSign.localScale = signRestingLocalScale * Mathf.Max(0f, eased);
            villageSign.localPosition = signRestingLocalPosition + Vector3.up * (idle + (1f - reveal) * -.12f);
            villageSign.localRotation = signRestingLocalRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.18f) * 1.6f);

            if (signGlow != null)
            {
                signGlow.enabled = true;
                signGlow.intensity = .65f + Mathf.Sin(Time.unscaledTime * 2.1f) * .18f;
            }
        }

        private void OnGUI()
        {
            if (!won) return;
            EnsureStyles();

            float progress = Mathf.Clamp01((Time.unscaledTime - wonAt) / .32f);
            float scale = Mathf.Lerp(.92f, 1f, 1f - Mathf.Pow(1f - progress, 3f));
            float width = Mathf.Min(620f, Screen.width - 36f);
            float height = 190f;
            Rect panel = new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f, width, height);

            Matrix4x4 previousMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), panel.center);
            GUI.color = new Color(.025f, .07f, .045f, Mathf.Lerp(0f, .96f, progress));
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(.36f, .86f, .48f, progress);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 5f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 20f, panel.y + 42f, panel.width - 40f, 62f), "YOU MADE IT HOME", titleStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 112f, panel.width - 40f, 34f), "The root mushroom is safely back in the village.", subtitleStyle);
            GUI.matrix = previousMatrix;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = new Color(.78f, .94f, .8f, 1f) }
            };
        }
    }
}
