using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldPainter2D
{
    [RequireComponent(typeof(MeshFilter))]
    public abstract class DrawShape : MonoBehaviour
    {
        public Color FillColor = Color.white;
        protected MeshFilter _meshFilter;
        /// <summary> 
        /// Whether all the points in the shape have been specified 
        /// </summary> 
        public abstract bool ShapeFinished { get; }


        /// <summary> 
        /// Adds a new vertex to the shape. The shape should  
        /// also update its mesh and collider. 
        /// </summary> 
        public abstract void AddVertex(Vector2 vertex);

        /// <summary> 
        /// Updates the last added vertex with the new given position. 
        /// The shape should also update its mesh and collider. 
        /// </summary> 
        public abstract void UpdateShape(Vector2 newVertex);

        public virtual List<Vector2> Vertices => null;
    }
}