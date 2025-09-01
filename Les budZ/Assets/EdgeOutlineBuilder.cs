using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class EdgeOutlineBuilder : MonoBehaviour
{
    // ====== MODES ======
    public enum OutlineBuildMode { Lines, Quads }
    public enum GenerationMode { OutlineMesh, PrefabPattern }
    public enum PatternSelection { Sequential, Random }
    public enum ScaleMode { Uniform, XYZ }

    [Serializable]
    public struct AngleRange
    {
        public float minDeg;
        public float maxDeg;
        public bool Contains(float angleDeg)
        {
            float a = minDeg, b = maxDeg;
            if (a > b) (a, b) = (b, a);
            return angleDeg >= a && angleDeg <= b;
        }
    }

    [Header("Auto-generate")]
    [SerializeField] bool autoGenerateOnStart = true;       // génère au Start() en Play
    [SerializeField] bool autoGenerateInEditMode = false;   // (optionnel) génère aussi en Éditeur

    // ====== SOURCE ======
    [Header("Source")]
    public MeshFilter sourceMeshFilter; // auto si null

    // ====== SÉLECTION D'ARÊTES ======
    [Header("Edge Selection")]
    [Range(0f, 180f)] public float angleThreshold = 0f;
    public List<AngleRange> allowedAngleRanges = new List<AngleRange>();
    public bool includeBorderEdges = true;
    public bool treatBorderWithAngle = true;
    [Range(0f, 180f)] public float borderAngleDeg = 180f;

    // ====== SORTIE : OUTLINE MESH ======
    [Header("Outline Mesh Rendering")]
    public GenerationMode generationMode = GenerationMode.OutlineMesh;
    public OutlineBuildMode buildMode = OutlineBuildMode.Quads;
    public Color outlineColor = Color.black;
    [Min(0.0001f)] public float worldThickness = 0.05f;
    [Tooltip("Assigne un matériau asset pour éviter Shader.Find en build.")]
    public Material overrideMaterial;

    [Header("Outline Mesh Output")]
    public string outlineChildName = "_Outline";
    public bool markStatic = true;

    // ====== SORTIE : PATTERN PREFABS ======
    [Header("Prefab Pattern")]
    public bool patternEnabled = false;
    public List<GameObject> patternPrefabs = new List<GameObject>();
    public PatternSelection patternSelection = PatternSelection.Sequential;
    [Min(0.01f)] public float patternSpacing = 0.5f;
    public bool alignToEdgeDirection = true;
    public ScaleMode scaleMode = ScaleMode.Uniform;
    public Vector2 uniformScaleMinMax = new Vector2(1f, 1f);
    public Vector3 scaleMin = Vector3.one;
    public Vector3 scaleMax = Vector3.one;
    public Vector2 rotationZMinMax = Vector2.zero;
    public Material patternOverrideMaterial;
    public string patternTempParentName = "_OutlinePattern";
    public string patternCombinedName = "_OutlinePatternCombined";

    // ====== SAVE OPTIONS ======
    [Header("Saving")]
    [Tooltip("If off (default), generated meshes/materials are transient and NOT saved in the scene file.")]
    public bool saveAsAssets = false;

#if UNITY_EDITOR
    [Tooltip("Folder under Assets/ where generated .asset files are stored.")]
    public string assetsFolderUnderAssets = "Generated/Outlines";
#endif

    // ===== cache runtime =====
    Mesh _outlineMesh;
    GameObject _outlineGO;
    MeshFilter _outlineMF;
    MeshRenderer _outlineMR;

    private void Start()
    {
        if (autoGenerateOnStart && (Application.isPlaying || autoGenerateInEditMode))
        {
            // Assure la source puis génère.
            TryAutoAssign();
            GenerateOutline();
        }
    }

    void OnEnable() { TryAutoAssign(); }
    void OnValidate() { TryAutoAssign(); }
    void TryAutoAssign() { if (sourceMeshFilter == null) sourceMeshFilter = GetComponent<MeshFilter>(); }
    bool HasSource() => sourceMeshFilter != null && sourceMeshFilter.sharedMesh != null;

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    // ====== OUTLINE MESH PIPELINE ======

    void EnsureOutlineObjects()
    {
        // (Re)trouve/crée l'enfant pour le mesh d'outline
        Transform found = null;
        for (int i = 0; i < transform.childCount; i++)
        {
            var ch = transform.GetChild(i);
            if (ch != null && ch.name == outlineChildName) { found = ch; break; }
        }

        if (found == null)
        {
            _outlineGO = new GameObject(outlineChildName);
            _outlineGO.transform.SetParent(transform, false);
        }
        else
        {
            _outlineGO = found.gameObject;
        }

        if (markStatic) _outlineGO.isStatic = true;

        _outlineMF = GetOrAdd<MeshFilter>(_outlineGO);
        _outlineMR = GetOrAdd<MeshRenderer>(_outlineGO);

        // Matériau
        if (overrideMaterial != null)
        {
            _outlineMR.sharedMaterial = overrideMaterial;
        }
        else
        {
            // Create a transient material (won't be saved to scene)
            var mat = _outlineMR.sharedMaterial;
            if (mat == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                mat = new Material(sh);
            }
            SetMaterialColor(mat, outlineColor);
            if (!saveAsAssets)
            {
                mat.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }
            _outlineMR.sharedMaterial = mat;
        }
    }

    static void SetMaterialColor(Material mat, Color c)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        else mat.color = c;
    }

    [ContextMenu("Generate Outline")]
    public void GenerateOutline()
    {
        if (!HasSource()) return;

        if (patternEnabled) generationMode = GenerationMode.PrefabPattern;

        ClearOutline();
        ClearPatternOutputs();

        if (generationMode == GenerationMode.OutlineMesh)
            BuildOutline(sourceMeshFilter.sharedMesh);
        else
            BuildPattern(sourceMeshFilter.sharedMesh);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    public void Regenerate() => GenerateOutline();

    [ContextMenu("Clear Outline")]
    public void ClearOutline()
    {
        // delete child
        var del = new List<GameObject>();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var ch = transform.GetChild(i);
            if (ch != null && ch.name == outlineChildName) del.Add(ch.gameObject);
        }
        foreach (var go in del)
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        // clean transient mesh
        if (_outlineMesh != null)
        {
#if UNITY_EDITOR
            if (saveAsAssets)
            {
                // if it’s an asset, keep it; user can delete .asset from disk if desired
            }
            else
#endif
            {
                if (Application.isPlaying) Destroy(_outlineMesh); else DestroyImmediate(_outlineMesh);
            }
            _outlineMesh = null;
        }

        _outlineGO = null; _outlineMF = null; _outlineMR = null;
    }

    void ClearPatternOutputs()
    {
        var del = new List<GameObject>();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var ch = transform.GetChild(i);
            if (ch == null) continue;
            if (ch.name == patternTempParentName || ch.name == patternCombinedName)
                del.Add(ch.gameObject);
        }
        foreach (var go in del)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }

    // ====== CONSTRUCTION COMMUNE ======

    struct Edge { public int a, b; public int faceA; public int faceB; } // faceB -1 if border

    class EdgeKeyComparer : IEqualityComparer<(int, int)>
    {
        public bool Equals((int, int) x, (int, int) y) => x.Item1 == y.Item1 && x.Item2 == y.Item2;
        public int GetHashCode((int, int) obj) => (obj.Item1 * 397) ^ obj.Item2;
    }

    Dictionary<(int,int), Edge> BuildEdgeMap(int[] tris)
    {
        var edges = new Dictionary<(int,int), Edge>(new EdgeKeyComparer());
        for (int t = 0; t < tris.Length; t += 3)
        {
            int i0 = tris[t], i1 = tris[t + 1], i2 = tris[t + 2];
            AddEdge(edges, i0, i1, t);
            AddEdge(edges, i1, i2, t);
            AddEdge(edges, i2, i0, t);
        }
        return edges;
    }

    static void AddEdge(Dictionary<(int,int), Edge> map, int i, int j, int triIndex)
    {
        int a = Mathf.Min(i, j), b = Mathf.Max(i, j);
        var key = (a, b);
        if (map.TryGetValue(key, out var e)) { if (e.faceB == -1) { e.faceB = triIndex; map[key] = e; } }
        else map[key] = new Edge { a = a, b = b, faceA = triIndex, faceB = -1 };
    }

    static Vector3 FaceNormalAt(int triOffset, int[] tris, Vector3[] verts)
    {
        int i0 = tris[triOffset], i1 = tris[triOffset + 1], i2 = tris[triOffset + 2];
        var p0 = verts[i0]; var p1 = verts[i1]; var p2 = verts[i2];
        var n = Vector3.Cross(p1 - p0, p2 - p0);
        float m = n.magnitude;
        return m > 1e-12f ? (n / m) : Vector3.up;
    }

    static float DihedralAngleDeg(Vector3 nA, Vector3 nB)
    {
        float d = Mathf.Clamp(Vector3.Dot(nA, nB), -1f, 1f);
        return Mathf.Acos(d) * Mathf.Rad2Deg;
    }

    static bool IsAngleAllowed(float angleDeg, List<AngleRange> ranges)
    {
        if (ranges == null || ranges.Count == 0) return true;
        for (int i = 0; i < ranges.Count; i++)
        {
            float a = ranges[i].minDeg, b = ranges[i].maxDeg;
            if (a > b) (a, b) = (b, a);
            if (angleDeg >= a && angleDeg <= b) return true;
        }
        return false;
    }

    List<Edge> SelectEdgesByAngle(
        Dictionary<(int,int), Edge> edges,
        Mesh source,
        Vector3[] vertices,
        float preThresholdDeg,
        List<AngleRange> ranges,
        bool includeBorders,
        bool borderUsesAngle,
        float borderAngleDegVal)
    {
        var result = new List<Edge>();
        var tris = source.triangles;

        foreach (var kv in edges)
        {
            var e = kv.Value;

            if (e.faceB == -1)
            {
                if (!includeBorders) continue;
                if (borderUsesAngle)
                {
                    float a = Mathf.Clamp(borderAngleDegVal, 0f, 180f);
                    if (preThresholdDeg > 0f && a < preThresholdDeg) continue;
                    if (!IsAngleAllowed(a, ranges)) continue;
                }
                result.Add(e);
                continue;
            }

            Vector3 nA = FaceNormalAt(e.faceA, tris, vertices);
            Vector3 nB = FaceNormalAt(e.faceB, tris, vertices);
            float angle = DihedralAngleDeg(nA, nB);

            if (preThresholdDeg > 0f && angle < preThresholdDeg) continue;
            if (!IsAngleAllowed(angle, ranges)) continue;

            result.Add(e);
        }

        return result;
    }

    // ====== OUTLINE MESH ======

    Mesh CreateTransientMesh(string name)
    {
        var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        if (!saveAsAssets)
            m.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return m;
    }

    void BuildOutline(Mesh source)
    {
        EnsureOutlineObjects();

        // prepare mesh (transient by default)
        _outlineMesh = CreateTransientMesh("EdgeOutlineMesh");

        var verts   = source.vertices;
        var normals = source.normals;
        var tris    = source.triangles;

        if (normals == null || normals.Length != verts.Length)
        {
            source.RecalculateNormals();
            normals = source.normals;
        }

        var edgeMap = BuildEdgeMap(tris);
        var selected = SelectEdgesByAngle(edgeMap, source, verts, angleThreshold,
                                          allowedAngleRanges, includeBorderEdges,
                                          treatBorderWithAngle, borderAngleDeg);

        if (buildMode == OutlineBuildMode.Lines)
            BuildLinesMesh(selected, verts, _outlineMesh);
        else
            BuildQuadsMesh(selected, verts, normals, worldThickness, _outlineMesh);

#if UNITY_EDITOR
        // Optionally persist as asset
        if (saveAsAssets && !Application.isPlaying)
        {
            string path = EnsureFolderAndUniquePath(gameObject, outlineChildName);
            AssetDatabase.CreateAsset(_outlineMesh, path);
            AssetDatabase.SaveAssets();
        }
#endif
        _outlineMF.sharedMesh = _outlineMesh;

        if (_outlineMR == null) _outlineMR = GetOrAdd<MeshRenderer>(_outlineGO);
        if (overrideMaterial != null) _outlineMR.sharedMaterial = overrideMaterial;
    }

    static void BuildLinesMesh(List<Edge> edges, Vector3[] vertices, Mesh target)
    {
        var lineVerts = new List<Vector3>(edges.Count * 2);
        var lineIdx   = new List<int>(edges.Count * 2);

        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            int i0 = lineVerts.Count;
            lineVerts.Add(vertices[e.a]);
            lineVerts.Add(vertices[e.b]);
            lineIdx.Add(i0);
            lineIdx.Add(i0 + 1);
        }

        target.SetVertices(lineVerts);
        target.SetIndices(lineIdx.ToArray(), MeshTopology.Lines, 0, true);
        target.RecalculateBounds();
    }

    static void BuildQuadsMesh(List<Edge> edges, Vector3[] vertices, Vector3[] normals, float thickness, Mesh target)
    {
        var v = new List<Vector3>(edges.Count * 4);
        var n = new List<Vector3>(edges.Count * 4);
        var uv = new List<Vector2>(edges.Count * 4);
        var idx = new List<int>(edges.Count * 6);

        for (int k = 0; k < edges.Count; k++)
        {
            var e = edges[k];
            Vector3 p0 = vertices[e.a];
            Vector3 p1 = vertices[e.b];

            Vector3 edgeDir = (p1 - p0);
            if (edgeDir.sqrMagnitude < 1e-12f) continue;
            edgeDir.Normalize();

            Vector3 n0 = (normals != null && normals.Length > e.a) ? normals[e.a] : Vector3.up;
            Vector3 n1 = (normals != null && normals.Length > e.b) ? normals[e.b] : Vector3.up;
            Vector3 avgN = (n0 + n1).sqrMagnitude > 1e-10f ? (n0 + n1).normalized : Vector3.up;

            Vector3 widthDir = Vector3.Cross(edgeDir, avgN).normalized;
            if (widthDir.sqrMagnitude < 1e-10f)
            {
                widthDir = Vector3.Cross(edgeDir, Vector3.up).normalized;
                if (widthDir.sqrMagnitude < 1e-10f) widthDir = Vector3.right;
            }

            Vector3 w = widthDir * (thickness * 0.5f);

            int baseIndex = v.Count;

            Vector3 p0a = p0 - w;
            Vector3 p0b = p0 + w;
            Vector3 p1a = p1 - w;
            Vector3 p1b = p1 + w;

            v.Add(p0a); v.Add(p0b); v.Add(p1a); v.Add(p1b);
            n.Add(avgN); n.Add(avgN); n.Add(avgN); n.Add(avgN);
            uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.up); uv.Add(Vector2.one);

            idx.Add(baseIndex + 0); idx.Add(baseIndex + 1); idx.Add(baseIndex + 2);
            idx.Add(baseIndex + 2); idx.Add(baseIndex + 1); idx.Add(baseIndex + 3);
        }

        target.SetVertices(v);
        target.SetNormals(n);
        target.SetUVs(0, uv);
        target.SetTriangles(idx, 0, true);
        target.RecalculateBounds();
    }

    // ====== PATTERN PREFABS ======

    void BuildPattern(Mesh source)
    {
        var verts = source.vertices;
        var edgeMap = BuildEdgeMap(source.triangles);
        var selected = SelectEdgesByAngle(edgeMap, source, verts, angleThreshold,
                                          allowedAngleRanges, includeBorderEdges,
                                          treatBorderWithAngle, borderAngleDeg);

        // Parent temporaire
        var tempParent = new GameObject(patternTempParentName);
        tempParent.transform.SetParent(transform, false);

        int seqIndex = 0;
        var rand = new System.Random();

        foreach (var e in selected)
        {
            Vector3 a = verts[e.a];
            Vector3 b = verts[e.b];
            Vector3 ab = b - a;
            float len = ab.magnitude;
            if (len < 1e-6f) continue;

            Vector3 dir = ab / len;

            int count = Mathf.Max(1, Mathf.FloorToInt(len / Mathf.Max(0.0001f, patternSpacing)) + 1);
            for (int i = 0; i < count; i++)
            {
                float d = Mathf.Min(i * patternSpacing, len);
                Vector3 p = a + dir * d;

                var prefab = PickPrefab(ref seqIndex, rand);
                if (prefab == null) continue;

                var go = Instantiate(prefab, tempParent.transform);
                go.name = prefab.name;
                go.transform.localPosition = p;

                Quaternion baseRot = Quaternion.identity;
                if (alignToEdgeDirection)
                {
                    Vector3 edgeXY = new Vector3(dir.x, dir.y, 0f);
                    if (edgeXY.sqrMagnitude > 1e-10f)
                        baseRot = Quaternion.FromToRotation(Vector3.right, edgeXY.normalized);
                }

                float rotZ = UnityEngine.Random.Range(rotationZMinMax.x, rotationZMinMax.y);
                go.transform.localRotation = baseRot * Quaternion.AngleAxis(rotZ, Vector3.forward);

                if (scaleMode == ScaleMode.Uniform)
                {
                    float s = UnityEngine.Random.Range(uniformScaleMinMax.x, uniformScaleMinMax.y);
                    go.transform.localScale = new Vector3(s, s, s);
                }
                else
                {
                    float sx = UnityEngine.Random.Range(scaleMin.x, scaleMax.x);
                    float sy = UnityEngine.Random.Range(scaleMin.y, scaleMax.y);
                    float sz = UnityEngine.Random.Range(scaleMin.z, scaleMax.z);
                    go.transform.localScale = new Vector3(sx, sy, sz);
                }
            }
        }

        // Fusion
        CombineChildrenToSingle(tempParent, patternCombinedName, patternOverrideMaterial, deleteChildren: true);
    }

    GameObject PickPrefab(ref int seqIndex, System.Random rng)
    {
        if (patternPrefabs == null || patternPrefabs.Count == 0) return null;
        if (patternSelection == PatternSelection.Sequential)
        {
            var go = patternPrefabs[seqIndex % patternPrefabs.Count];
            seqIndex++;
            return go;
        }
        else
        {
            return patternPrefabs[rng.Next(patternPrefabs.Count)];
        }
    }

    void CombineChildrenToSingle(GameObject tempParent, string combinedName, Material overrideMat, bool deleteChildren)
    {
        var filters = tempParent.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0) return;

        // Prepare combined GO
        var combinedGO = new GameObject(combinedName);
        combinedGO.transform.SetParent(transform, false);
        if (markStatic) combinedGO.isStatic = true;

        var combMF = combinedGO.AddComponent<MeshFilter>();
        var combMR = combinedGO.AddComponent<MeshRenderer>();

        // Gather CombineInstances
        var combis = new List<CombineInstance>(filters.Length);
        foreach (var mf in filters)
        {
            if (mf.sharedMesh == null) continue;
            combis.Add(new CombineInstance { mesh = mf.sharedMesh, transform = mf.transform.localToWorldMatrix });
        }

        var finalMesh = CreateTransientMesh($"{name}_{combinedName}");
        finalMesh.CombineMeshes(combis.ToArray(), overrideMat != null, true, false);
