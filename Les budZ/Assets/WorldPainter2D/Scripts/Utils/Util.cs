using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace WorldPainter2D
{
    public static class Util
    {
        /// <summary>
        /// Extension that converts an array of Vector2 to an array of Vector3
        /// </summary>
        public static Vector3[] ToVector3(this Vector2[] vectors)
        {
            return System.Array.ConvertAll<Vector2, Vector3>(vectors, v => v);
        }

        /// <summary>
        /// Extension that, given a collection of vectors, returns a centroid 
        /// (i.e., an average of all vectors) 
        /// </summary>
        public static Vector2 Centroid(this ICollection<Vector2> vectors)
        {
            return vectors.Aggregate((agg, next) => agg + next) / vectors.Count();
        }

        /// <summary>
        /// Extension returning the absolute value of a vector
        /// </summary>
        public static Vector2 Abs(this Vector2 vector)
        {
            return new Vector2(Mathf.Abs(vector.x), Mathf.Abs(vector.y));
        }

        public static Vector2 ClosestPointToMultiple(Vector2 v, float n)
        {
            v.x = Mathf.Round(v.x / n) * n;
            v.y = Mathf.Round(v.y / n) * n;

            return v;
        }

        public static Vector3 Vec3FromVec2(Vector2 v, float z)
        {
            return new Vector3(v.x, v.y, z);
        }

        public static void DrawGrid(Vector2 centerPos, float gridRadius, float gridGranularity, Color gridCenterColor, Color gridColor, float depth = 0, bool drawCenter = true)
        {
            if (drawCenter)
            {
                Gizmos.color = gridCenterColor;
                Gizmos.DrawSphere(Vec3FromVec2(centerPos, depth), gridGranularity * 1.5f / 5);
            }

            if (Mathf.Pow(gridRadius / gridGranularity, 2) <= 200)
            {
                Gizmos.color = gridColor;
                for (float x = -gridRadius; x <= gridRadius; x += gridGranularity)
                {
                    for (float y = -gridRadius; y <= gridRadius; y += gridGranularity)
                    {
                        var pos = centerPos;
                        pos.x += x;
                        pos.y += y;

                        var circlePos = ClosestPointToMultiple(pos, gridGranularity);

                        if (Vector2.Distance(centerPos, circlePos) <= gridRadius)
                        {
                            var alpha = Mathf.Min(1 - Mathf.Pow(Vector2.Distance(centerPos, circlePos) / gridRadius, 2), 1);
                            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, alpha);
                            Gizmos.DrawSphere(Vec3FromVec2(circlePos, depth), gridGranularity / 5);
                        }
                    }
                }
            }
        }
    }
}
