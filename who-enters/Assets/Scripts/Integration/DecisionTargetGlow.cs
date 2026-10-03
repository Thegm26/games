using UnityEngine;
using UnityEngine.UI;

namespace WhoEnters.Integration
{
    /// <summary>
    /// A non-interactive, layered halo for the fixed Admit/Deny targets.  The opaque decision
    /// button stays above this graphic, so the feedback never competes with card copy or hides
    /// the directional label.  The outer rings remain visible even when the button surface is
    /// fully opaque.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DecisionTargetGlow : Graphic
    {
        private Color glowColor;

        public float Intensity { get; private set; }

        public void Configure(Color value)
        {
            glowColor = value;
            raycastTarget = false;
            SetIntensity(0f);
        }

        public void SetIntensity(float value)
        {
            Intensity = Mathf.Clamp01(value);
            // A cue must be discoverable before the commit threshold, not emerge only at max
            // travel.  The mesh supplies the soft falloff; this alpha controls its strength.
            color = new Color(glowColor.r, glowColor.g, glowColor.b,
                Intensity <= .001f ? 0f : .34f + Intensity * .66f);
            enabled = Intensity > .001f;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f || color.a <= .001f) return;

            // Four nested frames produce a deliberately restrained falloff rather than the
            // rejected debug-strip look. The decision button covers the centre of this larger
            // halo, leaving a clear coloured perimeter around its fixed labelled target.
            AddFrame(vertices, rect, 0f, 5f, .16f);
            AddFrame(vertices, rect, 5f, 5f, .26f);
            AddFrame(vertices, rect, 10f, 5f, .46f);
            AddFrame(vertices, rect, 15f, 5f, .72f);
        }

        private void AddFrame(VertexHelper vertices, Rect outer, float inset, float thickness, float alpha)
        {
            var frame = new Rect(outer.xMin + inset, outer.yMin + inset,
                outer.width - inset * 2f, outer.height - inset * 2f);
            if (frame.width <= thickness * 2f || frame.height <= thickness * 2f) return;
            var tint = new Color(color.r, color.g, color.b, color.a * alpha);
            AddQuad(vertices, new Rect(frame.xMin, frame.yMax - thickness, frame.width, thickness), tint);
            AddQuad(vertices, new Rect(frame.xMin, frame.yMin, frame.width, thickness), tint);
            AddQuad(vertices, new Rect(frame.xMin, frame.yMin + thickness, thickness, frame.height - thickness * 2f), tint);
            AddQuad(vertices, new Rect(frame.xMax - thickness, frame.yMin + thickness, thickness, frame.height - thickness * 2f), tint);
        }

        private static void AddQuad(VertexHelper vertices, Rect rect, Color tint)
        {
            var start = vertices.currentVertCount;
            var vertex = UIVertex.simpleVert;
            vertex.color = tint;
            vertex.position = new Vector2(rect.xMin, rect.yMin); vertices.AddVert(vertex);
            vertex.position = new Vector2(rect.xMin, rect.yMax); vertices.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMax); vertices.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMin); vertices.AddVert(vertex);
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start, start + 2, start + 3);
        }
    }
}
