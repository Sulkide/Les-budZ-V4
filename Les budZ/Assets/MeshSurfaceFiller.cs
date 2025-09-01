using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class MeshSurfaceFiller : MonoBehaviour
{
    // ====== PUBLIC ENUMS ======
    public enum SelectionMode { Random, Sequential }
    public enum ClipMode { PreciseTriangulated, FastCentroidCull }
    public enum ScaleFitMode { Contain, Cover, Stretch }         // Contain=min, Cover=max, Stretch=sx/sy
    public enum GridMode { ExactFactorization, BestFitRaggedLastRow }
    public enum LastRowAlign { Left, Center, Right }
    public enum JitterMode { Random, SeqMaxToMin, SeqMinToMax }

    // ====== SOURCE ======
    [Header("Source plane (XY, Z constant)")]
    public MeshFilter sourceMeshFilter;
    public MeshRenderer sourceMeshRenderer;

    [Header("Auto-source fallback")]
    [Tooltip("Si aucun MeshFilter/MeshRenderer n'est renseigné, cherche automatiquement un enfant nommé \"Polygon\".")]
    public bool autoPickSourceWhenEmpty = true;
    [Tooltip("Nom de l'objet enfant à rechercher si auto-pick est activé.")]
    public string autoSourceChildName = "Polygon";

    // ====== PREFABS & PLACEMENT ======
    [Header("Prefabs & Placement")]
    public List<GameObject> prefabs = new List<GameObject>();
    public SelectionMode selection = SelectionMode.Random;
    [Min(1)] public int instanceCount = 30;

    [Tooltip("Contraint le jitter XY dans la cellule (évite les chevauchements).")]
    public bool constrainToCell = true;

    [Header("Cell scaling")]
    public ScaleFitMode scaleFitMode = ScaleFitMode.Cover;
    [Tooltip("Si true (pour Contain/Cover), garde un scale uniforme XY.")]
    public bool uniformScaleToCell = true;

    [Tooltip("Décalage Z global (local au mesh source).")]
    public float offsetZ = 0f;

    // ====== JITTER ======
    [Header("Random transform jitter (local)")]
    public Vector3 posJitterMin = new Vector3(-0.05f, -0.05f, 0f);
    public Vector3 posJitterMax = new Vector3( 0.05f,  0.05f, 0f);

    public JitterMode posXMode = JitterMode.Random;
    public JitterMode posYMode = JitterMode.Random;
    public JitterMode posZMode = JitterMode.Random;

    [Tooltip("Rotation locale autour de Z (degrés).")]
    public Vector2 rotZMinMax = new Vector2(-5f, 5f);
    public JitterMode rotZMode = JitterMode.Random;

    [Tooltip("Facteur multiplicatif appliqué après l’échelle de cellule.")]
    public Vector3 scaleMultMin = new Vector3(0.95f, 0.95f, 1f);
    public Vector3 scaleMultMax = new Vector3(1.05f, 1.05f, 1f);
    public JitterMode scaleMode = JitterMode.Random;

    // ====== OUTPUT / MATERIALS ======
    [Header("Output / Materials")]
    public string outputName = "MSF_Output";
    public Transform outputParent;
    public bool deleteSpawnedAfterCombine = true;
    public Material overrideMaterial;
    public bool use32BitIndices = true;

    // ====== OVERLAP & CLIPPING ======
    [Header("Overlap & Clipping")]
    [Tooltip("Retire les triangles totalement sous une instance au-dessus (approx).")]
    public bool removeOverlapExperimental = false;

    [Tooltip("Méthode de clip des bords XY du mesh source.")]
    public ClipMode clipMode = ClipMode.PreciseTriangulated;

    [Tooltip("Tolérance géométrique (en unités locales XY).")]
    [Range(1e-5f, 1e-2f)]
    public float epsilon = 1e-4f;

    [Header("Grid layout")]
    public GridMode gridMode = GridMode.ExactFactorization; // par défaut: zéro trou
    public LastRowAlign lastRowAlign = LastRowAlign.Left;

    
    [Header("Simplification Override")]
    
    
    // ====== OUTLINE BUILDER OVERRIDES ======
    //[Header("Outline (EdgeOutlineBuilder overrides)")]
    private bool generateOutlineAtEnd = false;
    [Range(0f, 180f)] private float outlineAngleThreshold = 0f;
    private List<EdgeOutlineBuilder.AngleRange> outlineAllowedAngleRanges = new List<EdgeOutlineBuilder.AngleRange>();
    private bool outlineIncludeBorderEdges = true;
    private bool outlineTreatBorderWithAngle = true;
    [Range(0f, 180f)] private float outlineBorderAngleDeg = 180f;

    private EdgeOutlineBuilder.OutlineBuildMode outlineBuildMode = EdgeOutlineBuilder.OutlineBuildMode.Quads;
    private Color outlineColor = Color.black;
    [Min(0.0001f)] private float outlineWorldThickness = 0.01f;
    private Material outlineOverrideMaterial;
    private string outlineChildName = "_Outline";
    private bool outlineMarkStatic = true;

    // ====== RUNTIME CACHES ======
    Transform _spawnParent;
    Transform SpawnParent => _spawnParent != null ? _spawnParent : (_spawnParent = GetOrCreateChild("__MSF_Spawned"));

    Transform GetOrCreateChild(string name)
    {
        var t = transform.Find(name);
        if (!t)
        {
            var go = new GameObject(name);
            go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
            go.transform.SetParent(transform, false);
            t = go.transform;
        }
        return t;
    }

    void Reset()
    {
        sourceMeshFilter = GetComponent<MeshFilter>();
        sourceMeshRenderer = GetComponent<MeshRenderer>();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // bornes scale mult
        EnsureMinMax(ref scaleMultMin, ref scaleMultMax);
        // bornes rotZ
        EnsureMinMax(ref rotZMinMax);
        // bornes jitter pos
        EnsureMinMax(ref posJitterMin, ref posJitterMax);

        if (outlineWorldThickness < 0.0001f) outlineWorldThickness = 0.0001f;

        if (!Application.isPlaying) SceneView.RepaintAll();
    }
#endif

    // ===================== PUBLIC ACTION =====================
    [ContextMenu("Generate")]
    public void Generate()
    {
        if (!ValidateInputs(out string err))
        {
            Debug.LogError($"[MeshSurfaceFiller] {err}", this);
            return;
        }

#if UNITY_EDITOR
        Undo.RegisterFullObjectHierarchyUndo(gameObject, "MeshSurfaceFiller Generate");
#endif

        // Nettoyage précédent
        ClearLastOutputInternal();

        // 1) Infos source
        var srcMF = sourceMeshFilter;
        var srcMesh = srcMF.sharedMesh;
        var srcToWorld = srcMF.transform.localToWorldMatrix;
        var worldToSrc = srcMF.transform.worldToLocalMatrix;

        Bounds2D planeBounds = ComputeMeshXYBounds(srcMesh);

        // 2) Grille
        ComputeGrid(instanceCount, planeBounds,
            out int rows, out int cols, out float cellW, out float cellH,
            out int lastRowCount, out float lastRowXOffset);

        // 3) Spawn + collecte triangles
        var instancesTris = new List<List<Tri3D>>(instanceCount);
        var rand = new System.Random(GetSeed());

        for (int i = 0; i < instanceCount; i++)
        {
            GameObject prefab = PickPrefab(i, rand);
            if (prefab == null) continue;

            // a) Instanciation (parent caché)
            var inst = InstantiateEditorSafe(prefab, SpawnParent);

            // b) Bounds locaux du prefab (dans l'espace local du SOURCE)
            Bounds2D prefabLocalBounds;
            {
                var renderers = inst.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
#if UNITY_EDITOR
                    Undo.DestroyObjectImmediate(inst);
#else
                    DestroyImmediate(inst);
#endif
                    continue;
                }
                Bounds worldB = new Bounds(renderers[0].bounds.center, Vector3.zero);
                foreach (var r in renderers) worldB.Encapsulate(r.bounds);

                var min = worldToSrc.MultiplyPoint3x4(worldB.min);
                var max = worldToSrc.MultiplyPoint3x4(worldB.max);
                prefabLocalBounds = Bounds2D.FromMinMax(
                    new Vector2(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y)),
                    new Vector2(Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y))
                );
            }

            // c) Indices de cellule + éventuel offset sur la dernière rangée
            int rIdx = i / cols;
            int cIdx = i % cols;

            bool isLastRow = (rIdx == rows - 1);
            float rowOffsetX =
                (gridMode == GridMode.BestFitRaggedLastRow && isLastRow && lastRowCount < cols)
                ? lastRowXOffset : 0f;

            Vector2 cellCenter = new Vector2(
                planeBounds.min.x + rowOffsetX + (cIdx + 0.5f) * cellW,
                planeBounds.min.y + (rIdx + 0.5f) * cellH
            );

            // d) Échelle pour remplir la cellule (Contain / Cover / Stretch)
            Vector2 baseSize = prefabLocalBounds.size;

            const float cellPad = 1.0015f; // micro marge pour éviter micro-trous
            float sx = (baseSize.x > epsilon) ? (cellW * cellPad) / baseSize.x : 1f;
            float sy = (baseSize.y > epsilon) ? (cellH * cellPad) / baseSize.y : 1f;

            Vector3 scaleToCell = Vector3.one;
            switch (scaleFitMode)
            {
                case ScaleFitMode.Contain:
                {
                    float s = Mathf.Min(sx, sy);
                    scaleToCell = uniformScaleToCell ? new Vector3(s, s, 1f) : new Vector3(s, s, 1f);
                    break;
                }
                case ScaleFitMode.Cover:
                {
                    if (uniformScaleToCell)
                    {
                        float s = Mathf.Max(sx, sy);
                        scaleToCell = new Vector3(s, s, 1f);
                    }
                    else
                    {
                        scaleToCell = new Vector3(sx, sy, 1f);
                    }
                    break;
                }
                case ScaleFitMode.Stretch:
                {
                    scaleToCell = new Vector3(sx, sy, 1f);
                    break;
                }
            }

            // e) Jitter position (modes indépendants)
            float jx = SampleByMode(posXMode, posJitterMin.x, posJitterMax.x, i, instanceCount, rand);
            float jy = SampleByMode(posYMode, posJitterMin.y, posJitterMax.y, i, instanceCount, rand);
            float jz = SampleByMode(posZMode, posJitterMin.z, posJitterMax.z, i, instanceCount, rand);

            if (constrainToCell)
            {
                float maxJx = Mathf.Max(0f, cellW * 0.5f - epsilon);
                float maxJy = Mathf.Max(0f, cellH * 0.5f - epsilon);
                jx = Mathf.Clamp(jx, -maxJx, maxJx);
                jy = Mathf.Clamp(jy, -maxJy, maxJy);
            }

            Vector3 jitter = new Vector3(jx, jy, jz);

            // f) Rotation Z (mode)
            float rotZ = SampleByMode(rotZMode, rotZMinMax.x, rotZMinMax.y, i, instanceCount, rand);

            // g) Scale multiplicatif (mode commun aux 3 axes)
            Vector3 scaleMult = new Vector3(
                SampleByMode(scaleMode, scaleMultMin.x, scaleMultMax.x, i, instanceCount, rand),
                SampleByMode(scaleMode, scaleMultMin.y, scaleMultMax.y, i, instanceCount, rand),
                SampleByMode(scaleMode, scaleMultMin.z, scaleMultMax.z, i, instanceCount, rand)
            );

            // h) TRS dans l'espace du source
            Vector3 localPos = new Vector3(cellCenter.x + jitter.x, cellCenter.y + jitter.y, offsetZ + jitter.z);
            var mLocal = Matrix4x4.TRS(localPos, Quaternion.Euler(0f, 0f, rotZ), Vector3.Scale(scaleToCell, scaleMult));
            var mWorld = srcMF.transform.localToWorldMatrix * mLocal;

            inst.transform.position = mWorld.MultiplyPoint3x4(Vector3.zero);
            inst.transform.rotation = mWorld.rotation;
            inst.transform.localScale = ExtractLossyScale(mWorld, inst.transform);

            // i) Triangles (dans l'espace local du SOURCE)
            var tris = ExtractTrianglesFromObject(inst.transform, worldToSrc);
            instancesTris.Add(tris);
        }

        // 4) Retrait recouvrements (optionnel)
        if (removeOverlapExperimental)
        {
            RemoveFullyCoveredTriangles(instancesTris);
        }

        // 5) Clip final au contour XY du mesh source
        var sourceTris2D = GetSourceTriangles2D(sourceMeshFilter.sharedMesh);
        List<Tri3D> finalTris = (clipMode == ClipMode.PreciseTriangulated)
            ? ClipAllToSourcePrecise(instancesTris, sourceTris2D)
            : ClipAllToSourceFast(instancesTris, sourceTris2D);

        // 6) Construction du mesh final
        var outGO = new GameObject(outputName);
