using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace WorldPainter2D
{
    [RequireComponent(typeof(PolygonCollider2D), typeof(LineRenderer))]
    public class DrawPolygon : DrawShape
    {
        private PolygonCollider2D _polygonCollider2D;
        private LineRenderer _lineRenderer;
        private bool shapeSetFinish;

        private readonly List<Vector2> _vertices = new List<Vector2>();

        public bool CanBeFinished { get { return _vertices.Count > 3; } }
        public override bool ShapeFinished { get { return shapeSetFinish; } }

        public override List<Vector2> Vertices => _vertices;

        public override void AddVertex(Vector2 vertex)
        {
            if (ShapeFinished) return;

            _vertices.Add(vertex);
            UpdateShape(vertex);
        }

        public override void UpdateShape(Vector2 newVertex)
        {
            if (_vertices.Count < 2) return;

            _vertices[^1] = newVertex;
            CalculateShape();
        }

        private void CalculateShape()
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_polygonCollider2D == null) _polygonCollider2D = GetComponent<PolygonCollider2D>();
            if (_lineRenderer == null) _lineRenderer = GetComponent<LineRenderer>();

            // Set the gameobject's position to be the center of mass
            var center = _vertices.Centroid();
            transform.position = center;

            // Update the mesh relative to the transform
            //var relativeVertices = _vertices.Select(v => v - center).ToArray();

            var relativeVertices = _vertices.Select(v => v - center).ToArray();
            _meshFilter.mesh = PolygonMesh(relativeVertices, FillColor);

            var gr = new Gradient();
            var grCK = new GradientColorKey[1];
            var grA = new GradientAlphaKey[1];

            grCK[0].color = Color.white - FillColor;
            grA[0].alpha = 1;
            grCK[0].time = 1; grA[0].time = 1;
            gr.SetKeys(grCK, grA);

            _lineRenderer.colorGradient = gr;
            _lineRenderer.positionCount = _vertices.Count;
            Vector3[] renderVertices = new Vector3[_vertices.Count];
            for (int i = 0; i < _vertices.Count; i++)
            {
                renderVertices[i] = new Vector3(_vertices[i].x, _vertices[i].y, 0);
            }
            _lineRenderer.SetPositions(renderVertices);

            // Update the collider
            _polygonCollider2D.points = relativeVertices;
        }


        /// <summary>
        /// Creates and returns a polygon mesh given a list of its vertices.
        /// </summary>
        private static Mesh PolygonMesh(Vector2[] vertices, Color fillColor)
        {
            if (vertices[^1] == vertices[^2] || vertices[^1] == vertices[0])
                vertices = vertices.SkipLast(1).ToArray();

            // Find all the triangles in the shape
            var triangles = new Triangulator(vertices).Triangulate();

            // Assign each vertex the fill color
            var colors = Enumerable.Repeat(fillColor, vertices.Length).ToArray();

            var mesh = new Mesh
            {
                name = "Polygon",
                vertices = vertices.ToVector3(),
                triangles = triangles,
                colors = colors
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public void SetFinishedShape()
        {
            shapeSetFinish = true;
            _vertices.RemoveAt(_vertices.Count - 1);
            CalculateShape();
        }
    }
}