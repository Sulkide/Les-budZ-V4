using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// - Copie la couleur de referenceLight vers childSpotLight
/// - Aligne la rotation Y de cet objet sur la rotation Y de referenceLight
/// - Déplace l'objet en X proportionnellement au X de la caméra (parallaxe)
///   en mappant [camStartX..camEndX] -> [lightStartX..lightEndX]
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class MatchGlobalLight : MonoBehaviour
{
    [Header("References")]
    [Tooltip("La Light CIBLE (celle qui reçoit la couleur). Par défaut, on prend la 1ère Light enfant.")]
    [SerializeField] private Light childSpotLight;

    [Tooltip("La caméra principale utilisée pour calculer le parallaxe.")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("La Light SOURCE (sa couleur est copiée et sa rotation Y est suivie).")]
    [SerializeField] private Light referenceLight;

    [Header("Parallax mapping (monde, axe X)")]
    [Tooltip("Position X de départ de la caméra (monde) à partir de laquelle le mapping commence.")]
    [SerializeField] private float camStartX = 0f;

    [Tooltip("Position X d'arrivée de la caméra (monde) à laquelle le mapping termine.")]
    [SerializeField] private float camEndX = 10f;

    [Tooltip("Position X de départ de l'objet (monde) quand la caméra est à camStartX.")]
    [SerializeField] private float lightStartX = 0f;

    [Tooltip("Position X d'arrivée de l'objet (monde) quand la caméra est à camEndX.")]
    [SerializeField] private float lightEndX = 5f;

    [Header("Options")]
    [Tooltip("Initialiser automatiquement camStartX et lightStartX avec les positions courantes au Start/OnEnable.")]
    [SerializeField] private bool autoInitFromCurrent = true;

    [Tooltip("Copier aussi l'intensité de la light source (en plus de la couleur).")]
    [SerializeField] private bool syncIntensity = false;

    // Internes (mémo si besoin)
    private Vector3 objInitialPos;

    private void Awake()
    {
        // Cible par défaut : première Light enfant si non assignée
        if (childSpotLight == null)
        {
            childSpotLight = GetComponentInChildren<Light>(true);
        }
    }

    private void OnEnable()
    {
        // En mode Éditeur, OnEnable peut se déclencher hors Play
        SafeBootstrap();
        if (autoInitFromCurrent)
        {
            AutoInitStarts();
        }
        // Appliquer une première fois
        ApplyColorSync();
        ApplyRotationYMatch();
        ApplyParallaxX();
    }

    private void Start()
    {
        SafeBootstrap();
        if (autoInitFromCurrent)
        {
            AutoInitStarts();
        }
        objInitialPos = transform.position;

        // Première application
        ApplyColorSync();
        ApplyRotationYMatch();
        ApplyParallaxX();
    }

    private void Update()
    {
        if (referenceLight == null) return;

        ApplyColorSync();
        ApplyRotationYMatch();
        ApplyParallaxX();
    }

    private void SafeBootstrap()
    {
        // Caméra fallback : GameManager.instance.mainCamera sinon Camera.main
        if (mainCamera == null)
        {
            mainCamera = (GameManager.instance != null && GameManager.instance.mainCamera != null)
                ? GameManager.instance.mainCamera
                : Camera.main;
        }

        // Light de référence fallback : GameManager.instance.globalLight si exposé
        if (referenceLight == null && GameManager.instance != null)
        {
            referenceLight = GameManager.instance.globalLight;
        }
    }

    private void AutoInitStarts()
    {
        if (mainCamera != null)
        {
            camStartX = mainCamera.transform.position.x;
        }
        lightStartX = transform.position.x;
    }

    private void ApplyColorSync()
    {
        if (childSpotLight == null || referenceLight == null) return;

        // Copie de la couleur (à chaque frame pour suivre toute variation)
        childSpotLight.color = referenceLight.color;

        if (syncIntensity)
        {
            childSpotLight.intensity = referenceLight.intensity;
        }
    }

    private void ApplyRotationYMatch()
    {
        if (referenceLight == null) return;

        // On ne touche qu'à Y pour "égaler" la rotation Y de la light source
        transform.rotation = referenceLight.transform.rotation;
    }

    private void ApplyParallaxX()
    {
        if (mainCamera == null) return;

        float camX = mainCamera.transform.position.x;

        // InverseLerp gère aussi le clamp 0..1 et l'ordre (si end < start, ça inverse correctement)
        float t = Mathf.InverseLerp(camStartX, camEndX, camX);

        float newX = Mathf.Lerp(lightStartX, lightEndX, t);

        var p = transform.position;
        p.x = newX;
        transform.position = p;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Aides visuelles simples en scène
        Gizmos.matrix = Matrix4x4.identity;

        // Caméra range
        Vector3 camA = new Vector3(camStartX, transform.position.y, transform.position.z);
        Vector3 camB = new Vector3(camEndX,   transform.position.y, transform.position.z);
        Handles.Label(camA + Vector3.up * 0.5f, "camStartX");
        Handles.Label(camB + Vector3.up * 0.5f, "camEndX");
        Gizmos.DrawLine(camA, camB);

        // Objet range
        Vector3 objA = new Vector3(lightStartX, transform.position.y, transform.position.z);
        Vector3 objB = new Vector3(lightEndX,   transform.position.y, transform.position.z);
        Handles.Label(objA + Vector3.up * 1.5f, "lightStartX");
        Handles.Label(objB + Vector3.up * 1.5f, "lightEndX");
        Gizmos.DrawLine(objA, objB);
    }
#endif
}
