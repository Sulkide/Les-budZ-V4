using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[AddComponentMenu("UI/Effects/Image Outline On Enable")]
public class Highlight : MonoBehaviour
{
    [Header("Outline Settings")]
    [Tooltip("Couleur de l'outline")]
    [SerializeField] private Color outlineColor = Color.white;
    [Tooltip("Distance de l'effet (X = horizontal, Y = vertical)")]
    [SerializeField] private Vector2 effectDistance = new Vector2(1f, -1f);

    private Outline outline;

    private void Awake()
    {
        // Récupère ou ajoute le composant Outline
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }
        // On désactive l'outline au démarrage
        outline.enabled = false;
    }

    private void OnEnable()
    {
        // Configure et active l'outline
        outline.effectColor = outlineColor;
        outline.effectDistance = effectDistance;
        outline.enabled = true;
    }

    private void OnDisable()
    {
        // Désactive l'outline quand ce script est désactivé
        if (outline != null)
            outline.enabled = false;
    }
}