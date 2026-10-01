using UnityEngine;
using UnityEngine.UI;

namespace WhoEnters.Integration
{
    /// <summary>
    /// Filled code-native arched aperture. It is mask-only: the authored visitor-card raster
    /// remains the visible foreground frame, gargoyles, banner and all.
    /// </summary>
    public sealed class PortraitFrameGraphic : Graphic
    {
        public const int ArcSegments = 16;

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var rect = GetPixelAdjustedRect();
            // The arch is inset from the baked ornate edge. Its bottom exactly meets the native
            // aperture bottom, so preserving a 2:3 portrait does not introduce a bottom gap.
            var inset = Mathf.Min(8f, rect.width * .04f);
            var left = rect.xMin + inset;
            var right = rect.xMax - inset;
            var bottom = rect.yMin;
            var shoulder = rect.yMax - rect.height * .34f;
            var radius = (right - left) * .5f;
            var centre = new Vector2(rect.center.x, shoulder);
            Add(vertices, new Vector2(left, bottom));
            Add(vertices, new Vector2(right, bottom));
            Add(vertices, new Vector2(right, shoulder));
            for (var step = 0; step <= ArcSegments; step++)
            {
                var angle = step * Mathf.PI / ArcSegments;
                Add(vertices, centre + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
            }
            Add(vertices, new Vector2(left, shoulder));
            for (var index = 1; index < vertices.currentVertCount - 1; index++)
                vertices.AddTriangle(0, index, index + 1);
        }

        private void Add(VertexHelper vertices, Vector2 point)
        {
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = point;
            vertices.AddVert(vertex);
        }
    }
}
