using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.Rendering;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SpriteSkin))]
public class SpriteSkinShadowCaster : MonoBehaviour
{
    private SpriteSkin spriteSkin;
    private SpriteRenderer spriteRenderer;

    private GameObject shadowGO;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh dynamicMesh;

    private Material shadowMaterial;

    void Awake()
    {
        spriteSkin = GetComponent<SpriteSkin>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Crée un GameObject enfant pour l'ombre
        shadowGO = new GameObject("ShadowMesh");
        shadowGO.transform.SetParent(transform, false);

        meshFilter = shadowGO.AddComponent<MeshFilter>();
        meshRenderer = shadowGO.AddComponent<MeshRenderer>();

        dynamicMesh = new Mesh();
        dynamicMesh.MarkDynamic();
        meshFilter.sharedMesh = dynamicMesh;

        // Matériau URP Lit avec transparence + clipping
        shadowMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        shadowMaterial.mainTexture = spriteRenderer.sprite.texture;
        shadowMaterial.color = Color.white;

        shadowMaterial.SetFloat("_Surface", 1); // Transparent
        shadowMaterial.SetFloat("_AlphaClip", 1); // Alpha clipping
        shadowMaterial.SetFloat("_Cutoff", 0.1f);
        shadowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        shadowMaterial.EnableKeyword("_ALPHATEST_ON");

        shadowMaterial.renderQueue = (int)RenderQueue.AlphaTest;

        meshRenderer.material = shadowMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.On;
        meshRenderer.receiveShadows = true;

        // Ne pas désactiver le SpriteRenderer ici — on veut les deux
    }

    void LateUpdate()
    {
        if (spriteSkin == null || !spriteSkin.isActiveAndEnabled || !spriteSkin.isActiveAndEnabled)
            return;

        // Met à jour le mesh animé dynamiquement
        //spriteSkin.BakeMesh(dynamicMesh);
        meshFilter.sharedMesh = dynamicMesh;

        // Synchronise la couleur du sprite
        shadowMaterial.color = spriteRenderer.color;
    }
}
