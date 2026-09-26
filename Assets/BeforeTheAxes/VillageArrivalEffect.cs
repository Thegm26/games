using UnityEngine;

namespace BeforeTheAxes
{
    /// <summary>
    /// A small, one-shot low-poly leaf celebration for reaching the village.
    /// The leaf mesh and material are supplied by the installed Supercyan forest
    /// pack, so the effect remains visually native to the forest and WebGL-safe.
    /// </summary>
    public sealed class VillageArrivalEffect : MonoBehaviour
    {
        private const float EffectDuration = 1.45f;

        [Header("Installed forest pack visuals")]
        [SerializeField] private Mesh leafMesh;
        [SerializeField] private Material leafMaterial;

        private ParticleSystem leaves;
        private Light warmLight;
        private bool played;
        private float startedAt;

        public bool HasPlayed => played;

        public void Play(Vector3 worldPosition)
        {
            if (played) return;
            played = true;
            startedAt = Time.unscaledTime;
            transform.position = worldPosition + Vector3.up * .85f;
            EnsureEffect();
            leaves.Clear(true);
            leaves.Play(true);
            warmLight.enabled = true;
        }

        private void Update()
        {
            if (!played) return;
            float progress = Mathf.Clamp01((Time.unscaledTime - startedAt) / EffectDuration);
            if (warmLight != null)
            {
                warmLight.intensity = Mathf.Lerp(2.1f, 0f, progress);
                warmLight.range = Mathf.Lerp(5.2f, 2.2f, progress);
                if (progress >= 1f) warmLight.enabled = false;
            }
        }

        private void OnGUI()
        {
            if (!played) return;
            float age = Time.unscaledTime - startedAt;
            if (age < 0f || age > .85f) return;

            // A brief, gentle warm wash reads as celebration without obscuring play.
            float alpha = Mathf.Sin(Mathf.Clamp01(age / .85f) * Mathf.PI) * .105f;
            Color previous = GUI.color;
            GUI.color = new Color(1f, .78f, .31f, alpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureEffect()
        {
            if (leaves != null) return;

            GameObject particlesObject = new GameObject("Village Arrival Leaves");
            particlesObject.transform.SetParent(transform, false);
            leaves = particlesObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = leaves.main;
            main.playOnAwake = false;
            main.prewarm = false;
            main.loop = false;
            main.duration = .35f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.05f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(.18f, .34f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = .28f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;
            main.maxParticles = 32;

            ParticleSystem.EmissionModule emission = leaves.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 26) });

            ParticleSystem.ShapeModule shape = leaves.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .5f;

            ParticleSystem.ColorOverLifetimeModule colour = leaves.colorOverLifetime;
            colour.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(.97f, .73f, .22f), 0f),
                    new GradientColorKey(new Color(.45f, .79f, .30f), .45f),
                    new GradientColorKey(new Color(.94f, .80f, .40f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(.94f, .12f),
                    new GradientAlphaKey(.85f, .7f),
                    new GradientAlphaKey(0f, 1f)
                });
            colour.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystem.RotationOverLifetimeModule rotation = leaves.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-1.6f, 1.6f);
            rotation.y = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
            rotation.z = new ParticleSystem.MinMaxCurve(-1.6f, 1.6f);

            ParticleSystemRenderer renderer = leaves.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            // These are serialized references to the downloaded Supercyan Mobile
            // Tree Leaf mesh and material. Do not replace them with generated art.
            if (leafMesh == null || leafMaterial == null)
            {
                Debug.LogError("VillageArrivalEffect needs its Supercyan leaf mesh and material assigned.", this);
                emission.enabled = false;
            }
            else
            {
                renderer.mesh = leafMesh;
                renderer.sharedMaterial = leafMaterial;
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 8;

            GameObject lightObject = new GameObject("Village Arrival Glow");
            lightObject.transform.SetParent(transform, false);
            warmLight = lightObject.AddComponent<Light>();
            warmLight.type = LightType.Point;
            warmLight.color = new Color(1f, .76f, .32f);
            warmLight.range = 5.2f;
            warmLight.intensity = 0f;
            warmLight.shadows = LightShadows.None;
            warmLight.enabled = false;
        }

    }
}