#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(outGO, "MSF Output Created");
#endif
        var outTrans = outGO.transform;
        outTrans.SetParent(outputParent ? outputParent : transform, false);
        var outMF = outGO.AddComponent<MeshFilter>();
        var outMR = outGO.AddComponent<MeshRenderer>();

        Mesh outMesh = new Mesh();
        if (use32BitIndices) outMesh.indexFormat = IndexFormat.UInt32;
        BuildMeshFromTriangles(finalTris, outMesh);
        outMF.sharedMesh = outMesh;

        // Matériau principal
        Material mat = overrideMaterial
            ? overrideMaterial
            : (sourceMeshRenderer && sourceMeshRenderer.sharedMaterial
                ? sourceMeshRenderer.sharedMaterial
                : null);
        if (!mat) mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        outMR.sharedMaterial = mat;

        //ajout
        outGO.AddComponent<VisibleFacadeExtractor>();
        
        // 7) EDGE OUTLINE (optionnel)
        if (generateOutlineAtEnd)
        {
            EdgeOutlineBuilder eob = outGO.GetComponent<EdgeOutlineBuilder>();
#if UNITY_EDITOR
            if (!eob) eob = Undo.AddComponent<EdgeOutlineBuilder>(outGO);
#else
            if (!eob) eob = outGO.AddComponent<EdgeOutlineBuilder>();
#endif
            eob.sourceMeshFilter = outMF;

            // Selection
            eob.angleThreshold = outlineAngleThreshold;
            eob.allowedAngleRanges = (outlineAllowedAngleRanges != null)
                ? new List<EdgeOutlineBuilder.AngleRange>(outlineAllowedAngleRanges)
                : new List<EdgeOutlineBuilder.AngleRange>();
            eob.includeBorderEdges = outlineIncludeBorderEdges;
            eob.treatBorderWithAngle = outlineTreatBorderWithAngle;
            eob.borderAngleDeg = outlineBorderAngleDeg;

            // Rendering
            eob.buildMode = outlineBuildMode;
            eob.outlineColor = outlineColor;
            eob.worldThickness = outlineWorldThickness;
            eob.overrideMaterial = outlineOverrideMaterial;

            // Output
            eob.outlineChildName = outlineChildName;
            eob.markStatic = outlineMarkStatic;

            eob.GenerateOutline();

#if UNITY_EDITOR
            EditorUtility.SetDirty(eob);
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(outGO.scene);
#endif
        }

        // 8) Nettoyage
        if (deleteSpawnedAfterCombine)
        {
#if UNITY_EDITOR
            if (SpawnParent) Undo.DestroyObjectImmediate(SpawnParent.gameObject);
#else
            if (SpawnParent) DestroyImmediate(SpawnParent.gameObject);
#endif
            _spawnParent = null;
        }

        Debug.Log($"[MeshSurfaceFiller] Done. tris={outMesh.triangles.Length/3}", this);
    }

    [ContextMenu("Clear Last Output")]
    public void ClearLastOutput()
    {
        ClearLastOutputInternal();
    }

    void ClearLastOutputInternal()
    {
        // supprime output existant
        var t = (outputParent ? outputParent : transform);
        var child = t.Find(outputName);
        if (child)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(child.gameObject);
#else
            DestroyImmediate(child.gameObject);
#endif
        }
        if (_spawnParent && _spawnParent)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(_spawnParent.gameObject);
