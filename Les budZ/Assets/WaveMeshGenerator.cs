using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[DisallowMultipleComponent]
public class WaveGenerator : MonoBehaviour
{
    public enum WaveAxis { AlongX, AlongZ }

    [Header("Source")]
    public MeshFilter sourceMeshFilter;
    public Material waveMaterial;

    [Header("Wave Shape")]
    [Min(0.01f)] public float wavelength = 1.0f;       // utilisé si useExactWaveCount = false
    [Min(0f)]    public float amplitudeY = 0.25f;
    public WaveAxis axis = WaveAxis.AlongX;
    [Min(2)] public int segmentsPerWavelength = 20;
    [Min(1)] public int perpendicularSegments = 2;
    public float baseYOffset = 0f;

    [Header("Mode: nombre exact de vagues")]
    [Tooltip("Quand activé, on génère EXACTEMENT 'waveCount' vagues sur toute la longueur des bounds, sans dépasser.")]
    public bool useExactWaveCount = false;
    [Min(1)] public int waveCount = 3;

    [Header("Bounds & Fit")]
    [Tooltip("Si useExactWaveCount = false, ajuste pour caser un nombre ENTIER d’ondes sans dépasser.")]
    public bool snapToWholeWaves = true;
    [Tooltip("Centre le motif afin de rester strictement à l’intérieur des bounds.")]
    public bool clampInsideBounds = true;

    [Header("Output")]
    public string outputName = "WaveMesh";
    public bool replacePrevious = true;
    public bool parentUnderSource = true;
    
#if UNITY_EDITOR
    [Header("Persistence (Editor)")]
    public bool saveMeshAsAsset = true;
    public string assetFolder = "Assets/GeneratedMeshes";
    public bool regenerateOnEnableIfMissing = true;
#endif
    
#if UNITY_EDITOR
    void OnEnable()
    {
        if (!Application.isPlaying && regenerateOnEnableIfMissing)
        {
            // Si l’output existe mais a perdu son mesh (cas typique en Prefab Mode)
            Transform parent = parentUnderSource ? (sourceMeshFilter ? sourceMeshFilter.transform : transform) : null;
            Transform searchRoot = parent ? parent : transform.parent;
            var found = (searchRoot ? searchRoot.Find(outputName) : null) as Transform;
            var outMF = found ? found.GetComponent<MeshFilter>() : null;
            if (outMF != null && outMF.sharedMesh == null)
            {
                // Regénère silencieusement
                Generate();
            }
        }
    }
#endif


