using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeforeTheAxes
{
    /// <summary>
    /// Reveals the village marker after the mushroom is collected, then plays a short
    /// unscaled in-world return-home cinematic when the guardian reaches the exit.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class VillageExit : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const float FirstCaptionAt = .58f;
        private const float SecondCaptionAt = 2.32f;
        private const float FadeAt = 4.45f;
        private const float LoadAt = 5.32f;

        [SerializeField] private RootMushroomPickup mushroom;
        [SerializeField] private VillageArrivalEffect arrivalEffect;
        [Header("Installed Supercyan Mobile village marker")]
        [SerializeField] private GameObject villageStumpPrefab;
        [SerializeField] private GameObject villageLeafPrefab;
        [SerializeField] private GameObject villageRedMushroomPrefab;
        [SerializeField] private GameObject villageStonePrefab;

        private bool objectiveCollected;
        private bool won;
        private bool returnRequested;
        private float wonAt;
        private Transform villageSign;
        private Renderer[] signRenderers;
        private Collider[] signColliders;
        private Vector3 signRestingLocalPosition;
        private Vector3 signRestingLocalScale;
        private Quaternion signRestingLocalRotation;
        private Light signGlow;
        private bool signShown;
        private float signRevealStartedAt;
        private Transform villageHome;
        private Light villageHomeGlow;
        private bool homeShown;
        private float homeRevealStartedAt;
        private Camera cinematicCamera;
        private ThirdPersonForestCamera thirdPersonCamera;
        private readonly RaycastHit[] cinematicOcclusionHits = new RaycastHit[32];
        private readonly HashSet<Renderer> hiddenCinematicTreeRenderers = new HashSet<Renderer>();
        private readonly HashSet<Renderer> requiredHiddenCinematicRenderers = new HashSet<Renderer>();
        private Vector3 cinematicPosition;
        private Vector3 cinematicFocus;
        private Quaternion cinematicRotation;
        private float cinematicFieldOfView;
        private GUIStyle captionStyle;
        private Coroutine returnRoutine;

        public bool IsObjectiveCollected => objectiveCollected;
        public bool HasWon => won;
        public bool CanExit => objectiveCollected && !won;

        private void Awake()
        {
            BoxCollider trigger = GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            if (arrivalEffect == null) arrivalEffect = GetComponent<VillageArrivalEffect>();
            if (arrivalEffect == null) arrivalEffect = gameObject.AddComponent<VillageArrivalEffect>();
            FindMushroom();
            SetupVillageSign();
            SetSignVisible(objectiveCollected, true);
            SetVillageHomeVisible(objectiveCollected, true);
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
            RestoreCinematicTreeRenderers();
        }

        private void FindMushroom()
        {
            if (mushroom == null) mushroom = FindAnyObjectByType<RootMushroomPickup>(FindObjectsInactive.Include);
            objectiveCollected = mushroom != null && mushroom.IsCollected;
        }

        private void OnMushroomCollected(RootMushroomPickup collectedMushroom)
        {
            mushroom = collectedMushroom;
            objectiveCollected = true;
            SetSignVisible(true, false);
            SetVillageHomeVisible(true, false);
        }

        private void Update()
        {
            AnimateVillageSign();
            AnimateVillageHome();
            UpdateCinematicCamera();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryComplete(other.GetComponentInParent<ForestGuardianController>());
        }

        /// <summary>Returns false until the mushroom is collected; this is also used by runtime probes.</summary>
        public bool TryComplete(ForestGuardianController guardian)
        {
            if (!CanExit || guardian == null) return false;

            won = true;
            wonAt = Time.unscaledTime;
            // Do not globally pause: the leaf system and the cinematic both intentionally use
            // real time, while the guardian and all cutters are explicitly frozen below.
            Time.timeScale = 1f;
            guardian.enabled = false;
            CharacterController controller = guardian.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            FreezeWoodcutters();
            BeginCinematicCamera(guardian.transform);
            Vector3 celebrationPoint = villageHome == null
                ? guardian.transform.position
                : Vector3.Lerp(guardian.transform.position, villageHome.position, .45f);
            arrivalEffect?.Play(celebrationPoint);
            returnRoutine = StartCoroutine(ReturnToMainMenuAfterCinematic());
            return true;
        }

        private IEnumerator ReturnToMainMenuAfterCinematic()
        {
            // WaitForSecondsRealtime makes the ending resilient to any other presentation
            // system changing timeScale, including the caught flow.
            yield return new WaitForSecondsRealtime(LoadAt);
            if (returnRequested) yield break;
            returnRequested = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void FreezeWoodcutters()
        {
            foreach (WoodcutterAI woodcutter in FindObjectsByType<WoodcutterAI>())
            {
                if (woodcutter != null) woodcutter.enabled = false;
            }
        }

        private void BeginCinematicCamera(Transform guardian)
        {
            cinematicCamera = Camera.main;
            if (cinematicCamera == null) return;

            thirdPersonCamera = cinematicCamera.GetComponent<ThirdPersonForestCamera>();
            if (thirdPersonCamera != null) thirdPersonCamera.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Vector3 homePosition = villageHome == null ? transform.position : villageHome.position;
            cinematicFocus = Vector3.Lerp(guardian.position, homePosition, .5f) + Vector3.up * .92f;
            // This overlook is intentionally selected for the open side of the return clearing;
            // dynamically placing the camera behind the guardian can put it inside the dense firs.
            cinematicPosition = cinematicFocus + new Vector3(-4.8f, 3.15f, -4.8f);
            cinematicRotation = Quaternion.LookRotation(cinematicFocus - cinematicPosition, Vector3.up);
            cinematicFieldOfView = 48f;
        }

        private void UpdateCinematicCamera()
        {
            if (!won || cinematicCamera == null) return;
            float blend = 1f - Mathf.Exp(-6.3f * Time.unscaledDeltaTime);
            cinematicCamera.transform.position = Vector3.Lerp(cinematicCamera.transform.position, cinematicPosition, blend);
            cinematicCamera.transform.rotation = Quaternion.Slerp(cinematicCamera.transform.rotation, cinematicRotation, blend);
            cinematicCamera.fieldOfView = Mathf.Lerp(cinematicCamera.fieldOfView, cinematicFieldOfView, blend);
            UpdateCinematicTreeOcclusion();
        }

        private void UpdateCinematicTreeOcclusion()
        {
            requiredHiddenCinematicRenderers.Clear();
            Vector3 direction = cinematicFocus - cinematicCamera.transform.position;
            float distance = direction.magnitude;
            if (distance > .01f)
            {
                int count = Physics.SphereCastNonAlloc(cinematicCamera.transform.position, .24f, direction / distance,
                    cinematicOcclusionHits, distance, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    Collider collider = cinematicOcclusionHits[i].collider;
                    if (collider == null || !TryGetTreeRoot(collider.transform, out Transform treeRoot)) continue;
                    foreach (Renderer renderer in treeRoot.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer != null) requiredHiddenCinematicRenderers.Add(renderer);
                    }
                }
            }

            foreach (Renderer renderer in hiddenCinematicTreeRenderers)
            {
                if (renderer != null && !requiredHiddenCinematicRenderers.Contains(renderer)) renderer.enabled = true;
            }
            hiddenCinematicTreeRenderers.RemoveWhere(renderer => renderer == null || !requiredHiddenCinematicRenderers.Contains(renderer));
            foreach (Renderer renderer in requiredHiddenCinematicRenderers)
            {
                if (renderer == null) continue;
                renderer.enabled = false;
                hiddenCinematicTreeRenderers.Add(renderer);
            }
        }

        private static bool TryGetTreeRoot(Transform candidate, out Transform treeRoot)
        {
            treeRoot = null;
            for (Transform current = candidate; current != null; current = current.parent)
            {
                if (current.name.IndexOf("tree", StringComparison.OrdinalIgnoreCase) >= 0) treeRoot = current;
            }
            return treeRoot != null;
        }

        private void RestoreCinematicTreeRenderers()
        {
            foreach (Renderer renderer in hiddenCinematicTreeRenderers)
            {
                if (renderer != null) renderer.enabled = true;
            }
            hiddenCinematicTreeRenderers.Clear();
            requiredHiddenCinematicRenderers.Clear();
        }

        private void SetupVillageSign()
        {
            if (villageSign != null) return;
            foreach (Transform child in transform)
            {
                if (child.name.IndexOf("Village Road Sign", StringComparison.OrdinalIgnoreCase) < 0 &&
                    child.name.IndexOf("Road Sign", StringComparison.OrdinalIgnoreCase) < 0) continue;
                villageSign = child;
                break;
            }
            if (villageSign == null) return;

            signRestingLocalPosition = villageSign.localPosition;
            signRestingLocalScale = villageSign.localScale;
            signRestingLocalRotation = villageSign.localRotation;
            signRenderers = villageSign.GetComponentsInChildren<Renderer>(true);
            signColliders = villageSign.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in signColliders)
            {
                if (collider != null) collider.enabled = false;
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
                foreach (Renderer renderer in signRenderers)
                {
                    if (renderer != null) renderer.enabled = false;
                }
                if (signGlow != null) signGlow.enabled = false;
                return;
            }

            foreach (Renderer renderer in signRenderers)
            {
                if (renderer != null) renderer.enabled = true;
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
            float reveal = Mathf.Clamp01((Time.unscaledTime - signRevealStartedAt) / .52f);
            float eased = 1f + 2.7f * Mathf.Pow(reveal - 1f, 3f) + 1.7f * Mathf.Pow(reveal - 1f, 2f);
            float idle = Mathf.Sin(Time.unscaledTime * 1.55f + transform.position.x) * .028f;
            villageSign.localScale = signRestingLocalScale * Mathf.Max(0f, eased);
            villageSign.localPosition = signRestingLocalPosition + Vector3.up * (idle - (1f - reveal) * .12f);
            villageSign.localRotation = signRestingLocalRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.18f) * 1.6f);
            if (signGlow != null)
            {
                signGlow.enabled = true;
                signGlow.intensity = .65f + Mathf.Sin(Time.unscaledTime * 2.1f) * .18f;
            }
        }

        private void SetVillageHomeVisible(bool visible, bool immediate)
        {
            if (!visible)
            {
                if (villageHome != null) villageHome.gameObject.SetActive(false);
                homeShown = false;
                return;
            }

            EnsureVillageHome();
            if (villageHome == null) return;
            homeShown = true;
            villageHome.gameObject.SetActive(true);
            homeRevealStartedAt = immediate ? -10f : Time.unscaledTime;
            villageHome.localScale = immediate ? Vector3.one : Vector3.zero;
        }

        private void EnsureVillageHome()
        {
            if (villageHome != null) return;
            if (villageStumpPrefab == null || villageLeafPrefab == null || villageRedMushroomPrefab == null || villageStonePrefab == null)
            {
                Debug.LogWarning("VillageExit needs its Supercyan Mobile marker prefab references assigned.", this);
                return;
            }

            GameObject root = new GameObject("Village Home Marker");
            villageHome = root.transform;
            villageHome.SetParent(transform, false);
            // The existing non-blocking road sign remains the signpost. Keep this small and
            // architectural: a stump cottage with a single mushroom roof reads as a destination,
            // rather than another clump of forest foliage.
            villageHome.localPosition = new Vector3(-1.25f, -.5f, .35f);
            villageHome.localRotation = Quaternion.Euler(0f, -26f, 0f);
            villageHome.localScale = Vector3.one;

            // The leaf prefab intentionally stays assigned for backwards-compatible scene data,
            // but is not used here: its separate foliage meshes made the marker read as scattered
            // canopy rather than a small home.
            CreateVillagePiece(villageStumpPrefab, "Cottage Stump Walls", Vector3.zero, new Vector3(1.7f, 1.45f, 1.7f), 0f);
            // Sink the mushroom stem into the stump so the cap meets the walls as a roof,
            // with no visible floating gap from any camera angle.
            CreateVillagePiece(villageRedMushroomPrefab, "Cottage Mushroom Roof", new Vector3(0f, .24f, .02f), Vector3.one * 4.8f, 0f);
            CreateVillagePiece(villageRedMushroomPrefab, "Cottage Garden Mushroom A", new Vector3(.82f, .03f, .34f), Vector3.one * 1.2f, 18f);
            CreateVillagePiece(villageRedMushroomPrefab, "Cottage Garden Mushroom B", new Vector3(-.76f, .03f, .28f), Vector3.one * .88f, -30f);
            CreateVillagePiece(villageStonePrefab, "Cottage Doorstep", new Vector3(0f, .02f, -.76f), Vector3.one * .82f, 0f);
            CreateVillagePiece(villageStonePrefab, "Cottage Path Stone A", new Vector3(.32f, .02f, -1.22f), Vector3.one * .56f, 22f);
            CreateVillagePiece(villageStonePrefab, "Cottage Path Stone B", new Vector3(-.26f, .02f, -1.58f), Vector3.one * .48f, -18f);

            GameObject glowObject = new GameObject("Cottage Doorway Glow");
            glowObject.transform.SetParent(villageHome, false);
            glowObject.transform.localPosition = new Vector3(0f, .54f, -.74f);
            villageHomeGlow = glowObject.AddComponent<Light>();
            villageHomeGlow.type = LightType.Point;
            villageHomeGlow.color = new Color(1f, .67f, .25f);
            villageHomeGlow.range = 2.7f;
            villageHomeGlow.intensity = 0f;
            villageHomeGlow.shadows = LightShadows.None;
        }

        private void CreateVillagePiece(GameObject prefab, string objectName, Vector3 localPosition, Vector3 localScale, float yaw)
        {
            GameObject instance = Instantiate(prefab, villageHome);
            instance.name = objectName;
            Transform piece = instance.transform;
            piece.localPosition = localPosition;
            piece.localRotation = Quaternion.Euler(0f, yaw, 0f);
            piece.localScale = localScale;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
        }

        private void AnimateVillageHome()
        {
            if (!homeShown || villageHome == null) return;
            float reveal = Mathf.Clamp01((Time.unscaledTime - homeRevealStartedAt) / .62f);
            float eased = 1f + 2.8f * Mathf.Pow(reveal - 1f, 3f) + 1.8f * Mathf.Pow(reveal - 1f, 2f);
            float bob = Mathf.Sin(Time.unscaledTime * 1.32f) * .035f;
            villageHome.localScale = Vector3.one * Mathf.Max(0f, eased);
            villageHome.localPosition = new Vector3(-1.25f, -.5f + bob - (1f - reveal) * .16f, .35f);
            if (villageHomeGlow != null)
            {
                villageHomeGlow.intensity = .75f + Mathf.Sin(Time.unscaledTime * 2f) * .18f;
                villageHomeGlow.enabled = true;
            }
        }

        private void OnGUI()
        {
            if (!won) return;
            EnsureStyles();
            float elapsed = Time.unscaledTime - wonAt;
            DrawCaption("THE MUSHROOM IS HOME", FirstCaptionAt, elapsed);
            DrawCaption("THE ROOTS WILL GROW AGAIN", SecondCaptionAt, elapsed);

            float fade = Mathf.Clamp01((elapsed - FadeAt) / (LoadAt - FadeAt - .12f));
            if (fade <= 0f) return;
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, fade);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawCaption(string message, float startAt, float elapsed)
        {
            float localAge = elapsed - startAt;
            if (localAge < 0f || localAge > 1.55f) return;
            int visibleCharacters = Mathf.Clamp(Mathf.FloorToInt(localAge * 34f), 0, message.Length);
            float alpha = Mathf.Clamp01(localAge / .18f) * Mathf.Clamp01((1.55f - localAge) / .28f);
            float width = Mathf.Min(980f, Screen.width - 40f);
            Rect panel = new Rect((Screen.width - width) * .5f, Screen.height * .70f, width, 92f);
            Color previous = GUI.color;
            GUI.color = new Color(.015f, .04f, .025f, alpha * .52f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(panel, message.Substring(0, visibleCharacters), captionStyle);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (captionStyle != null) return;
            captionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 38,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.93f, 1f, .86f, 1f) }
            };
        }
    }
}
