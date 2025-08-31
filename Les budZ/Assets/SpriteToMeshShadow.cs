using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]


public class SpriteToMeshShadowCaster : MonoBehaviour
{
    
    
    public Vector3 cloneMesheRotationOffset = new Vector3(0, 0, 0.15f);
    void Start()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Sprite sprite = spriteRenderer.sprite;

        // Crée un enfant avec MeshFilter + MeshRenderer
        GameObject shadowCaster = new GameObject("SpriteShadowCaster");
        shadowCaster.transform.SetParent(transform);
        shadowCaster.transform.localPosition = cloneMesheRotationOffset;
        shadowCaster.transform.localRotation = Quaternion.identity;
        shadowCaster.transform.localScale = Vector3.one;

        var meshFilter = shadowCaster.AddComponent<MeshFilter>();
        var meshRenderer = shadowCaster.AddComponent<MeshRenderer>();

        // Convertit le sprite en mesh
        meshFilter.mesh = SpriteToMesh(sprite);

        // Crée un matériau URP avec transparence + alpha clip
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.mainTexture = sprite.texture;
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0); // Alpha blend mode
        mat.SetFloat("_AlphaClip", 1); // Active alpha clipping
        mat.SetFloat("_Cutoff", 0.1f); // Seuil d’opacité à ajuster selon ton sprite
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHATEST_ON");

        meshRenderer.material = mat;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        meshRenderer.receiveShadows = true;

        // Désactive le SpriteRenderer (ou pas, à toi de choisir)
        //spriteRenderer.enabled = false;
    }

    Mesh SpriteToMesh(Sprite sprite)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[sprite.vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = sprite.vertices[i];

        mesh.vertices = System.Array.ConvertAll(vertices, v => (Vector3)v);
        mesh.triangles = System.Array.ConvertAll(sprite.triangles, i => (int)i);
        mesh.uv = sprite.uv;
        return mesh;
    }
}
