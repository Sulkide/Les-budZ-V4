using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Construit un mesh "façade" : conserve uniquement les triangles visibles depuis une direction.
/// Deux modes:
///  - Perspective Rays : rayons partent de la position caméra.
///  - Directional Sweep (par défaut): la caméra ne donne QUE la direction; balayage parallèle sur tout le mesh.
/// Sortie en enfant ou au niveau du parent; détruit l'objet générateur à la fin (configurable).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisibleFacadeExtractor : MonoBehaviour
{
    [Header("Référence")]
    public Camera targetCamera; // Si null, Camera.main

    [Header("Mode de visibilité")]
    [Tooltip("Balaye tout le mesh avec des rayons parallèles à la direction de la caméra (recommandé).")]
    public bool useDirectionalSweep = true;
    [Tooltip("Marge ajoutée à la longueur de balayage (m).")]
    [Min(0f)] public float sweepPadding = 0.05f;

    [Header("Filtrage")]
    [Tooltip("Retire les triangles dos-direction (normale orientée à l'opposé de la caméra).")]
    public bool removeBackFaces = true;
    [Tooltip("Retire les triangles occlus par d'autres triangles du même mesh (ou de la scène).")]
    public bool removeOccluded = true;
    [Tooltip("Néglige les triangles au-delà de cette distance depuis la caméra (0 = illimité). N'agit qu'en mode 'Perspective Rays'.")]
    public float maxViewDistance = 0f;

    [Header("Échantillonnage visibilité")]
    [Tooltip("Nombre de points testés par triangle.")]
    [Min(1)] public int samplesPerTriangle = 24;
    [Tooltip("Décalage anti auto-intersection (m).")]
    [Min(0f)] public float rayBias = 0.0005f;

    [Header("Occlusion (Raycast)")]
    [Tooltip("Si coché, ne tient compte que du mesh courant (ignore la scène).")]
    public bool selfOcclusionOnly = true;
    [Tooltip("Masque utilisé pour les raycasts (ignoré si 'Self Occlusion Only').")]
    public LayerMask occlusionMask = ~0;

    [Header("Sortie")]
    public string facadeName = "_Facade Background";
    [Tooltip("Placer la façade au même niveau que l'objet générateur (parent = parent du générateur).")]
    public bool outputAsSibling = true;
    [Tooltip("Copier la layer & flags statiques de la source sur la sortie.")]
    public bool copyLayerAndStaticFlags = true;

    [Tooltip("Recalcule les normales du mesh de sortie.")]
    public bool recalcNormals = true;
    [Tooltip("Recalcule les tangentes si disponibles et si les normales sont recalculées.")]
    public bool recalcTangents = false;
    [Tooltip("Supprime les sous-meshes vides et filtre le tableau de matériaux en conséquence.")]
    public bool compactSubmeshes = true;

    [Header("Fin de génération")]
    [Tooltip("Détruire entièrement l'objet générateur à la fin (préservant la façade).")]
    public bool destroyOriginalGameObject = true; // <- activé par défaut

    public void GenerateFacade() => GenerateImpl();

#if UNITY_EDITOR
    void Reset()
    {
        targetCamera = Camera.main;
        occlusionMask = 1 << gameObject.layer;
        useDirectionalSweep = true;
        samplesPerTriangle = 24;
        outputAsSibling = true;
        destroyOriginalGameObject = true;
    }
#endif

    void GenerateImpl()
    {
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        if (!mf || !mr || !mf.sharedMesh)
        {
            Debug.LogError("[VisibleFacadeExtractor] MeshFilter/MeshRenderer manquants ou mesh nul.");
            return;
        }

        var mesh = mf.sharedMesh;
        if (mesh.vertexCount == 0)
        {
            Debug.LogWarning("[VisibleFacadeExtractor] Mesh vide.");
            return;
        }

        var cam = targetCamera ? targetCamera : Camera.main;
        if (!cam)
        {
            Debug.LogError("[VisibleFacadeExtractor] Aucune caméra disponible.");
            return;
        }

        // MeshCollider temporaire (pour triangleIndex fiable)
        bool addedCollider = false;
        var mc = GetComponent<MeshCollider>();
        if (!mc)
        {
            mc = gameObject.AddComponent<MeshCollider>();
            addedCollider = true;
        }

        var prevConvex = mc.convex;
        var prevSharedMesh = mc.sharedMesh;
#if UNITY_2020_2_OR_NEWER
        var prevCooking = mc.cookingOptions;
#endif
        mc.sharedMesh = mesh;
        mc.convex = false;
#if UNITY_2020_2_OR_NEWER
        mc.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning | MeshColliderCookingOptions.UseFastMidphase;
#endif

        // Attributs source
        var verts    = mesh.vertices;
        var normals  = mesh.normals;
        var uvs      = mesh.uv;
        var uvs2     = mesh.uv2;
        var colors   = mesh.colors;
        var tangents = mesh.tangents;

        var l2w   = transform.localToWorldMatrix;
        Vector3 camPos = cam.transform.position;
        Vector3 dir    = cam.transform.forward.normalized; // direction uniquement

        // Triangles par sous-mesh
        int subCount = mesh.subMeshCount;
        var submeshTris = new List<int[]>(subCount);
        for (int s = 0; s < subCount; s++) submeshTris.Add(mesh.GetTriangles(s));

        // Offset global par sous-mesh (concaténation)
        var submeshBaseTri = new int[subCount];
        int totalTris = 0;
        for (int s = 0; s < subCount; s++)
        {
            submeshBaseTri[s] = totalTris;
            totalTris += submeshTris[s].Length / 3;
        }

        // Préparation sortie & remap
        var outIndicesPerSub = new List<List<int>>(subCount);
        for (int s = 0; s < subCount; s++) outIndicesPerSub.Add(new List<int>(1024));

        var mapOldToNew = new Dictionary<int, int>(verts.Length);
        var newVerts    = new List<Vector3>(verts.Length);
        var newNormals  = (normals  != null && normals.Length  == verts.Length) ? new List<Vector3>(verts.Length) : null;
        var newTangents = (tangents != null && tangents.Length == verts.Length) ? new List<Vector4>(verts.Length) : null;
        var newUV       = (uvs      != null && uvs.Length      == verts.Length) ? new List<Vector2>(verts.Length) : null;
        var newUV2      = (uvs2     != null && uvs2.Length     == verts.Length) ? new List<Vector2>(verts.Length) : null;
        var newColors   = (colors   != null && colors.Length   == verts.Length) ? new List<Color>(verts.Length)   : null;

        var barySamples = BuildBarySamples(samplesPerTriangle);
        int usedMask    = selfOcclusionOnly ? (1 << gameObject.layer) : occlusionMask.value;

        // Directional Sweep : longueur de balayage
        float sweepLength = 0f;
        if (useDirectionalSweep)
        {
            float minDot = float.PositiveInfinity;
            float maxDot = float.NegativeInfinity;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 w = l2w.MultiplyPoint3x4(verts[i]);
                float dDot = Vector3.Dot(w, dir);
                if (dDot < minDot) minDot = dDot;
                if (dDot > maxDot) maxDot = dDot;
            }
            sweepLength = Mathf.Max(0.0001f, (maxDot - minDot) + Mathf.Max(sweepPadding, rayBias * 4f));
        }

        // Boucle triangles
        for (int s = 0; s < subCount; s++)
        {
            var tris = submeshTris[s];
            for (int t = 0; t < tris.Length; t += 3)
            {
                int i0 = tris[t + 0];
                int i1 = tris[t + 1];
                int i2 = tris[t + 2];

                Vector3 w0 = l2w.MultiplyPoint3x4(verts[i0]);
                Vector3 w1 = l2w.MultiplyPoint3x4(verts[i1]);
                Vector3 w2 = l2w.MultiplyPoint3x4(verts[i2]);

                if (!useDirectionalSweep && maxViewDistance > 0f)
                {
                    float d = (Vector3.Distance(camPos, w0) + Vector3.Distance(camPos, w1) + Vector3.Distance(camPos, w2)) / 3f;
                    if (d > maxViewDistance) continue;
                }

                // Backface culling par rapport à la DIRECTION
                if (removeBackFaces)
                {
                    Vector3 faceN = Vector3.Normalize(Vector3.Cross(w1 - w0, w2 - w0));
                    if (Vector3.Dot(faceN, dir) >= 0f)
                        continue;
                }

                bool visible = true;

                if (removeOccluded)
                {
                    visible = false;
                    int localTriIndex     = (t / 3);
                    int expectedGlobalTri = submeshBaseTri[s] + localTriIndex;

                    for (int k = 0; k < barySamples.Count; k++)
                    {
                        Vector3 bc = barySamples[k];
                        Vector3 ws = w0 * bc.x + w1 * bc.y + w2 * bc.z;

                        Vector3 origin, castDir;
                        float maxDist;

                        if (useDirectionalSweep)
                        {
                            castDir = dir;
                            origin  = ws - castDir * (sweepLength + rayBias);
                            maxDist = sweepLength + (rayBias * 2f);
                        }
                        else
                        {
                            Vector3 to = (ws - camPos);
                            float dist = to.magnitude;
                            if (dist <= 1e-6f) { visible = true; break; }
                            castDir = to / dist;
                            origin  = camPos + castDir * rayBias;
                            maxDist = dist - rayBias * 0.5f;
                        }

                        if (Physics.Raycast(origin, castDir, out RaycastHit hit, maxDist, usedMask, QueryTriggerInteraction.Ignore))
                        {
                            if (hit.collider == mc && hit.triangleIndex == expectedGlobalTri)
                            {
                                visible = true;
                                break;
                            }
                        }
                        else
                        {
                            visible = true;
                            break;
                        }
                    }
                }

                if (!visible) continue;

                int ni0 = GetOrAddRemap(i0, mapOldToNew, newVerts, newNormals, newTangents, newUV, newUV2, newColors, verts, normals, tangents, uvs, uvs2, colors);
                int ni1 = GetOrAddRemap(i1, mapOldToNew, newVerts, newNormals, newTangents, newUV, newUV2, newColors, verts, normals, tangents, uvs, uvs2, colors);
                int ni2 = GetOrAddRemap(i2, mapOldToNew, newVerts, newNormals, newTangents, newUV, newUV2, newColors, verts, normals, tangents, uvs, uvs2, colors);

                outIndicesPerSub[s].Add(ni0);
                outIndicesPerSub[s].Add(ni1);
                outIndicesPerSub[s].Add(ni2);
            }
        }

        // Nettoyage MeshCollider temporaire
        if (mc)
        {
            mc.sharedMesh = prevSharedMesh;
            mc.convex = prevConvex;
#if UNITY_2020_2_OR_NEWER
            mc.cookingOptions = prevCooking;
#endif
            if (addedCollider)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(mc);
                else Destroy(mc);
#else
                DestroyImmediate(mc);
#endif
            }
        }

        // Bilan
        int keptTris = 0;
        for (int s = 0; s < outIndicesPerSub.Count; s++) keptTris += outIndicesPerSub[s].Count;
        if (keptTris == 0)
        {
            Debug.LogWarning("[VisibleFacadeExtractor] Aucun triangle retenu (direction / occlusion ?).");
            return;
        }

        // Construction mesh sortie
        var outGO = PrepareOutput(facadeName, mr, outputAsSibling, this);
        var outMF = outGO.GetComponent<MeshFilter>();
        var outMR = outGO.GetComponent<MeshRenderer>();
        outGO.AddComponent<EdgeOutlineBuilder>();
        var outMesh = new Mesh { name = mesh.name + "_Facade" };
        if (newVerts.Count > 65535)
            outMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        outMesh.SetVertices(newVerts);

        // Compactage sous-meshes et matériaux
        var finalSubIndices = new List<int[]>(outIndicesPerSub.Count);
        var finalMaterials  = new List<Material>(outIndicesPerSub.Count);
        var srcMats = mr.sharedMaterials;

        for (int s = 0; s < outIndicesPerSub.Count; s++)
        {
            var list = outIndicesPerSub[s];
            if (list.Count == 0 && compactSubmeshes) continue;
            finalSubIndices.Add(list.ToArray());
            int matIdx = Mathf.Clamp(s, 0, Mathf.Max(0, srcMats.Length - 1));
            finalMaterials.Add(srcMats.Length > 0 ? srcMats[matIdx] : null);
        }
        if (finalSubIndices.Count == 0)
        {
            finalSubIndices.Add(outIndicesPerSub[0].ToArray());
            int matIdx = Mathf.Clamp(0, 0, Mathf.Max(0, mr.sharedMaterials.Length - 1));
            finalMaterials.Add(mr.sharedMaterials.Length > 0 ? mr.sharedMaterials[matIdx] : null);
        }

        outMesh.subMeshCount = finalSubIndices.Count;
        for (int s = 0; s < finalSubIndices.Count; s++)
            outMesh.SetTriangles(finalSubIndices[s], s, true);

        if (newUV  != null) outMesh.SetUVs(0, newUV);
        if (newUV2 != null) outMesh.SetUVs(1, newUV2);

        if (!recalcNormals && newNormals != null) outMesh.SetNormals(newNormals);
        else outMesh.RecalculateNormals();

        if ((recalcTangents || (newTangents != null && recalcNormals)) && outMesh.vertexCount > 0)
            outMesh.RecalculateTangents();
        else if (newTangents != null)
            outMesh.SetTangents(newTangents);

        if (newColors != null) outMesh.SetColors(newColors);

        outMesh.RecalculateBounds();

        outMF.sharedMesh      = outMesh;
        outMR.sharedMaterials = finalMaterials.ToArray();

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(outGO, "Create Facade Mesh");
        Undo.RecordObject(outMF, "Assign Facade Mesh");
        Undo.RecordObject(outMR, "Assign Facade Materials");
        EditorUtility.SetDirty(outMF);
        EditorUtility.SetDirty(outMR);
        EditorUtility.SetDirty(gameObject);
