using UnityEngine;
using UnityEngine.U2D;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteShapeController))]
public class SpriteShapeTextureFill : MonoBehaviour
{
    [Tooltip("Texture PNG à répéter (Wrap Mode must be Repeat).")]
    public Texture2D fillTexture;

    [Tooltip("Taille d'une tuile. Plus grand = moins de répétitions.")]
    public float tileScale = 1f;

    private SpriteShapeController shapeController;

    void Start()
    {
        shapeController = GetComponent<SpriteShapeController>();
        if (fillTexture == null)
        {
            Debug.LogError("[Fill] Aucune texture assignée !");
            return;
        }

        fillTexture.wrapMode = TextureWrapMode.Repeat;
        StartCoroutine(GenerateFillNextFrame());
    }

    IEnumerator GenerateFillNextFrame()
    {
        yield return new WaitForEndOfFrame();

        // Ajout automatique du PolygonCollider2D si nécessaire
        var poly = GetComponent<PolygonCollider2D>();
        if (poly == null)
        {
            poly = gameObject.AddComponent<PolygonCollider2D>();
            Debug.Log("[Fill] PolygonCollider2D manquant, création automatique.");
        }

        // Génère la forme physique
        shapeController.BakeCollider();
        if (poly.pathCount == 0)
        {
            Debug.LogError("[Fill] Aucun path détecté. Vérifiez Create Physics Shape.");
            yield break;
        }

        var points = poly.GetPath(0);
        Debug.Log($"[Fill] Nombre de points : {points.Length}");
        if (points.Length < 3)
        {
            Debug.LogError("[Fill] Moins de 3 points : impossible de trianguler.");
            yield break;
        }

        int[] tris = Triangulate(points);
        Debug.Log($"[Fill] Triangles créés : {tris.Length / 3}");

        // Création de l'objet enfant pour le remplissage
        var fillObj = new GameObject("SpriteShapeFill");
        fillObj.transform.SetParent(transform, false);
        var mf = fillObj.AddComponent<MeshFilter>();
        var mr = fillObj.AddComponent<MeshRenderer>();

        // Construction du mesh
        var mesh = new Mesh();
        Vector3[] vertices = new Vector3[points.Length];
        Vector2[] uvs = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            vertices[i] = points[i];
            uvs[i] = points[i] / tileScale;
        }
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        // Matériau et texture
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = fillTexture;
        mr.material = mat;

        // Copier tri-layer et order pour correspondre exactement au SpriteShapeRenderer du parent
        var sr = shapeController.spriteShapeRenderer;
        if (sr != null)
        {
            mr.sortingLayerID = sr.sortingLayerID;
            mr.sortingOrder   = sr.sortingOrder;
        }

        Debug.Log("[Fill] Remplissage créé avec succès !");

        // Détache l'enfant avant de détruire le parent, pour garder le mesh en scène
        var sceneParent = transform.parent;
        fillObj.transform.SetParent(sceneParent, true);

        // Supprime le GameObject parent devenu inutile
        Destroy(gameObject);
    }

    // Méthode d’oreille pour trianguler un polygone
    int[] Triangulate(Vector2[] pts)
    {
        var indices = new List<int>(pts.Length);
        for (int i = 0; i < pts.Length; i++) indices.Add(i);
        var tris = new List<int>();
        int safety = 0;

        while (indices.Count > 3 && ++safety < 1000)
        {
            bool earFound = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int i0 = indices[(i + indices.Count - 1) % indices.Count];
                int i1 = indices[i];
                int i2 = indices[(i + 1) % indices.Count];
                var a = pts[i0];
                var b = pts[i1];
                var c = pts[i2];

                if (Vector2.SignedAngle(b - a, c - b) >= 0) continue;

                bool anyInside = false;
                for (int j = 0; j < indices.Count; j++)
                {
                    int vi = indices[j];
                    if (vi == i0 || vi == i1 || vi == i2) continue;
                    if (PointInTriangle(pts[vi], a, b, c)) { anyInside = true; break; }
                }
                if (anyInside) continue;

                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                indices.RemoveAt(i);
                earFound = true;
                break;
            }
            if (!earFound) break;
        }
        if (indices.Count == 3)
            tris.AddRange(new[]{indices[0], indices[1], indices[2]});

        return tris.ToArray();
    }

    bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float A  = Mathf.Abs((a.x*(b.y-c.y) + b.x*(c.y-a.y) + c.x*(a.y-b.y))*0.5f);
        float A1 = Mathf.Abs((p.x*(b.y-c.y) + b.x*(c.y-p.y) + c.x*(p.y-b.y))*0.5f);
        float A2 = Mathf.Abs((a.x*(p.y-c.y) + p.x*(c.y-a.y) + c.x*(a.y-p.y))*0.5f);
        float A3 = Mathf.Abs((a.x*(b.y-p.y) + b.x*(p.y-a.y) + p.x*(a.y-b.y))*0.5f);
        return Mathf.Abs(A - (A1 + A2 + A3)) < 1e-4f;
    }
}
