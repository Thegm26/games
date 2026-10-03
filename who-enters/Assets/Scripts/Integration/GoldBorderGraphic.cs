using UnityEngine;
using UnityEngine.UI;

namespace WhoEnters.Integration
{
    /// <summary>A raycastable three-pixel outline which never paints over the button fill.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GoldBorderGraphic : Graphic
    {
        [SerializeField] private float thickness = 3f;

        /// <summary>
        /// Applies the visual and interaction contract after this component has been attached.
        /// Keeping the configuration here makes the creation path safe in both edit and Play
        /// mode: callers never need to add a second Graphic to an old Image object.
        /// </summary>
        public void Configure(Color borderColor, float borderThickness = 3f)
        {
            thickness = Mathf.Max(1f, borderThickness);
            color = borderColor;
            raycastTarget = true;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;
            var inset = Mathf.Clamp(thickness, 1f, Mathf.Min(rect.width, rect.height) * .25f);
            if (inset <= 0f) return;
            AddQuad(vertices, new Rect(rect.xMin, rect.yMax - inset, rect.width, inset));
            AddQuad(vertices, new Rect(rect.xMin, rect.yMin, rect.width, inset));
            AddQuad(vertices, new Rect(rect.xMin, rect.yMin + inset, inset, rect.height - inset * 2f));
            AddQuad(vertices, new Rect(rect.xMax - inset, rect.yMin + inset, inset, rect.height - inset * 2f));
        }

        private void AddQuad(VertexHelper vertices, Rect rect)
        {
            var start = vertices.currentVertCount;
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = new Vector2(rect.xMin, rect.yMin); vertices.AddVert(vertex);
            vertex.position = new Vector2(rect.xMin, rect.yMax); vertices.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMax); vertices.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMin); vertices.AddVert(vertex);
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start, start + 2, start + 3);
        }
    }
}
