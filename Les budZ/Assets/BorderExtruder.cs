using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[ExecuteAlways]
public class BorderExtruder : MonoBehaviour
{
    [Header("Références")]
    public MeshFilter sourceMeshFilter;        // Le MeshFilter de l'objet plat original
    public Material overrideMaterial = null;   // Matériau optionnel pour la bordure

    [Header("Paramètres")]
    public float depth = 0.5f;                // Profondeur de la bordure (sur Z)
    public float topRepeat = 0.1f;    // combien de fois la texture se répète sur la face supérieure (en XY)
    public float sideRepeatU = 0.1f;  // répétition le long de la longueur du contour
    public float sideRepeatV = 0.1f;  // répétition sur la profondeur (depth)
    public bool autoCalculetedDepth = false;
    
    private const string borderName = "BorderMesh";

    void Start()
    {
        if (sourceMeshFilter == null)
        {
            Debug.LogError("[BorderExtruder] sourceMeshFilter non assigné.");
            return;
        }

        Mesh sourceMesh = sourceMeshFilter.sharedMesh;
        if (sourceMesh == null)
        {
            Debug.LogError("[BorderExtruder] Le MeshFilter n'a pas de mesh.");
            return;
        }

        // Déterminer le matériau à utiliser (override ou celui de la source)
        Material matToUse = overrideMaterial;
        var sourceRenderer = sourceMeshFilter.GetComponent<MeshRenderer>();
        if (matToUse == null && sourceRenderer != null)
        {
            matToUse = sourceRenderer.sharedMaterial;
        }

        if (matToUse == null)
        {
            Debug.LogWarning("[BorderExtruder] Aucun matériau défini ni override ni sur l'objet source. Bordure non générée.");
            return;
        }

        // Supprimer l'ancien enfant si existant
        Transform existing = sourceMeshFilter.transform.Find(borderName);
        if (existing != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(existing.gameObject);
            else
#endif
                Destroy(existing.gameObject);
        }

        Vector3[] origVerts = sourceMesh.vertices;
        int[] origTris = sourceMesh.triangles;
        int n = origVerts.Length;

        if (autoCalculetedDepth)
        {
            depth -= transform.position.z;
        }
        
        // Sommets : base + extrudés
        Vector3[] newVerts = new Vector3[n * 2];
        for (int i = 0; i < n; i++)
        {
            newVerts[i] = origVerts[i];
            newVerts[i + n] = origVerts[i] + new Vector3(0, 0, depth);
        }
        
        Vector2[] uvs = new Vector2[n * 2];
        for (int i = 0; i < n; i++)
        {
            Vector3 v = origVerts[i];
            uvs[i] = new Vector2(v.x, v.y) * topRepeat;
            // la version extrudée en hauteur peut reprendre la même UV en Y si tu veux que la texture
            // sur le dessus soit identique (ou la modifier si nécessaire)
            uvs[i + n] = new Vector2(v.x, v.y) * topRepeat;
        }
        
        
        
        // Construire l'ensemble d'arêtes frontières
        var edgeSet = new HashSet<(int, int)>();
        for (int t = 0; t < origTris.Length; t += 3)
        {
            AddOrRemoveEdge(edgeSet, origTris[t], origTris[t + 1]);
            AddOrRemoveEdge(edgeSet, origTris[t + 1], origTris[t + 2]);
            AddOrRemoveEdge(edgeSet, origTris[t + 2], origTris[t]);
        }

        // Reconstruire les boucles ordonnées
        List<List<int>> loops = BuildOrderedLoops(edgeSet);

        // Identifier la boucle externe : celle avec la plus grande aire absolue
        int outerLoopIndex = -1;
        float maxArea = 0f;
        for (int i = 0; i < loops.Count; i++)
        {
            float area = Mathf.Abs(ComputeSignedArea(loops[i], origVerts));
            if (area > maxArea)
            {
                maxArea = area;
                outerLoopIndex = i;
            }
        }

        var newTriangles = new List<int>();

        // Générer les faces latérales
        for (int li = 0; li < loops.Count; li++)
        {
            var loop = loops[li];
            if (loop.Count < 2) continue;

            bool isOuter = (li == outerLoopIndex);

            for (int i = 0; i < loop.Count; i++)
            {
                int a = loop[i];
                int b = loop[(i + 1) % loop.Count];

                if (isOuter)
                {
                    // Pour la boucle externe : on veut que les normales pointent vers l'extérieur,
                    // donc on inverse le winding par rapport à la version “naïve”
                    // Tri 1 inversé : (b, a, b + n)
                    newTriangles.Add(b);
                    newTriangles.Add(a);
                    newTriangles.Add(b + n);
                    // Tri 2 inversé : (a + n, b + n, a)
                    newTriangles.Add(a + n);
                    newTriangles.Add(b + n);
                    newTriangles.Add(a);
                }
                else
                {
                    // Boucles internes : on les laisse comme avant (normales moins visibles)
                    newTriangles.Add(a);
                    newTriangles.Add(b);
                    newTriangles.Add(b + n);
                    newTriangles.Add(b + n);
                    newTriangles.Add(a + n);
                    newTriangles.Add(a);
                }
            }
        }
        
        foreach (var loop in loops)
        {
            // Calculer la longueur totale de la boucle (en XY) pour normaliser si on veut
            float accumulated = 0f;
            for (int i = 0; i < loop.Count; i++)
            {
                int a = loop[i];
                int b = loop[(i + 1) % loop.Count];
                Vector3 va = origVerts[a];
                Vector3 vb = origVerts[b];
                float edgeLen = Vector3.Distance(new Vector3(va.x, va.y, 0f), new Vector3(vb.x, vb.y, 0f));

                // Pour chaque extrémité de l'arête, on peut définir des UV "à la volée"
                // U : distance cumulative le long du contour
                // V : 0 en bas, sideRepeatV en haut (profondeur)
                float u0 = accumulated * sideRepeatU;
                accumulated += edgeLen;
                float u1 = accumulated * sideRepeatU;

                // On met à jour les uv pour les sommets a et b (les extrudés sont en V = sideRepeatV)
                // On choisit de garder la valeur la plus pertinente si plusieurs arêtes touchent un même vertex
                uvs[a] = new Vector2(u0, 0f);
                uvs[b] = new Vector2(u1, 0f);
                uvs[a + n] = new Vector2(u0, sideRepeatV);
                uvs[b + n] = new Vector2(u1, sideRepeatV);
            }
        }

        // Création du mesh de bordure
        Mesh borderMesh = new Mesh();
        borderMesh.name = "Border_Mesh_Generated";
        borderMesh.vertices = newVerts;
        borderMesh.triangles = newTriangles.ToArray();
        borderMesh.RecalculateNormals();
        borderMesh.RecalculateBounds();

        // Création de l'objet enfant
        GameObject borderObject = new GameObject(borderName);
        borderObject.transform.SetParent(sourceMeshFilter.transform, false);
        borderObject.transform.localPosition = Vector3.zero;
        borderObject.transform.localRotation = Quaternion.identity;
        borderObject.transform.localScale = Vector3.one;
        borderObject.layer = gameObject.layer;

        var mf = borderObject.AddComponent<MeshFilter>();
        var mr = borderObject.AddComponent<MeshRenderer>();
        var mc = borderObject.AddComponent<MeshCollider>();
        mf.mesh = borderMesh;
        mr.material = matToUse;
        mc.sharedMesh = borderMesh;
        borderMesh.uv = uvs;
    }

