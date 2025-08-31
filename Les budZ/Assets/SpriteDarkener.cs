using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteDarkenerByZ : MonoBehaviour
{
    [Header("Paramètres")]
    [Tooltip("Position Z de référence à partir de laquelle l'assombrissement commence.")]
    public float baseZ = 0f;

    [Tooltip("Multiplicateur d'assombrissement par unité de Z au-delà de baseZ.")]
    public float darkenMultiplier = 0.5f;

    [Tooltip("Alpha minimal du sprite (0 = invisible, 1 = opaque).")]
    public float minAlpha = 0.2f;

    [Tooltip("Assombrir uniquement la couleur sans toucher à l'alpha.")]
    public bool affectOnlyColor = true;

    private SpriteRenderer sr;
    private Color originalColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    void Update()
    {
        float zDiff = transform.position.z - baseZ;
        float darkenAmount = Mathf.Max(0f, zDiff * darkenMultiplier);

        // On limite à 1.0f max
        float factor = Mathf.Clamp01(1f - darkenAmount);

        Color newColor = originalColor * factor;

        // Optionnel : conserver alpha original ou forcer un minimum
        if (affectOnlyColor)
        {
            newColor.a = originalColor.a;
        }
        else
        {
            newColor.a = Mathf.Max(minAlpha, originalColor.a * factor);
        }

        sr.color = newColor;
    }
}