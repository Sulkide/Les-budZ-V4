using UnityEngine;
using UnityEditor;


[ExecuteAlways]
[DisallowMultipleComponent]
public class GridClothPlane : MonoBehaviour
{
    [Header("Taille du plane (m)")]
    public float width = 2f;
    public float height = 2f;

    [Header("Subdivision (plus haut = plus dense)")]
    [Min(1)] public int xSegments = 40;
    [Min(1)] public int ySegments = 40;

    [Header("Matériau (optionnel)")]
    public Material material;

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (width <= 0f) width = 1f;
        if (height <= 0f) height = 1f;
        xSegments = Mathf.Max(1, xSegments);
        ySegments = Mathf.Max(1, ySegments);

        int vx = xSegments + 1;
        int vy = ySegments + 1;
        int vertexCount = vx * vy;
        int quadCount = xSegments * ySegments;

        var verts = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var uvs = new Vector2[vertexCount];
        var tris = new int[quadCount * 6];

        // Génération vertices/UV/normales
        for (int y = 0; y < vy; y++)
        {
            float fy = (float)y / ySegments;
            for (int x = 0; x < vx; x++)
            {
                float fx = (float)x / xSegments;
                int i = y * vx + x;

                float px = Mathf.Lerp(-width * 0.5f, width * 0.5f, fx);
                float pz = Mathf.Lerp(-height * 0.5f, height * 0.5f, fy);

                verts[i] = new Vector3(px, 0f, pz);
                normals[i] = Vector3.up;
                uvs[i] = new Vector2(fx, fy);
            }
        }

        // Triangles (winding clockwise pour face vers le haut)
        int t = 0;
        for (int y = 0; y < ySegments; y++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int i0 = y * vx + x;
                int i1 = i0 + 1;
                int i2 = i0 + vx;
                int i3 = i2 + 1;

                // Tri 1
                tris[t++] = i0;
                tris[t++] = i2;
                tris[t++] = i1;

                // Tri 2
                tris[t++] = i1;
                tris[t++] = i2;
                tris[t++] = i3;
            }
        }

        var mesh = new Mesh();
        mesh.name = $"GridClothPlane_{xSegments}x{ySegments}";
        if (vertexCount > 65000)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        // SkinnedMeshRenderer requis par Cloth
        var smr = GetComponent<SkinnedMeshRenderer>();
        if (!smr) smr = gameObject.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = mesh;

        if (material == null)
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        smr.sharedMaterial = material;

        // Ajoute Cloth si absent (tu devras peindre les contraintes ensuite)
        var cloth = GetComponent<Cloth>();
        if (!cloth) cloth = gameObject.AddComponent<Cloth>();

        // Conseils basiques de qualité (optionnel)
        cloth.useGravity = true;
        cloth.worldAccelerationScale = 1f;
        cloth.worldVelocityScale = 1f;

        // Important : peins à nouveau les contraintes après régénération du mesh
        Debug.Log($"Generated cloth-ready grid: {xSegments}x{ySegments} ({vertexCount} verts). Repeignez les contraintes Cloth.");
    }

#if UNITY_EDITOR
    // Auto-clamp & aperçu “live” dans l’éditeur
    private void OnValidate()
    {
        xSegments = Mathf.Max(1, xSegments);
        ySegments = Mathf.Max(1, ySegments);
    }
#endif
}
