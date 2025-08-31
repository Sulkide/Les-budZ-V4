using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace WorldPainter2D
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class DrawCircle : DrawShape
    {


        // Center and radius points
        private readonly List<Vector2> _vertices = new List<Vector2>(2);

        private CircleCollider2D _circleCollider2D;


        //public override bool ShapeFinished => throw new System.NotImplementedException();
        public override bool ShapeFinished { get { return _vertices.Count >= 2; } }

        public override void AddVertex(Vector2 vertex)
        {
            if (ShapeFinished) return;

            _vertices.Add(vertex);
            UpdateShape(vertex);
        }

        public override void UpdateShape(Vector2 newVertex)
        {
            if (_vertices.Count < 2) return;
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_circleCollider2D == null) _circleCollider2D = GetComponent<CircleCollider2D>();

            _vertices[_vertices.Count - 1] = newVertex;

            // Set the gameobject's position to be the center of mass
            transform.position = _vertices[0];

            // Update the mesh relative to the transform
            var v0Relative = Vector2.zero;
            var v1Relative = _vertices[1] - _vertices[0];
            _meshFilter.mesh = CircleMesh(v0Relative, v1Relative, FillColor);

            // Update the collider
            _circleCollider2D.radius = Vector2.Distance(_vertices[0], _vertices[1]);

        }

        /// <summary>
        /// Creates and returns a circle mesh given two vertices on its center 
        /// and any outer edge point.
        /// </summary>
        private static Mesh CircleMesh(Vector2 v0, Vector2 v1, Color fillColor)
        {
            var radius = Vector2.Distance(v0, v1);

            // We want to make sure that the circle appears to be curved.
            // This can be approximated by drawing a regular polygon with lots of segments.
            // The number of segments can be increased based on the radius so that large circles also appear curved.
            // We use an offset and multiplier to create a tunable linear function.
            const float segmentOffset = 40f;
            const float segmentMultiplier = 2 * Mathf.PI;
            var numSegments = (int)(radius * segmentMultiplier + segmentOffset);

            // Create an array of points arround a cricle
            var circleVertices = Enumerable.Range(0, numSegments)
                .Select(i =>
                {
                    var theta = 2 * Mathf.PI * i / numSegments;
                    return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * radius;
                })
                .ToArray();

            // Find all the triangles in the shape
            var triangles = new Triangulator(circleVertices).Triangulate();

            // Assign each vertex the fill color
            var colors = Enumerable.Repeat(fillColor, circleVertices.Length).ToArray();

            var mesh = new Mesh
            {
                name = "Circle",
                vertices = circleVertices.ToVector3(),
                triangles = triangles,
                colors = colors
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
