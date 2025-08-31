using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class CrumplePaperEffect : MonoBehaviour
{
    [Header("Paramètres des pics (déformations)")]
    [SerializeField] int minFacesPerPeak = 3;    // Nombre minimal de faces par pic
    [SerializeField] int maxFacesPerPeak = 8;    // Nombre maximal de faces par pic
    [SerializeField] int minPeaks = 1;           // Nombre minimal de pics à générer
    [SerializeField] int maxPeaks = 5;           // Nombre maximal de pics à générer

    [SerializeField] float minHeight = 0.1f;     // Hauteur minimale d'une bosse (en unités Unity)
    [SerializeField] float maxHeight = 0.3f;     // Hauteur maximale d'une bosse
    [SerializeField] float minDepth = 0.1f;      // Profondeur minimale d'un creux (vers le bas)
    [SerializeField] float maxDepth = 0.3f;      // Profondeur maximale d'un creux

    [SerializeField] float minWidth = 0.2f;      // Largeur minimale du pic (rayon d'influence)
    [SerializeField] float maxWidth = 0.5f;      // Largeur maximale du pic (rayon d'influence)

    [Header("Préservation des bords")]
    [SerializeField] float borderMarginPercent = 0.1f;  // Pourcentage de marge à laisser intact depuis le bord

    void Start()
    {
        // Récupérer le maillage et en faire une copie pour ne pas modifier l'asset original
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh originalMesh = mf.sharedMesh;
        Mesh meshInstance = Instantiate(originalMesh);
        mf.mesh = meshInstance;  // Assigner le mesh instancié à l'objet
        Mesh mesh = mf.mesh;

        // Récupérer les données du maillage
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Identifier les sommets de bord à exclure
        int vertexCount = vertices.Length;
        bool[] isBorderVertex = new bool[vertexCount];  // tableau marquant les sommets de bord
        IdentifyBorderVertices(triangles, isBorderVertex);

        // Optionnel : calculer la marge intérieure en unités réelles d'après le pourcentage
        // On utilise le bounding box du mesh pour approximer la zone intérieure autorisée
        Bounds bounds = mesh.bounds;
        float marginX = bounds.size.x * borderMarginPercent;
        float marginY = bounds.size.y * borderMarginPercent;
        float minX = bounds.min.x + marginX;
        float maxX = bounds.max.x - marginX;
        float minY = bounds.min.y + marginY;
        float maxY = bounds.max.y - marginY;

        // Déterminer le nombre de pics à créer aléatoirement entre minPeaks et maxPeaks
        int peakCount = Random.Range(minPeaks, maxPeaks + 1);

        // Garder une trace des faces déjà utilisées par un pic pour éviter les chevauchements (optionnel)
        HashSet<int> usedFaces = new HashSet<int>();

        // Obtenir la liste des faces adjacentes (voisines) de chaque face, pour permettre les clusters continus
        Dictionary<int, List<int>> neighbors = BuildFaceAdjacency(triangles, vertices.Length);

        // Générer chaque pic
        for (int p = 0; p < peakCount; p++)
        {
            // Trouver une face de départ aléatoire qui n'est pas au bord et pas déjà utilisée
            int startFace = FindRandomStartFace(triangles, isBorderVertex, usedFaces);
            if (startFace == -1) break; // plus de face disponible
            usedFaces.Add(startFace);

            // Choisir aléatoirement la taille du cluster (nombre de faces) pour ce pic
            int targetFacesCount = Random.Range(minFacesPerPeak, maxFacesPerPeak + 1);

            // Rassembler le cluster de faces pour le pic (en utilisant une recherche en largeur d'abord autour de la face de départ)
            List<int> clusterFaces = new List<int>();
            clusterFaces.Add(startFace);
            // BFS expansion depuis la face de départ
            Queue<int> frontier = new Queue<int>();
            frontier.Enqueue(startFace);
            while (frontier.Count > 0 && clusterFaces.Count < targetFacesCount)
            {
                int currentFace = frontier.Dequeue();
                if (!neighbors.ContainsKey(currentFace)) continue;
                foreach (int neigh in neighbors[currentFace])
                {
                    // Ajouter la face voisine si pas déjà dans le cluster, pas déjà utilisée, et n'est pas en bord
                    if (!clusterFaces.Contains(neigh) && !usedFaces.Contains(neigh) && !FaceHasBorderVertex(neigh, triangles, isBorderVertex))
                    {
                        clusterFaces.Add(neigh);
                        usedFaces.Add(neigh);
                        frontier.Enqueue(neigh);
                        if (clusterFaces.Count >= targetFacesCount) break;
                    }
                }
            }

            // Récupérer l'ensemble des sommets uniques appartenant à ces faces
            HashSet<int> clusterVertices = new HashSet<int>();
            foreach (int faceIndex in clusterFaces)
            {
                int triStart = faceIndex * 3;
                // Ajouter les 3 indices de sommets de la face
                clusterVertices.Add(triangles[triStart]);
                clusterVertices.Add(triangles[triStart + 1]);
                clusterVertices.Add(triangles[triStart + 2]);
            }

            // Choisir aléatoirement si on fait une bosse (vers le haut) ou un creux (vers le bas)
            bool upward = (Random.value > 0.5f);
            float amplitude = upward 
                              ? Random.Range(minHeight, maxHeight)   // hauteur de la bosse
                              : -Random.Range(minDepth, maxDepth);   // profondeur du creux (valeur négative)

            // Choisir une largeur aléatoire pour ce pic (rayon d'influence approximatif)
            float radius = Random.Range(minWidth, maxWidth);

            // Calculer le centre approximatif du cluster (centre de la face de départ)
            Vector3 center = GetFaceCenter(startFace, triangles, vertices);

            // Appliquer la déformation aux sommets du cluster, avec une atténuation linéaire selon la distance au centre
            foreach (int vIndex in clusterVertices)
            {
                // Ne pas déplacer les sommets de bord ou ceux en dehors de la zone intérieure définie par la marge
                Vector3 vPos = vertices[vIndex];
                if (isBorderVertex[vIndex] || vPos.x < minX || vPos.x > maxX || vPos.y < minY || vPos.y > maxY)
                    continue;

                // Calculer la distance 2D du sommet au centre du pic (dans le plan X-Y)
                float dist = Vector2.Distance(new Vector2(vPos.x, vPos.y), new Vector2(center.x, center.y));
                if (dist > radius) 
                {
                    // En dehors du rayon d'influence, on peut éventuellement ne pas bouger le sommet
                    continue;
                }
                // Facteur d'atténuation (1 au centre, 0 aux bords du rayon)
                float t = 1f - (dist / radius);
                // Appliquer la déformation en Z en utilisant ce facteur (déplacement maximum * t)
                float deltaZ = amplitude * t;
                vertices[vIndex].z += deltaZ;
            }
        }

        // Appliquer les modifications de sommets au mesh
        mesh.vertices = vertices;
        // Recalculer normales et bounds pour finaliser le mesh
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    // Identifie les sommets de bord en marquant ceux appartenant à au moins une arête frontière
    void IdentifyBorderVertices(int[] triangles, bool[] isBorderVertex)
    {
        // Dictionnaire pour compter les occurrences des arêtes (sommets par paire)
        Dictionary<(int,int), int> edgeCount = new Dictionary<(int,int), int>();
        int faceCount = triangles.Length / 3;
        for (int fi = 0; fi < faceCount; fi++)
        {
            int a = triangles[fi * 3];
            int b = triangles[fi * 3 + 1];
            int c = triangles[fi * 3 + 2];
            AddEdgeCount(edgeCount, a, b);
            AddEdgeCount(edgeCount, b, c);
            AddEdgeCount(edgeCount, c, a);
        }
        // Une arête est de bord si elle n'apparaît qu'une seule fois
        foreach (var kvp in edgeCount)
        {
            if (kvp.Value == 1) 
            {
                // Marquer les deux sommets de cette arête comme sommets de bord
                (int va, int vb) = kvp.Key;
                isBorderVertex[va] = true;
                isBorderVertex[vb] = true;
            }
        }
    }
    void AddEdgeCount(Dictionary<(int,int), int> edgeCount, int v1, int v2)
    {
        // Ordonner l’arête par index croissant pour normaliser la clé
        var edge = (v1 < v2) ? (v1, v2) : (v2, v1);
        if (edgeCount.ContainsKey(edge))
            edgeCount[edge]++;
        else
            edgeCount[edge] = 1;
    }

    // Construit la liste des faces adjacentes pour chaque face (partageant une arête commune)
    Dictionary<int, List<int>> BuildFaceAdjacency(int[] triangles, int vertexCount)
    {
        Dictionary<(int,int), List<int>> edgeToFaces = new Dictionary<(int,int), List<int>>();
        int faceCount = triangles.Length / 3;
        for (int fi = 0; fi < faceCount; fi++)
        {
            int a = triangles[fi * 3];
            int b = triangles[fi * 3 + 1];
            int c = triangles[fi * 3 + 2];
            AddFaceToEdge(edgeToFaces, a, b, fi);
            AddFaceToEdge(edgeToFaces, b, c, fi);
            AddFaceToEdge(edgeToFaces, c, a, fi);
        }
        // Maintenant construire le dictionnaire de voisinage de faces
        Dictionary<int, List<int>> neighbors = new Dictionary<int, List<int>>();
        foreach (var kvp in edgeToFaces)
        {
            List<int> faceList = kvp.Value;
            if (faceList.Count == 2)
            {
                int f1 = faceList[0];
                int f2 = faceList[1];
                if (!neighbors.ContainsKey(f1)) neighbors[f1] = new List<int>();
                if (!neighbors.ContainsKey(f2)) neighbors[f2] = new List<int>();
                neighbors[f1].Add(f2);
                neighbors[f2].Add(f1);
            }
        }
        return neighbors;
    }
    void AddFaceToEdge(Dictionary<(int,int), List<int>> edgeToFaces, int v1, int v2, int faceIndex)
    {
        var edge = (v1 < v2) ? (v1, v2) : (v2, v1);
        if (!edgeToFaces.ContainsKey(edge))
            edgeToFaces[edge] = new List<int>();
        edgeToFaces[edge].Add(faceIndex);
    }

    // Vérifie si une face (index) contient au moins un sommet de bord
    bool FaceHasBorderVertex(int faceIndex, int[] triangles, bool[] isBorderVertex)
    {
        int triStart = faceIndex * 3;
        return (isBorderVertex[triangles[triStart]] ||
                isBorderVertex[triangles[triStart + 1]] ||
                isBorderVertex[triangles[triStart + 2]]);
    }

    // Trouve une face aléatoire utilisable comme départ de pic (interne, non utilisée, non sur bord)
    int FindRandomStartFace(int[] triangles, bool[] isBorderVertex, HashSet<int> usedFaces)
    {
        int faceCount = triangles.Length / 3;
        // Essayer un certain nombre de fois pour trouver une face valide
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int f = Random.Range(0, faceCount);
            if (usedFaces.Contains(f)) continue;
            // vérifie que la face n'a aucun sommet de bord
            if (FaceHasBorderVertex(f, triangles, isBorderVertex)) continue;
            return f;
        }
        return -1; // pas trouvé
    }

    // Calcule le centre (moyenne des sommets) d'une face donnée
    Vector3 GetFaceCenter(int faceIndex, int[] triangles, Vector3[] vertices)
    {
        int triStart = faceIndex * 3;
        Vector3 v1 = vertices[triangles[triStart]];
        Vector3 v2 = vertices[triangles[triStart + 1]];
        Vector3 v3 = vertices[triangles[triStart + 2]];
        return (v1 + v2 + v3) / 3f;
    }
}