#else
            DestroyImmediate(_spawnParent.gameObject);
#endif
            _spawnParent = null;
        }
    }

    // ===================== HELPERS =====================

    bool ValidateInputs(out string error)
    {
        // Auto-pick si vide
        if (autoPickSourceWhenEmpty &&
            (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null || sourceMeshRenderer == null))
        {
            if (TryAutoAssignSourceFromChildrenByName(autoSourceChildName))
            {
                Debug.Log($"[MeshSurfaceFiller] Source auto-assignée depuis \"{autoSourceChildName}\".", this);
            }
        }

        // Compléter renderer si seul le MF est trouvé
        if (sourceMeshRenderer == null && sourceMeshFilter != null)
            sourceMeshRenderer = sourceMeshFilter.GetComponent<MeshRenderer>();

        if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
        {
            error = $"Aucun MeshFilter source valide. Active l'auto-pick ou assigne un MeshFilter (enfant \"{autoSourceChildName}\").";
            return false;
        }
        if (sourceMeshRenderer == null)
        {
            error = "MeshRenderer source manquant (sur le même objet que le MeshFilter).";
            return false;
        }
        if (prefabs == null || prefabs.Count == 0 || prefabs.All(p => p == null))
        {
            error = "La liste de prefabs est vide.";
            return false;
        }

        error = null;
        return true;
    }

    bool TryAutoAssignSourceFromChildrenByName(string childName)
    {
        if (string.IsNullOrEmpty(childName)) return false;

        var all = GetComponentsInChildren<Transform>(true);
        Transform found = null;
        foreach (var t in all)
        {
            if (t == transform) continue;
            if (t.name == childName) { found = t; break; }
        }
        if (!found) return false;

        var mf = found.GetComponent<MeshFilter>() ?? found.GetComponentInChildren<MeshFilter>(true);
        var mr = found.GetComponent<MeshRenderer>() ?? found.GetComponentInChildren<MeshRenderer>(true);

        if (mf != null && mr != null && mf.sharedMesh != null)
        {
            sourceMeshFilter = mf;
            sourceMeshRenderer = mr;
            return true;
        }
        return false;
    }

    static Bounds2D ComputeMeshXYBounds(Mesh m)
    {
        var v = m.vertices;
        if (v == null || v.Length == 0) return new Bounds2D(Vector2.zero, Vector2.zero);
        Vector2 min = new Vector2(v[0].x, v[0].y);
        Vector2 max = min;
        for (int i = 1; i < v.Length; i++)
        {
            Vector2 p = new Vector2(v[i].x, v[i].y);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        return Bounds2D.FromMinMax(min, max);
    }

    void ComputeGrid(
        int count, Bounds2D b,
        out int rows, out int cols, out float cellW, out float cellH,
        out int lastRowCount, out float lastRowXOffset)
    {
        // Valeurs par défaut
        rows = 1; cols = count; cellW = 0f; cellH = 0f;
        lastRowCount = 0; lastRowXOffset = 0f;

        float w = Mathf.Max(b.size.x, 1e-6f);
        float h = Mathf.Max(b.size.y, 1e-6f);
        float aspect = w / h;

        if (gridMode == GridMode.ExactFactorization)
        {
            int bestR = 1, bestC = count;
            float bestScore = float.PositiveInfinity;

            for (int r = 1; r <= count; r++)
            {
                if (count % r != 0) continue;
                int c = count / r;
                float score = Mathf.Abs(((float)c / r) - aspect);
                if (score < bestScore) { bestScore = score; bestR = r; bestC = c; }
            }

            rows = bestR; cols = bestC;
            cellW = w / cols; cellH = h / rows;

            lastRowCount = cols;
            lastRowXOffset = 0f;
            return;
        }

        // BestFitRaggedLastRow
        rows = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(count / aspect)));
        cols = Mathf.CeilToInt((float)count / rows);

        cellW = w / cols;
        cellH = h / rows;

        lastRowCount = count - (rows - 1) * cols;
        if (lastRowCount <= 0) lastRowCount = cols;

        int empty = cols - lastRowCount;
        float emptyWidth = empty * cellW;

        switch (lastRowAlign)
        {
            case LastRowAlign.Left:   lastRowXOffset = 0f; break;
            case LastRowAlign.Center: lastRowXOffset = emptyWidth * 0.5f; break;
            case LastRowAlign.Right:  lastRowXOffset = emptyWidth; break;
            default:                  lastRowXOffset = 0f; break;
        }
    }

    GameObject PickPrefab(int i, System.Random rnd)
    {
        if (prefabs.Count == 0) return null;
        if (selection == SelectionMode.Sequential)
        {
            return prefabs[i % prefabs.Count];
        }
        else
        {
            var candidates = prefabs.Where(p => p != null).ToList();
            if (candidates.Count == 0) return null;
            return candidates[rnd.Next(candidates.Count)];
        }
    }

    static int GetSeed() => UnityEngine.Random.Range(int.MinValue, int.MaxValue);

    static float RandRange(System.Random r, float a, float b)
    {
        double t = r.NextDouble();
        return (float)(a + (b - a) * t);
    }

    static float SampleByMode(JitterMode mode, float min, float max, int index, int total, System.Random rnd)
    {
        switch (mode)
        {
            case JitterMode.Random:
                return RandRange(rnd, min, max);
            case JitterMode.SeqMaxToMin:
            {
                if (total <= 1) return max;
                float t = (float)index / (total - 1);
                return Mathf.Lerp(max, min, t);
            }
            case JitterMode.SeqMinToMax:
            {
                if (total <= 1) return min;
                float t = (float)index / (total - 1);
                return Mathf.Lerp(min, max, t);
            }
        }
        return RandRange(rnd, min, max);
    }

    static Vector3 ExtractLossyScale(Matrix4x4 trs, Transform target)
    {
        Vector3 sx = new Vector3(trs.m00, trs.m01, trs.m02);
        Vector3 sy = new Vector3(trs.m10, trs.m11, trs.m12);
        Vector3 sz = new Vector3(trs.m20, trs.m21, trs.m22);
        Vector3 lossy = new Vector3(sx.magnitude, sy.magnitude, sz.magnitude);
        return lossy;
    }

    // ------- Triangles & Mesh build -------
    struct Vtx
    {
        public Vector3 pos;   // espace local du SOURCE
        public Vector3 nrm;
        public Vector2 uv;
    }

    struct Tri3D
    {
        public Vtx a, b, c;
        public int layer;
    }

    struct Tri2D
    {
        public Vector2 a, b, c;
        public int layer;
    }

    List<Tri3D> ExtractTrianglesFromObject(Transform root, Matrix4x4 worldToSource)
    {
        var list = new List<Tri3D>(256);
        var filters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in filters)
        {
            var mesh = mf.sharedMesh;
            if (!mesh) continue;
            var tx = worldToSource * mf.transform.localToWorldMatrix;

            var verts = mesh.vertices;
            var nrms  = mesh.normals;
            var uvs   = mesh.uv != null && mesh.uv.Length == verts.Length ? mesh.uv : new Vector2[verts.Length];
            var tris  = mesh.triangles;

            bool hasNrm = nrms != null && nrms.Length == verts.Length;

            for (int i = 0; i < tris.Length; i += 3)
            {
                int i0 = tris[i], i1 = tris[i + 1], i2 = tris[i + 2];
                Vector3 p0 = tx.MultiplyPoint3x4(verts[i0]);
                Vector3 p1 = tx.MultiplyPoint3x4(verts[i1]);
                Vector3 p2 = tx.MultiplyPoint3x4(verts[i2]);

                Vector3 n0 = hasNrm ? (tx.MultiplyVector(nrms[i0])).normalized : Vector3.forward;
                Vector3 n1 = hasNrm ? (tx.MultiplyVector(nrms[i1])).normalized : Vector3.forward;
                Vector3 n2 = hasNrm ? (tx.MultiplyVector(nrms[i2])).normalized : Vector3.forward;

                list.Add(new Tri3D
                {
                    a = new Vtx { pos = p0, nrm = n0, uv = uvs[i0] },
                    b = new Vtx { pos = p1, nrm = n1, uv = uvs[i1] },
                    c = new Vtx { pos = p2, nrm = n2, uv = uvs[i2] },
                    layer = 0
                });
            }
        }
        return list;
    }

    void RemoveFullyCoveredTriangles(List<List<Tri3D>> perInstance)
    {
        for (int i = 0; i < perInstance.Count; i++)
        {
            var li = perInstance[i];
            for (int t = 0; t < li.Count; t++)
            {
                var tri = li[t];
                tri.layer = i;
                li[t] = tri;
            }
        }

        for (int i = 0; i < perInstance.Count; i++)
        {
            var lower = perInstance[i];
            for (int t = lower.Count - 1; t >= 0; t--)
            {
                var triL = lower[t];
                Vector2 c = new Vector2((triL.a.pos.x + triL.b.pos.x + triL.c.pos.x) / 3f,
                                        (triL.a.pos.y + triL.b.pos.y + triL.c.pos.y) / 3f);

                bool covered = false;
                for (int j = i + 1; j < perInstance.Count && !covered; j++)
                {
                    foreach (var triU in perInstance[j])
                    {
                        if (PointInTri2D(c, triU)) { covered = true; break; }
                    }
                }
                if (covered) lower.RemoveAt(t);
            }
        }
    }

    static bool PointInTri2D(Vector2 p, Tri3D tri)
    {
        Vector2 a = new Vector2(tri.a.pos.x, tri.a.pos.y);
        Vector2 b = new Vector2(tri.b.pos.x, tri.b.pos.y);
        Vector2 c = new Vector2(tri.c.pos.x, tri.c.pos.y);
        float s = Cross(b - a, p - a);
        float t = Cross(c - b, p - b);
        float u = Cross(a - c, p - c);
        bool hasNeg = (s < 0) || (t < 0) || (u < 0);
        bool hasPos = (s > 0) || (t > 0) || (u > 0);
        return !(hasNeg && hasPos);
    }

    static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;

    static List<Tri2D> GetSourceTriangles2D(Mesh m)
    {
        var res = new List<Tri2D>(m.triangles.Length / 3);
        var v = m.vertices;
        var tri = m.triangles;
        for (int i = 0; i < tri.Length; i += 3)
        {
            Vector2 a = new Vector2(v[tri[i]].x,     v[tri[i]].y);
            Vector2 b = new Vector2(v[tri[i + 1]].x, v[tri[i + 1]].y);
            Vector2 c = new Vector2(v[tri[i + 2]].x, v[tri[i + 2]].y);
            res.Add(new Tri2D { a = a, b = b, c = c, layer = 0 });
        }
        return res;
    }

    List<Tri3D> ClipAllToSourceFast(List<List<Tri3D>> perInstance, List<Tri2D> sourceTris)
    {
        var outList = new List<Tri3D>(perInstance.Sum(l => l.Count));
        foreach (var li in perInstance)
        {
            foreach (var t in li)
            {
                Vector2 c = new Vector2((t.a.pos.x + t.b.pos.x + t.c.pos.x) / 3f,
                                        (t.a.pos.y + t.b.pos.y + t.c.pos.y) / 3f);
                if (PointInSource(c, sourceTris))
                    outList.Add(t);
            }
        }
        return outList;
    }

    static bool PointInSource(Vector2 p, List<Tri2D> srcTris)
    {
        for (int i = 0; i < srcTris.Count; i++)
        {
            var T = srcTris[i];
            float s = Cross(T.b - T.a, p - T.a);
            float t = Cross(T.c - T.b, p - T.b);
            float u = Cross(T.a - T.c, p - T.c);
            bool hasNeg = (s < 0) || (t < 0) || (u < 0);
            bool hasPos = (s > 0) || (t > 0) || (u > 0);
            if (!(hasNeg && hasPos)) return true;
        }
        return false;
    }

    List<Tri3D> ClipAllToSourcePrecise(List<List<Tri3D>> perInstance, List<Tri2D> sourceTris)
    {
        var result = new List<Tri3D>(perInstance.Sum(l => l.Count));
        foreach (var li in perInstance)
        {
            foreach (var tri in li)
            {
                var poly = new List<Vtx>(3) { tri.a, tri.b, tri.c };
                var poly2 = new List<Vtx>(8);

                var accumForThisTri = new List<List<Vtx>>(4);

                foreach (var clipTri in sourceTris)
                {
                    poly2.Clear();
                    if (ClipPolygonAgainstTriangle(poly, clipTri, ref poly2, epsilon))
                    {
                        if (poly2.Count >= 3)
                            accumForThisTri.Add(new List<Vtx>(poly2));
                    }
                }

                foreach (var piece in accumForThisTri)
                {
                    TriangulateFan(piece, tri.layer, result);
                }
            }
        }
        return result;
    }

    static bool ClipPolygonAgainstTriangle(List<Vtx> polyIn, Tri2D tri, ref List<Vtx> polyOut, float eps)
    {
        if (polyIn.Count < 3) return false;

        Vector2 ta = tri.a, tb = tri.b, tc = tri.c;
        if (Cross(tb - ta, tc - ta) < 0)
        {
            var tmp = tb; tb = tc; tc = tmp;
        }

        List<Vtx> current = new List<Vtx>(polyIn);
        List<Vtx> buffer = new List<Vtx>(polyIn.Count + 8);

        Vector2[] E0 = { ta, tb, tc };
        Vector2[] E1 = { tb, tc, ta };

        for (int e = 0; e < 3; e++)
        {
            buffer.Clear();
            if (!ClipAgainstHalfPlane(current, E0[e], E1[e], true, buffer, eps))
            {
                polyOut.Clear();
                return false; // tout dehors
            }
            current.Clear();
            current.AddRange(buffer);
        }

        polyOut.Clear();
        polyOut.AddRange(current);
        return polyOut.Count >= 3;
    }

    static bool ClipAgainstHalfPlane(List<Vtx> poly, Vector2 A, Vector2 B, bool insideIsLeft, List<Vtx> output, float eps)
    {
        if (poly.Count == 0) return false;
        Vtx S = poly[poly.Count - 1];
        for (int i = 0; i < poly.Count; i++)
        {
            Vtx E = poly[i];
            Vector2 s2 = new Vector2(S.pos.x, S.pos.y);
            Vector2 e2 = new Vector2(E.pos.x, E.pos.y);

            bool Ein = IsInsideHalfPlane(e2, A, B, insideIsLeft, eps);
            bool Sin = IsInsideHalfPlane(s2, A, B, insideIsLeft, eps);

            if (Ein)
            {
                if (!Sin)
                {
                    if (IntersectSegments2D(s2, e2, A, B, out Vector2 I, out float t))
                        output.Add(Interpolate(S, E, t, I));
                }
                output.Add(E);
            }
            else if (Sin)
            {
                if (IntersectSegments2D(s2, e2, A, B, out Vector2 I, out float t))
                    output.Add(Interpolate(S, E, t, I));
            }
            S = E;
        }
        return output.Count > 0;
    }

    static bool IsInsideHalfPlane(Vector2 P, Vector2 A, Vector2 B, bool insideIsLeft, float eps)
    {
        float side = Cross(B - A, P - A);
        return insideIsLeft ? side >= -eps : side <= eps;
    }

    static bool IntersectSegments2D(Vector2 P, Vector2 Q, Vector2 A, Vector2 B, out Vector2 I, out float tPQ)
    {
        I = Vector2.zero; tPQ = 0f;
        Vector2 r = Q - P;
        Vector2 s = B - A;
        float rxs = Cross(r, s);
        if (Mathf.Abs(rxs) < 1e-9f) return false; // parallèles
        float t = Cross((A - P), s) / rxs;
        I = P + t * r;
        tPQ = Mathf.Clamp01(t);
        return true;
    }

    static Vtx Interpolate(Vtx S, Vtx E, float t, Vector2 pos2DOverride)
    {
        t = Mathf.Clamp01(t);
        return new Vtx
        {
            pos = new Vector3(pos2DOverride.x, pos2DOverride.y, Mathf.Lerp(S.pos.z, E.pos.z, t)),
            nrm = Vector3.Lerp(S.nrm, E.nrm, t).normalized,
            uv  = Vector2.Lerp(S.uv,  E.uv,  t)
        };
    }

    static void TriangulateFan(List<Vtx> poly, int layer, List<Tri3D> outList)
    {
        if (poly.Count < 3) return;
        var v0 = poly[0];
        for (int i = 1; i < poly.Count - 1; i++)
        {
            outList.Add(new Tri3D
            {
                a = v0,
                b = poly[i],
                c = poly[i + 1],
                layer = layer
            });
        }
    }

    static void BuildMeshFromTriangles(List<Tri3D> tris, Mesh m)
    {
        int n = tris.Count;
        var verts = new Vector3[n * 3];
        var nrms  = new Vector3[n * 3];
        var uvs   = new Vector2[n * 3];
        var idx   = new int[n * 3];

        for (int i = 0; i < n; i++)
        {
            int b = i * 3;
            verts[b + 0] = tris[i].a.pos;
            verts[b + 1] = tris[i].b.pos;
            verts[b + 2] = tris[i].c.pos;

            nrms[b + 0]  = tris[i].a.nrm;
            nrms[b + 1]  = tris[i].b.nrm;
            nrms[b + 2]  = tris[i].c.nrm;

            uvs[b + 0]   = tris[i].a.uv;
            uvs[b + 1]   = tris[i].b.uv;
            uvs[b + 2]   = tris[i].c.uv;

            idx[b + 0] = b + 0;
            idx[b + 1] = b + 1;
            idx[b + 2] = b + 2;
        }

        m.Clear();
        m.vertices = verts;
        m.normals  = nrms;
        m.uv       = uvs;
        m.triangles = idx;
        m.RecalculateBounds();
    }

    static GameObject InstantiateEditorSafe(GameObject prefab, Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (!obj) obj = GameObject.Instantiate(prefab, parent);
            return obj;
        }
