using UnityEngine;
using UnityEngine.UI;
using WhoEnters.Core;

namespace WhoEnters.Integration
{
    /// <summary>Data-defined, non-skeletal scene motion. All positions are logical canvas units.</summary>
    public sealed class GatehouseEnvironmentRig : MonoBehaviour
    {
        public const float MotionOverscanLogicalPixels = 24f;
        public const float VistaParallaxAmplitude = 3f;
        public const float FogDriftAmplitude = 8f;
        public const float ForegroundParallaxAmplitude = 12f;
        public const float TorchMinimumAlpha = .95f;
        public const float TorchMaximumAlpha = 1f;

        private RectTransform vista;
        private RectTransform fog;
        private RectTransform foreground;
        private Image torches;
        private float elapsed;
        private bool reducedMotion;

        public static GatehouseEnvironmentRig Create(Transform canvas, VisualAssetCatalog catalog)
        {
            var root = new GameObject("Gatehouse Environment", typeof(RectTransform), typeof(GatehouseEnvironmentRig));
            root.transform.SetParent(canvas, false);
            root.transform.SetAsFirstSibling();
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            var rig = root.GetComponent<GatehouseEnvironmentRig>();
            rig.vista = rig.Layer("01 Castle Vista", catalog.CastleVista, 0);
            rig.Layer("02 Gate Frame", catalog.GateFrame, 10);
            rig.fog = rig.Layer("03 Fog Rain", catalog.FogRain, 20);
            rig.torches = rig.Layer("04 Torches Glow", catalog.TorchesGlow, 30).GetComponentInChildren<Image>();
            rig.foreground = rig.Layer("05 Foreground Silhouettes", catalog.ForegroundSilhouettes, 40);
            DebugTrace.Log("environment.bound", "layers=5;order=0,10,20,30,40;safeRegion=x20-80;y24-84");
            return rig;
        }

        private RectTransform Layer(string name, Sprite sprite, int order)
        {
            // This transform owns the permanent bleed and parallax. The AspectRatioFitter is
            // deliberately below it: fitters rewrite their own sizeDelta, so putting bleed on
            // the fitted transform makes it disappear on a later layout pass.
            var layer = new GameObject(name, typeof(RectTransform));
            layer.transform.SetParent(transform, false);
            var rect = layer.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * -MotionOverscanLogicalPixels;
            rect.offsetMax = Vector2.one * MotionOverscanLogicalPixels;

            var raster = new GameObject("Raster Cover", typeof(RectTransform), typeof(Image));
            raster.transform.SetParent(layer.transform, false);
            var rasterRect = raster.GetComponent<RectTransform>();
            rasterRect.anchorMin = Vector2.zero;
            rasterRect.anchorMax = Vector2.one;
            rasterRect.offsetMin = Vector2.zero;
            rasterRect.offsetMax = Vector2.zero;
            var image = raster.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var fitter = raster.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 9f / 16f;
            layer.name = name + " [order " + order + "]";
            return rect;
        }

        private void Update()
        {
            if (reducedMotion) return;
            elapsed += Time.unscaledDeltaTime;
            if (vista != null) vista.anchoredPosition = new Vector2(Mathf.Sin(elapsed * .08f) * VistaParallaxAmplitude, 0f);
            if (fog != null) fog.anchoredPosition = new Vector2(Mathf.Repeat(elapsed * 4f, FogDriftAmplitude * 2f) - FogDriftAmplitude, 0f);
            if (foreground != null) foreground.anchoredPosition = new Vector2(Mathf.Sin(elapsed * .16f + .8f) * ForegroundParallaxAmplitude, 0f);
            if (torches != null)
            {
                var color = torches.color;
                color.a = Mathf.Lerp(TorchMinimumAlpha, TorchMaximumAlpha, (Mathf.Sin(elapsed * 3.2f) + 1f) * .5f);
                torches.color = color;
            }
        }

        public void SetReducedMotion(bool enabled)
        {
            reducedMotion = enabled;
            elapsed = 0f;
            if (vista != null) vista.anchoredPosition = Vector2.zero;
            if (fog != null) fog.anchoredPosition = Vector2.zero;
            if (foreground != null) foreground.anchoredPosition = Vector2.zero;
            if (torches != null)
            {
                var color = torches.color; color.a = 1f; torches.color = color;
            }
            DebugTrace.Log("environment.reduced_motion", "enabled=" + enabled + ";positions=zero;torchAlpha=1");
        }
    }

}
