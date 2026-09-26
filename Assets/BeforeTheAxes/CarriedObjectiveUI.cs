using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>Small top-right confirmation, intentionally hidden until the objective is carried.</summary>
    public sealed class CarriedObjectiveUI : MonoBehaviour
    {
        private RootMushroomPickup pickup;
        private bool visible;
        private GUIStyle labelStyle;
        private Texture2D fillTexture;

        /// <summary>Read-only runtime state used by the pickup verifier and later objective logic.</summary>
        public bool IsCarriedVisible => visible;

        private void OnEnable()
        {
            RootMushroomPickup.Collected += OnPickupCollected;
            pickup = FindFirstObjectByType<RootMushroomPickup>(FindObjectsInactive.Include);
            visible = pickup != null && pickup.IsCollected;
        }

        private void OnDisable()
        {
            RootMushroomPickup.Collected -= OnPickupCollected;
        }

        private void OnPickupCollected(RootMushroomPickup collectedPickup)
        {
            pickup = collectedPickup;
            visible = true;
        }

        private void OnGUI()
        {
            if (!visible) return;
            EnsureStyle();
            Rect panel = new Rect(Screen.width - 202f, 22f, 180f, 52f);
            GUI.color = new Color(.035f, .07f, .055f, .94f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(.22f, .55f, .95f, 1f);
            GUI.DrawTexture(new Rect(panel.x + 10f, panel.y + 10f, 32f, 32f), fillTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 52f, panel.y + 4f, 116f, 44f), "ROOT MUSHROOM\nCARRIED", labelStyle);
        }

        private void EnsureStyle()
        {
            if (labelStyle != null) return;
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            fillTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            fillTexture.SetPixel(0, 0, Color.white);
            fillTexture.Apply();
        }
    }
}
