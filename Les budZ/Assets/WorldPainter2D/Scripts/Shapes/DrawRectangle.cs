using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace WorldPainter2D
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class DrawRectangle : DrawShape
    {

        // Start and end vertices (in absolute coordinates)
        private readonly List<Vector2> _vertices = new List<Vector2>(2);

        public override bool ShapeFinished { get { return _vertices.Count >= 2; } }

        private BoxCollider2D _boxCollider2D;


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
            if (_boxCollider2D == null) _boxCollider2D = GetComponent<BoxCollider2D>();

            _vertices[_vertices.Count - 1] = newVertex;

            // Set the gameobject's position to be the center of mass
            var center = _vertices.Centroid();
            transform.position = center;

            // Update the mesh relative to the transform
            var relativeVertices = _vertices.Select(v => v - center).ToArray();
            _meshFilter.mesh = RectangleMesh(relativeVertices[0], relativeVertices[1], FillColor);

            // Update the collider
            var dimensions = (_vertices[1] - _vertices[0]).Abs();
            _boxCollider2D.size = dimensions;

        }

        /// <summary>
        /// Creates and returns a rectangle mesh given two vertices on its 
        /// opposite corners and fills it with the given color. 
        /// </summary>
        private static Mesh RectangleMesh(Vector2 v0, Vector2 v1, Color fillColor)
        {
            // Calculate implied verticies from corner vertices
            // Note: vertices must be adjacent to each other for Triangulator to work properly
            var v2 = new Vector2(v0.x, v1.y);
            var v3 = new Vector2(v1.x, v0.y);
            var rectangleVertices = new[] { v0, v2, v1, v3 };

            // Find all the triangles in the shape
            var triangles = new Triangulator(rectangleVertices).Triangulate();

            // Assign each vertex the fill color
            var colors = Enumerable.Repeat(fillColor, rectangleVertices.Length).ToArray();

            var mesh = new Mesh
            {
                name = "Rectangle",
                vertices = rectangleVertices.ToVector3(),
                triangles = triangles,
                colors = colors
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }
    }
}