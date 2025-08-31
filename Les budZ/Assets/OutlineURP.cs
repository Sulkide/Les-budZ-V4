using UnityEngine;

// Ce composant requiert MeshFilter et MeshRenderer sur le GameObject
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OutlineURP : MonoBehaviour
{
    [Tooltip("Couleur du contour extérieur")]
    public Color outlineColor = Color.black;
    [Tooltip("Épaisseur du contour (unités monde)")]
    [Range(0f, 0.2f)] public float outlineWidth = 0.1f;

    private Material outlineMaterial;
    private MeshRenderer meshRenderer;
    private readonly string outlineShaderName = "Custom/OutlineURP";
    
    [Header("Crease lines (arêtes)")]
    public Color creaseColor = Color.black;
    [Range(0.02f, 0.6f)] public float creaseThreshold = 0.22f;
    [Range(0.5f, 3f)] public float creaseWidthPx = 1f;
    [Range(0f, 1f)] public float creaseOpacity = 1f;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        // Vérifie la présence des composants requis
        if (meshRenderer == null || GetComponent<MeshFilter>() == null)
        {
            Debug.LogError("Le composant OutlineURP nécessite un MeshFilter et un MeshRenderer sur le même objet.");
            enabled = false;
            return;
        }
        // Charge le shader d'outline URP
        Shader outlineShader = Shader.Find(outlineShaderName);
        if (outlineShader == null)
        {
            Debug.LogError("Shader d'outline introuvable. Assurez-vous qu'un shader nommé \"" 
                           + outlineShaderName + "\" existe dans le projet.");
            enabled = false;
            return;
        }
        // Crée le matériau d'outline à partir du shader
        outlineMaterial = new Material(outlineShader);
        // Copie la texture et la couleur du matériau original s'ils existent
        Material originalMat = meshRenderer.sharedMaterial;
        if (originalMat != null)
        {
            if (originalMat.mainTexture != null)
                outlineMaterial.SetTexture("_MainTex", originalMat.mainTexture);
            outlineMaterial.SetColor("_Color", originalMat.color);
        }
        // Applique les paramètres initiaux d'outline
        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineMaterial.SetColor("_CreaseColor", creaseColor);
        outlineMaterial.SetFloat("_CreaseThreshold", creaseThreshold);
        outlineMaterial.SetFloat("_CreaseWidthPx", creaseWidthPx);
        outlineMaterial.SetFloat("_CreaseOpacity", creaseOpacity);
        // Assigne le nouveau matériau à l'objet
        meshRenderer.material = outlineMaterial;
    }

    // Appelé lors de modifications dans l’éditeur
    void OnValidate()
    {
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", outlineColor);
            outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
            outlineMaterial.SetColor("_CreaseColor", creaseColor);
            outlineMaterial.SetFloat("_CreaseThreshold", creaseThreshold);
            outlineMaterial.SetFloat("_CreaseWidthPx", creaseWidthPx);
            outlineMaterial.SetFloat("_CreaseOpacity", creaseOpacity);
        }
    }
}