#endif

        // --- Préserver la façade si elle est encore enfant, avant destruction du générateur ---
        if (destroyOriginalGameObject)
        {
            // Si la façade a été créée en "child" par erreur, on la remonte au parent pour la préserver
            if (outGO.transform.parent == transform)
            {
                var parent = transform.parent;
                var wpos = outGO.transform.position;
                var wrot = outGO.transform.rotation;
                var wsca = outGO.transform.lossyScale;

                outGO.transform.SetParent(parent, true); // conserve le monde
                outGO.transform.position = wpos;
                outGO.transform.rotation = wrot;
                // Pour la scale, SetParent(true) garde déjà la worldScale; pas besoin de recalc.
            }

            // Et on détruit le générateur (après avoir tout assigné).
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(gameObject);
                return;
            }
#endif
            // En Play, on détruit en fin de frame (sûr)
            Destroy(gameObject);
            return;
        }

        Debug.Log($"[VisibleFacadeExtractor] Façade générée : {newVerts.Count} sommets, {CountAllTris(finalSubIndices)} tris, {finalSubIndices.Count} sous-mesh(es). " +
                  $"Mode={(useDirectionalSweep ? "Directional Sweep" : "Perspective Rays")} | Sortie={(outputAsSibling ? "Sibling" : "Child")}.");
    }

    // === Utils ===

    static int CountAllTris(List<int[]> subIdx)
    {
        int c = 0; foreach (var a in subIdx) if (a != null) c += a.Length / 3; return c;
    }

    static int GetOrAddRemap(
        int oldIndex,
        Dictionary<int,int> map,
        List<Vector3> newV,
        List<Vector3> newN,
        List<Vector4> newT,
        List<Vector2> newUV,
        List<Vector2> newUV2,
        List<Color> newC,
        Vector3[] srcV,
        Vector3[] srcN,
        Vector4[] srcT,
        Vector2[] srcUV,
        Vector2[] srcUV2,
        Color[] srcC)
    {
        if (map.TryGetValue(oldIndex, out int ni)) return ni;
        int newIndex = newV.Count;
        map.Add(oldIndex, newIndex);
        newV.Add(srcV[oldIndex]);
        if (newN != null && srcN != null && srcN.Length == srcV.Length) newN.Add(srcN[oldIndex]);
        if (newT != null && srcT != null && srcT.Length == srcV.Length) newT.Add(srcT[oldIndex]);
        if (newUV != null && srcUV != null && srcUV.Length == srcV.Length) newUV.Add(srcUV[oldIndex]);
        if (newUV2 != null && srcUV2 != null && srcUV2.Length == srcV.Length) newUV2.Add(srcUV2[oldIndex]);
        if (newC != null && srcC != null && srcC.Length == srcV.Length) newC.Add(srcC[oldIndex]);
        return newIndex;
    }

    static List<Vector3> BuildBarySamples(int count)
    {
        var list = new List<Vector3>(count);
        if (count >= 1) list.Add(new Vector3(1f/3f, 1f/3f, 1f/3f));
        if (count >= 2) list.Add(new Vector3(0.6f, 0.2f, 0.2f));
        if (count >= 3) list.Add(new Vector3(0.2f, 0.6f, 0.2f));
        if (count >= 4) list.Add(new Vector3(0.2f, 0.2f, 0.6f));
        if (count >= 5) list.Add(new Vector3(0.5f, 0.3f, 0.2f));
        if (count >= 6) list.Add(new Vector3(0.2f, 0.5f, 0.3f));
        if (count >= 7) list.Add(new Vector3(0.3f, 0.2f, 0.5f));
        for (int i = 7; i < count; i++)
        {
            float u = VanDerCorput(i + 1, 2);
            float v = VanDerCorput(i + 1, 3);
            float a = u;
            float b = v * (1f - a);
            float c = 1f - a - b;
            list.Add(new Vector3(a, b, c));
        }
        for (int i = 0; i < list.Count; i++)
        {
            var v = list[i];
            float s = v.x + v.y + v.z;
            list[i] = (s <= 1e-6f) ? new Vector3(1f/3f, 1f/3f, 1f/3f) : (v / s);
        }
        return list;
    }

    static float VanDerCorput(int n, int b)
    {
        float x = 0f, denom = 1f;
        while (n > 0)
        {
            denom *= b;
            x += (n % b) / denom;
            n /= b;
        }
        return x;
    }

    /// <summary>
    /// Crée/retourne l'objet de sortie.
    /// - Si outputAsSibling = true : parent = transform.parent (même niveau que le générateur), même pose monde.
    /// - Sinon : parent = transform (enfant).
    /// </summary>
    GameObject PrepareOutput(string name, MeshRenderer srcRenderer, bool asSibling, VisibleFacadeExtractor src)
    {
        Transform desiredParent = asSibling ? transform.parent : transform;
        GameObject go = null;

        if (desiredParent != null)
        {
            for (int i = 0; i < desiredParent.childCount; i++)
            {
                var c = desiredParent.GetChild(i);
                if (c.name == name) { go = c.gameObject; break; }
            }
        }

        if (!go)
        {
            go = new GameObject(name);
            go.transform.SetParent(desiredParent, false);
        }

        if (asSibling)
        {
            // Conserver la même pose monde que le générateur
            go.transform.position = transform.position;
            go.transform.rotation = transform.rotation;
            go.transform.localScale = transform.lossyScale;
        }
        else
        {
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;
        }

        var mf = go.GetComponent<MeshFilter>(); if (!mf) mf = go.AddComponent<MeshFilter>();
        var mr = go.GetComponent<MeshRenderer>(); if (!mr) mr = go.AddComponent<MeshRenderer>();

        // Copie paramètres de rendu utiles
        mr.shadowCastingMode = srcRenderer.shadowCastingMode;
        mr.receiveShadows    = srcRenderer.receiveShadows;
#if UNITY_2021_1_OR_NEWER
        mr.allowOcclusionWhenDynamic = srcRenderer.allowOcclusionWhenDynamic;
#endif
        mr.lightProbeUsage    = srcRenderer.lightProbeUsage;
        mr.reflectionProbeUsage = srcRenderer.reflectionProbeUsage;
        mr.probeAnchor        = srcRenderer.probeAnchor;

        if (copyLayerAndStaticFlags)
        {
            go.layer = srcRenderer.gameObject.layer;
#if UNITY_EDITOR
            GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(srcRenderer.gameObject));
#endif
        }

        return go;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(VisibleFacadeExtractor))]
    class VisibleFacadeExtractorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Facade", GUILayout.Height(30)))
            {
                var comp = (VisibleFacadeExtractor)target;
                comp.GenerateFacade();
            }

            EditorGUILayout.HelpBox(
                "Mode Directional Sweep : homogène sur toute la façade (recommandé).\n" +
                "La façade est créée puis l'objet générateur est détruit (par défaut).\n" +
                "Si vous tenez à garder le générateur, décochez 'destroyOriginalGameObject'.",
                MessageType.Info);
        }
    }
#endif
}