    void AddOrRemoveEdge(HashSet<(int, int)> set, int i, int j)
    {
        var edge = (i < j) ? (i, j) : (j, i);
        if (!set.Add(edge))
            set.Remove(edge);
    }

    // Reconstitue des boucles (chaînes) à partir de l'ensemble d'arêtes
    List<List<int>> BuildOrderedLoops(HashSet<(int, int)> edges)
    {
        var result = new List<List<int>>();
        var adjacency = new Dictionary<int, List<int>>();

        foreach (var (u, v) in edges)
        {
            if (!adjacency.ContainsKey(u)) adjacency[u] = new List<int>();
            if (!adjacency.ContainsKey(v)) adjacency[v] = new List<int>();
            adjacency[u].Add(v);
            adjacency[v].Add(u);
        }

        var visitedVerts = new HashSet<int>();

        foreach (var start in adjacency.Keys)
        {
            if (visitedVerts.Contains(start)) continue;

            var loop = new List<int>();
            int current = start;
            int previous = -1;

            while (true)
            {
                loop.Add(current);
                visitedVerts.Add(current);

                var neighbors = adjacency[current];
                int next = -1;

                foreach (var nb in neighbors)
                {
                    if (nb == previous) continue;
                    next = nb;
                    break;
                }

                if (next == -1) break;
                previous = current;
                current = next;

                if (current == start) break;
                if (loop.Contains(current)) break;
            }

            if (loop.Count >= 2)
                result.Add(loop);
        }

        return result;
    }

    // Aire signée dans le plan XY pour déterminer orientation
    float ComputeSignedArea(List<int> loop, Vector3[] verts)
    {
        float area = 0f;
        int count = loop.Count;
        for (int i = 0; i < count; i++)
        {
            Vector3 a = verts[loop[i]];
            Vector3 b = verts[loop[(i + 1) % count]];
            area += (a.x * b.y) - (b.x * a.y);
        }
        return area * 0.5f;
    }
}