    public void Generate()
    {
        var mf = sourceMeshFilter != null ? sourceMeshFilter : GetComponent<MeshFilter>();
        var mr = mf ? mf.GetComponent<MeshRenderer>() : null;

        if (mf == null || mr == null || mf.sharedMesh == null)
        {
            Debug.LogError("[WaveGenerator] MeshFilter/MeshRenderer manquant(s) ou mesh nul.");
            return;
        }
        if (segmentsPerWavelength < 2 || perpendicularSegments < 1)
        {
            Debug.LogError("[WaveGenerator] Paramètres d’échantillonnage invalides.");
            return;
        }

        Bounds worldB = mr.bounds;
        var srcTr = mf.transform;

        Vector3 localMin = srcTr.InverseTransformPoint(worldB.min);
        Vector3 localMax = srcTr.InverseTransformPoint(worldB.max);

        float sizeX = Mathf.Abs(localMax.x - localMin.x);
        float sizeZ = Mathf.Abs(localMax.z - localMin.z);
        float minX = Mathf.Min(localMin.x, localMax.x);
        float maxX = Mathf.Max(localMin.x, localMax.x);
        float minZ = Mathf.Min(localMin.z, localMax.z);
        float maxZ = Mathf.Max(localMin.z, localMax.z);

        bool alongX = axis == WaveAxis.AlongX;
        float length = alongX ? sizeX : sizeZ; // dimension de propagation
        float width  = alongX ? sizeZ : sizeX; // dimension perpendiculaire
        if (length <= 0f || width <= 0f)
        {
            Debug.LogError("[WaveGenerator] Bounds trop petits en X/Z.");
            return;
        }

        // Détermination de la longueur utilisée, du pas et du facteur d’onde
        float usedLength;
        float margin;
        int stepsLength;
        float k; // fréquence spatiale

        if (useExactWaveCount)
        {
            waveCount = Mathf.Max(1, waveCount);
            usedLength = length;       // on remplit toute la longueur, donc pas de marge
            margin = 0f;
            stepsLength = Mathf.Max(2, waveCount * segmentsPerWavelength);
            k = Mathf.PI * 2f * waveCount / usedLength; // phase = 2π * N * t
        }
        else
        {
            if (wavelength <= 0f) wavelength = 0.01f;

            int waveCountInt = 1;
            usedLength = length;

            if (snapToWholeWaves)
            {
                waveCountInt = Mathf.Max(1, Mathf.FloorToInt(length / wavelength));
                usedLength   = waveCountInt * wavelength;
            }

            margin = clampInsideBounds ? (length - usedLength) * 0.5f : 0f;
            stepsLength = Mathf.Max(2, waveCountInt * segmentsPerWavelength);
            k = snapToWholeWaves ? (Mathf.PI * 2f / wavelength) : (Mathf.PI * 2f / usedLength);
        }

        int stepsWidth = Mathf.Max(1, perpendicularSegments);

        float startL = alongX ? (minX + margin) : (minZ + margin);
        float endL   = startL + usedLength;
        float startW = alongX ? minZ : minX;
        float endW   = alongX ? maxZ : maxX;

        var verts = new List<Vector3>((stepsLength + 1) * (stepsWidth + 1));
        var norms = new List<Vector3>(verts.Capacity);
        var uvs   = new List<Vector2>(verts.Capacity);
        var tris  = new List<int>(stepsLength * stepsWidth * 6);

        for (int j = 0; j <= stepsWidth; j++)
        {
            float tW = stepsWidth == 0 ? 0f : (float)j / stepsWidth;
            float wPos = Mathf.Lerp(startW, endW, tW);

            for (int i = 0; i <= stepsLength; i++)
            {
                float tL = (float)i / stepsLength;
                float lPos = Mathf.Lerp(startL, endL, tL);

                float phase = k * (tL * usedLength);
                float y = baseYOffset + Mathf.Sin(phase) * amplitudeY;

                Vector3 p = alongX ? new Vector3(lPos, y, wPos) : new Vector3(wPos, y, lPos);
                verts.Add(p);
                norms.Add(Vector3.up);

                // UV : u = progression le long (échelle = nb de périodes), v = perpendiculaire
                float periods = useExactWaveCount ? waveCount : (snapToWholeWaves ? (usedLength / Mathf.Max(0.0001f, wavelength)) : 1f);
                uvs.Add(new Vector2(tL * periods, tW));
            }
        }

        int stride = stepsLength + 1;
        for (int j = 0; j < stepsWidth; j++)
        {
            for (int i = 0; i < stepsLength; i++)
            {
                int a = j * stride + i;
                int b = a + 1;
                int c = a + stride;
                int d = c + 1;

                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }

        Transform parent = parentUnderSource ? mf.transform : null;

        GameObject existing = null;
        if (replacePrevious)
        {
            var searchRoot = parent ? parent : mf.transform.parent;
            var found = searchRoot ? searchRoot.Find(outputName) : null;
            if (found != null) existing = found.gameObject;
        }

        if (existing != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RecordObject(existing, "Replace WaveMesh");
#endif
            ApplyToGameObject(existing, verts, norms, uvs, tris);
        }
        else
        {
            var go = new GameObject(outputName);
#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(go, "Create WaveMesh");
#endif
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = mf.transform.position;
            go.transform.rotation = mf.transform.rotation;
            go.transform.localScale = Vector3.one;

            ApplyToGameObject(go, verts, norms, uvs, tris);
        }
    }

    void ApplyToGameObject(GameObject go, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
    {
        var outMF = go.GetComponent<MeshFilter>();
        if (outMF == null) outMF = go.AddComponent<MeshFilter>();
        var outMR = go.GetComponent<MeshRenderer>();
        if (outMR == null) outMR = go.AddComponent<MeshRenderer>();

        var mesh = new Mesh();
        mesh.name = "WaveMeshRuntime";
        mesh.SetVertices(v);
        mesh.SetNormals(n);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

#if UNITY_EDITOR
        if (!Application.isPlaying && saveMeshAsAsset)
        {
            // Assure le dossier
            if (!AssetDatabase.IsValidFolder(assetFolder))
            {
                var parts = assetFolder.Trim('/').Split('/');
                string path = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = path + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(path, parts[i]);
                    path = next;
                }
            }

            // Nom unique
            string fileName = $"{go.name}_{System.DateTime.Now:yyyyMMdd_HHmmssfff}.asset";
            string assetPath = System.IO.Path.Combine(assetFolder, fileName).Replace("\\", "/");
            AssetDatabase.CreateAsset(mesh, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif

        outMF.sharedMesh = mesh;

        if (waveMaterial != null)
            outMR.sharedMaterial = waveMaterial;
        else
            outMR.sharedMaterial = outMR.sharedMaterial != null ? outMR.sharedMaterial : new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(WaveGenerator))]
public class WaveGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space(8);

        var comp = (WaveGenerator)target;

        // Petits repères d’usage
        if (comp.useExactWaveCount)
        {
            EditorGUILayout.HelpBox(
                "Mode 'Nombre exact de vagues' : le mesh remplit toute la longueur des bounds avec exactement " +
                comp.waveCount + " vagues, sans dépasser. Les paramètres 'snapToWholeWaves' et 'wavelength' sont ignorés.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Mode 'Longueur d’onde' :\n" +
                "- snapToWholeWaves ON : caser un nombre ENTIER de vagues (centré, sans dépasser).\n" +
                "- snapToWholeWaves OFF : 1 période est étirée sur toute la longueur.",
                MessageType.None);
        }

        if (GUILayout.Button("Generate", GUILayout.Height(32)))
            comp.Generate();
    }
    
    
}
#endif


