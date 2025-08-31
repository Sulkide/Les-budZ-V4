// BorderSpawner.cs
// Ce script instancie un prefab le long des bords d'un PolygonCollider2D
// Les instances sont espacées uniformément sur tout le contour, indépendamment des segments individuels.
// Ajout d'offsets de position, rotation et scale applicables aux instances.
// Ne spawne que si l'angle Z final est entre -25 et 25 degrés.

using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class BorderSpawner : MonoBehaviour
{
    [Tooltip("Le prefab à instancier le long du bord.")]
    public GameObject prefab;

    [Tooltip("Espacement uniforme entre chaque instance.")]
    public float spacing = 1f;

    [Tooltip("Faire tourner le prefab pour qu'il suive la direction du bord.")]
    public bool alignToEdge = false;

    [Tooltip("Offset de position à appliquer sur chaque instance (local au prefab).")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("Offset de rotation (en degrés) à appliquer sur chaque instance.")]
    public Vector3 rotationOffsetEuler = Vector3.zero;

    [Tooltip("Offset d'échelle à appliquer sur chaque instance (multiplie l'échelle originale). ")]
    public Vector3 scaleOffset = Vector3.one;

    [Tooltip("Angle Z minimum pour instancier (degrés).")]
    public float minZAngle = -25f;

    [Tooltip("Angle Z maximum pour instancier (degrés).")]
    public float maxZAngle = 25f;

    [Tooltip("Lancer automatiquement au démarrage.")]
    public bool spawnOnStart = true;

    void Start()
    {
        if (spawnOnStart)
            SpawnOnBorder();
    }

    [ContextMenu("Spawn Prefabs On Border")]
    public void SpawnOnBorder()
    {
        // Récupère le PolygonCollider2D (ici adapté à votre structure de GameObject)
        var collider = transform.GetChild(2).GetChild(0).GetComponent<PolygonCollider2D>();
        var localPoints = collider.points;
        int pointCount = localPoints.Length;

        // Convertir en points monde
        Vector2[] worldPoints = new Vector2[pointCount];
        for (int i = 0; i < pointCount; i++)
            worldPoints[i] = transform.TransformPoint(localPoints[i]);

        // Calculer les longueurs des arêtes et le périmètre total
        float[] edgeLengths = new float[pointCount];
        float perimeter = 0f;
        for (int i = 0; i < pointCount; i++)
        {
            Vector2 p1 = worldPoints[i];
            Vector2 p2 = worldPoints[(i + 1) % pointCount];
            float len = Vector2.Distance(p1, p2);
            edgeLengths[i] = len;
            perimeter += len;
        }

        // Nombre d'instances à spawn
        int spawnCount = Mathf.FloorToInt(perimeter / spacing);
        int currentEdge = 0;

        for (int i = 0; i <= spawnCount; i++)
        {
            float targetDistance = i * spacing;

            // Trouver sur quelle arête se trouve la position cible
            while (targetDistance > edgeLengths[currentEdge])
            {
                targetDistance -= edgeLengths[currentEdge];
                currentEdge = (currentEdge + 1) % pointCount;
            }

            // Calculer la position de spawn sur cette arête
            Vector2 start = worldPoints[currentEdge];
            Vector2 end = worldPoints[(currentEdge + 1) % pointCount];
            Vector2 dir = (end - start).normalized;
            Vector2 spawnPos2D = start + dir * targetDistance;

            // Calcul de la rotation de base
            Quaternion baseRot = alignToEdge ? Quaternion.FromToRotation(Vector3.up, dir) : Quaternion.identity;

            // Application des offsets de position et rotation
            Vector3 worldPos3D = new Vector3(spawnPos2D.x, spawnPos2D.y, 0f);
            Vector3 finalPos = worldPos3D + baseRot * positionOffset;
            Quaternion finalRot = baseRot * Quaternion.Euler(rotationOffsetEuler);

            // Vérifier l'angle Z dans l'intervalle souhaité
            float zAngle = finalRot.eulerAngles.z;
            if (zAngle > 180f) zAngle -= 360f; // Convertir en [-180,180]
            if (zAngle < minZAngle || zAngle > maxZAngle)
                continue;

            // Instanciation
            GameObject instance = Instantiate(prefab, finalPos, finalRot, transform);

            // Application de l'offset d'échelle (multiplicatif)
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scaleOffset);
        }
    }
}