#if UNITY_EDITOR
        if (saveAsAssets && !Application.isPlaying)
        {
            string path = EnsureFolderAndUniquePath(gameObject, combinedName);
            AssetDatabase.CreateAsset(finalMesh, path);
            AssetDatabase.SaveAssets();
        }
#endif
        combMF.sharedMesh = finalMesh;

        if (overrideMat != null)
        {
            combMR.sharedMaterial = overrideMat;
        }
        else
        {
            // If you want to preserve original materials per-submesh, expand logic here.
            combMR.sharedMaterial = _outlineMR != null ? _outlineMR.sharedMaterial : null;
        }

        if (deleteChildren)
        {
            if (Application.isPlaying) Destroy(tempParent); else DestroyImmediate(tempParent);
        }
        else tempParent.SetActive(false);
    }

#if UNITY_EDITOR
    string EnsureFolderAndUniquePath(GameObject owner, string suffix)
    {
        string root = "Assets";
        string folder = Path.Combine(root, assetsFolderUnderAssets).Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(folder))
        {
            string[] parts = assetsFolderUnderAssets.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
            string current = root;
            foreach (var p in parts)
            {
                string next = $"{current}/{p}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, p);
                current = next;
            }
        }
        string baseName = $"{owner.name}_{suffix}".Replace(" ", "_");
        string path = $"{folder}/{baseName}.asset";
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        return path;
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(EdgeOutlineBuilder))]
[CanEditMultipleObjects]
public class EdgeOutlineBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate", GUILayout.Height(28)))
            {
                foreach (var o in targets)
                {
                    var b = o as EdgeOutlineBuilder;
                    if (!b) continue;
                    Undo.RegisterFullObjectHierarchyUndo(b.gameObject, "Generate Edge Output");
                    b.GenerateOutline();
                    EditorUtility.SetDirty(b);
                    if (!Application.isPlaying)
                        EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Height(28)))
            {
                foreach (var o in targets)
                {
                    var b = o as EdgeOutlineBuilder;
                    if (!b) continue;
                    Undo.RegisterFullObjectHierarchyUndo(b.gameObject, "Clear Edge Output");
                    b.ClearOutline();
                    b.SendMessage("ClearPatternOutputs", SendMessageOptions.DontRequireReceiver);
                    EditorUtility.SetDirty(b);
                    if (!Application.isPlaying)
                        EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
                }
            }
        }

        EditorGUILayout.HelpBox(
            "Scene bloat fix:\n" +
            "• Leave 'Save As Assets' OFF to keep generated meshes transient (not serialized in the scene).\n" +
            "• Or turn it ON to write .asset files into Assets/Generated/Outlines.",
            MessageType.Info);
    }
}
#endif
