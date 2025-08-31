using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

[RequireComponent(typeof(SpriteShapeController))]
public class CollidersToSpriteShape : MonoBehaviour
{
    [Header("Inputs")]
    [Tooltip("Liste des colliders 2D à fusionner pour former la forme fermée.")]
    public Collider2D[] colliders;

    [Tooltip("Profil SpriteShape à utiliser pour générer le shape child.")]
    public SpriteShape spriteShapeProfile;

    [Tooltip("Si activé, génère et crée le child au démarrage.")]
    public bool updateOnStart = true;

    private SpriteShapeController parentController;

    void Start()
    {
        parentController = GetComponent<SpriteShapeController>();
        if (updateOnStart)
            GenerateShapeChild();
    }

    /// <summary>
    /// Génère la forme à partir des colliders, crée un GameObject enfant
    /// avec SpriteShapeController/Renderer, et y applique la spline.
    /// </summary>
    [ContextMenu("Generate Shape Child")]
    public void GenerateShapeChild()
    {
        if (colliders == null || colliders.Length == 0)
        {
            Debug.LogWarning("[CollidersToSpriteShape] Aucun collider assigné.");
            return;
        }
        if (spriteShapeProfile == null)
        {
            Debug.LogError("[CollidersToSpriteShape] Aucun SpriteShapeProfile assigné.");
            return;
        }

        // Collecte des points de tous les colliders
        List<Vector2> allPoints = new List<Vector2>();
        foreach (var col in colliders)
        {
            if (col == null) continue;
            if (col is PolygonCollider2D poly)
            {
                for (int p = 0; p < poly.pathCount; p++)
                {
                    var path = poly.GetPath(p);
                    allPoints.AddRange(path.Select(v => col.transform.TransformPoint(v)));
                }
            }
            else if (col is BoxCollider2D box)
            {
                Vector2 size = box.size * 0.5f;
                Vector2[] corners = new Vector2[] {
                    new Vector2(-size.x, -size.y),
                    new Vector2( size.x, -size.y),
                    new Vector2( size.x,  size.y),
                    new Vector2(-size.x,  size.y)
                };
                allPoints.AddRange(corners.Select(v => box.transform.TransformPoint(v + box.offset)));
            }
            else if (col is CircleCollider2D circle)
            {
                int segments = 16;
                float angleStep = 360f / segments;
                for (int i = 0; i < segments; i++)
                {
                    float ang = Mathf.Deg2Rad * (i * angleStep);
                    Vector2 pt = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * circle.radius;
                    allPoints.Add(circle.transform.TransformPoint(pt + circle.offset));
                }
            }
            else
            {
                Debug.LogWarning($"[CollidersToSpriteShape] Collider non géré: {col.GetType().Name}");
            }
        }

        if (allPoints.Count < 3)
        {
            Debug.LogError("[CollidersToSpriteShape] Pas assez de points pour former une shape.");
            return;
        }

        // Calcul de l'enveloppe convexe (Convex Hull)
        List<Vector2> hull = ComputeConvexHull(allPoints);

        // Création de l'objet enfant pour le SpriteShape
        GameObject child = new GameObject("GeneratedSpriteShape");
        child.transform.SetParent(transform, false);

        // Ajout du SpriteShapeController et assignation du profil
        var childController = child.AddComponent<SpriteShapeController>();
        childController.spriteShape = spriteShapeProfile;

        // Ajout du Renderer
        var childRenderer = child.AddComponent<SpriteShapeRenderer>();

        // Construction de la spline
        var spline = childController.spline;
        spline.Clear();
        for (int i = 0; i < hull.Count; i++)
        {
            // Convertit les points du monde en local
            Vector3 localPos = transform.InverseTransformPoint(hull[i]);
            spline.InsertPointAt(i, localPos);
            spline.SetCorner(i, false); // coins lissés pour arrondir
        }
        spline.isOpenEnded = false; // forme fermée

        // Génération du collider interne si nécessaire
        childController.BakeCollider();

        Debug.Log($"[CollidersToSpriteShape] Child créé avec {hull.Count} points.");
    }

    /// <summary>
    /// Algorithme de Monotone Chain pour le Convex Hull
    /// </summary>
    private List<Vector2> ComputeConvexHull(List<Vector2> pts)
    {
        var points = pts.Distinct().ToList();
        points.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
        if (points.Count < 3) return points;

        List<Vector2> lower = new List<Vector2>();
        foreach (var p in points)
        {
            while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0)
                lower.RemoveAt(lower.Count - 1);
            lower.Add(p);
        }

        List<Vector2> upper = new List<Vector2>();
        for (int i = points.Count - 1; i >= 0; i--)
        {
            var p = points[i];
            while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0)
                upper.RemoveAt(upper.Count - 1);
            upper.Add(p);
        }

        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);
        lower.AddRange(upper);
        return lower;
    }

    private float Cross(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }
}
