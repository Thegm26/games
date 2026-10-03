using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using WhoEnters.Integration;

namespace WhoEnters.Tests.EditMode.Integration
{
    public sealed class GatehouseEnvironmentRigOverscanTests
    {
        [TestCase(360f, 640f)]
        [TestCase(720f, 1280f)]
        public void ExactNineBySixteenLayersCoverTheViewportAfterAspectLayoutAtEveryMotionEndpoint(float width, float height)
        {
            var viewport = new GameObject("Environment Viewport", typeof(RectTransform));
            try
            {
                var viewportRect = viewport.GetComponent<RectTransform>();
                viewportRect.sizeDelta = new Vector2(width, height);
                var rig = GatehouseEnvironmentRig.Create(viewport.transform, VisualAssetCatalog.LoadRequired());
                ForcePostAspectLayout(viewportRect, rig);

                var endpoints = new[]
                {
                    -GatehouseEnvironmentRig.VistaParallaxAmplitude, GatehouseEnvironmentRig.VistaParallaxAmplitude,
                    -GatehouseEnvironmentRig.FogDriftAmplitude, GatehouseEnvironmentRig.FogDriftAmplitude,
                    -GatehouseEnvironmentRig.ForegroundParallaxAmplitude, GatehouseEnvironmentRig.ForegroundParallaxAmplitude,
                };
                foreach (var layer in rig.transform)
                {
                    var wrapper = (RectTransform)((Transform)layer);
                    Assert.That(wrapper.offsetMin, Is.EqualTo(Vector2.one * -GatehouseEnvironmentRig.MotionOverscanLogicalPixels));
                    Assert.That(wrapper.offsetMax, Is.EqualTo(Vector2.one * GatehouseEnvironmentRig.MotionOverscanLogicalPixels));
                    var raster = wrapper.GetComponentInChildren<Image>().rectTransform;
                    var fitter = raster.GetComponent<AspectRatioFitter>();
                    Assert.That(fitter.aspectRatio, Is.EqualTo(9f / 16f));

                    foreach (var endpoint in endpoints)
                    {
                        wrapper.anchoredPosition = new Vector2(endpoint, 0f);
                        AssertCoversViewport(raster, viewportRect, wrapper.name + " at x=" + endpoint);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(viewport);
            }
        }

        [Test]
        public void MotionAndFitterHaveSeparateStableGeometryOwners()
        {
            var viewport = new GameObject("Environment Viewport", typeof(RectTransform));
            try
            {
                var viewportRect = viewport.GetComponent<RectTransform>();
                viewportRect.sizeDelta = new Vector2(720f, 1280f);
                var rig = GatehouseEnvironmentRig.Create(viewport.transform, VisualAssetCatalog.LoadRequired());
                ForcePostAspectLayout(viewportRect, rig);

                Assert.That(rig.transform.childCount, Is.EqualTo(5));
                foreach (Transform layer in rig.transform)
                {
                    Assert.That(layer.GetComponent<AspectRatioFitter>(), Is.Null, layer.name);
                    Assert.That(layer.GetComponentInChildren<AspectRatioFitter>(), Is.Not.Null, layer.name);
                    Assert.That(layer.GetComponentInChildren<Image>().preserveAspect, Is.True, layer.name);
                }
            }
            finally
            {
                Object.DestroyImmediate(viewport);
            }
        }

        private static void ForcePostAspectLayout(RectTransform viewport, GatehouseEnvironmentRig rig)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
            foreach (var fitter in rig.GetComponentsInChildren<AspectRatioFitter>())
            {
                fitter.SetLayoutHorizontal();
                fitter.SetLayoutVertical();
            }
            Canvas.ForceUpdateCanvases();
        }

        private static void AssertCoversViewport(RectTransform raster, RectTransform viewport, string message)
        {
            var viewportCorners = new Vector3[4];
            var rasterCorners = new Vector3[4];
            viewport.GetWorldCorners(viewportCorners);
            raster.GetWorldCorners(rasterCorners);
            var viewportMin = viewportCorners[0];
            var viewportMax = viewportCorners[2];
            var rasterMin = rasterCorners[0];
            var rasterMax = rasterCorners[2];
            const float epsilon = .01f;
            Assert.That(rasterMin.x, Is.LessThanOrEqualTo(viewportMin.x + epsilon), message);
            Assert.That(rasterMax.x, Is.GreaterThanOrEqualTo(viewportMax.x - epsilon), message);
            Assert.That(rasterMin.y, Is.LessThanOrEqualTo(viewportMin.y + epsilon), message);
            Assert.That(rasterMax.y, Is.GreaterThanOrEqualTo(viewportMax.y - epsilon), message);
        }
    }
}
