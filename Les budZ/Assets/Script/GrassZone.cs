using UnityEngine;

/// <summary>
/// Script de génération d'une zone d'herbe rotatable sur l’axe Z.
/// </summary>
public class GrassZone : MonoBehaviour
{
    [Header("Prefab et zone")]
    public GameObject grassPrefab;
    public Vector2   zoneSize       = new Vector2(10f, 5f);
    public int       numberOfBlades = 50;

    [Header("Paramètres du vent global")]
    [Range(-1f, 1f)]
    public float initialWindDirection = 1f;
    [Range(0f, 1f)]
    public float initialWindIntensity = 0.5f;

    [Header("Option de pliage sous le joueur")]
    [Tooltip("Si activé, les brins se penchent latéralement vers le joueur au lieu de s'aplatir")]
    public bool useSideBend = false;

    void Start()
    {
        // Init vent global
        GrassBlade.windDirection = Mathf.Sign(initialWindDirection);
        GrassBlade.windIntensity = initialWindIntensity;

        float halfW = zoneSize.x * 0.5f;
        float halfH = zoneSize.y * 0.5f;

        for (int i = 0; i < numberOfBlades; i++)
        {
            // 1) Générer une position aléatoire en local space
            Vector3 localPos = new Vector3(
                Random.Range(-halfW, halfW),
                Random.Range(-halfH, halfH),
                0f
            );

            // 2) Transformer en world space en tenant compte de la position + rotation
            Vector3 worldPos = transform.TransformPoint(localPos);

            // 3) Instancier en tant qu’enfant pour hériter de la rotation si besoin
            GameObject bladeGO = Instantiate(grassPrefab, worldPos, Quaternion.identity, transform);
            bladeGO.transform.localRotation = Quaternion.identity;

            // 4) Transmettre l’option de pli latéral
            var blade = bladeGO.GetComponent<GrassBlade>();
            if (blade != null)
                blade.useSideBend = useSideBend;

            // 5) Désactiver par défaut : ce sera le CullingManager qui activera
            bladeGO.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Sauver la matrice actuelle
        var oldMat = Gizmos.matrix;

        // Définir une matrice de transformation: position + rotation Z + échelle 1
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        // Dessiner un wire-cube centré en (0,0) en local space
        Gizmos.color = Color.green;
        Vector3 size = new Vector3(zoneSize.x, zoneSize.y, 0f);
        Gizmos.DrawWireCube(Vector3.zero, size);

        // Rétablir la matrice d’origine
        Gizmos.matrix = oldMat;
    }
}