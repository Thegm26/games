using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>Completes the level when the guardian returns to the village after securing the mushroom.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class VillageExit : MonoBehaviour
    {
        [SerializeField] private RootMushroomPickup mushroom;
        [SerializeField] private VillageArrivalEffect arrivalEffect;

        private bool objectiveCollected;
        private bool won;
        private float wonAt;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;

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
        }

        private void OnEnable()
        {
            RootMushroomPickup.Collected += OnMushroomCollected;
            FindMushroom();
        }

        private void OnDisable()
        {
            RootMushroomPickup.Collected -= OnMushroomCollected;
        }

        private void FindMushroom()
        {
            if (mushroom == null)
                mushroom = FindFirstObjectByType<RootMushroomPickup>(FindObjectsInactive.Include);
            objectiveCollected = mushroom != null && mushroom.IsCollected;
        }

        private void OnMushroomCollected(RootMushroomPickup collectedMushroom)
        {
            mushroom = collectedMushroom;
            objectiveCollected = true;
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
            arrivalEffect?.Play(guardian.transform.position);
            guardian.enabled = false;
            CharacterController characterController = guardian.GetComponent<CharacterController>();
            if (characterController != null) characterController.enabled = false;
            return true;
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