#endif
        return GameObject.Instantiate(prefab, parent);
    }

    // ----- Struct Bounds2D -----
    struct Bounds2D
    {
        public Vector2 center;
        public Vector2 size;
        public Vector2 min => center - size * 0.5f;
        public Vector2 max => center + size * 0.5f;

        public Bounds2D(Vector2 c, Vector2 s) { center = c; size = s; }
        public static Bounds2D FromMinMax(Vector2 mi, Vector2 ma)
        {
            return new Bounds2D((mi + ma) * 0.5f, (ma - mi));
        }
    }

    // ===== EnsureMinMax helpers =====
    static void EnsureMinMax(ref Vector3 min, ref Vector3 max)
    {
        if (min.x > max.x) { float t = min.x; min.x = max.x; max.x = t; }
        if (min.y > max.y) { float t = min.y; min.y = max.y; max.y = t; }
        if (min.z > max.z) { float t = min.z; min.z = max.z; max.z = t; }
    }
    static void EnsureMinMax(ref Vector2 minmax)
    {
        if (minmax.x > minmax.y) { float t = minmax.x; minmax.x = minmax.y; minmax.y = t; }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MeshSurfaceFiller))]
public class MeshSurfaceFillerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var t = (MeshSurfaceFiller)target;
        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!t || !t.enabled))
        {
            if (GUILayout.Button("Generate", GUILayout.Height(32)))
            {
                t.Generate();
                MarkDirty(t);
            }
        }
        if (GUILayout.Button("Clear Last Output"))
        {
            t.ClearLastOutput();
            MarkDirty(t);
        }
    }

    static void MarkDirty(UnityEngine.Object o)
    {
        EditorUtility.SetDirty(o);
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
#endif
