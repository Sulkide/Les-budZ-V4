using UnityEngine;
using Unity.Cinemachine;

/**
 * CameraController2D
 * 
 * Ce script contrôle une caméra 2D pour un jeu multijoueur (jusqu'à 4 joueurs).
 * 
 * Fonctionnalités :
 * - Calcule la position du groupe en considérant tous les joueurs vivants.
 * - Tant que le dézoom maximum n'est pas atteint, tous les joueurs sont affichés.
 * - La position est ensuite ajustée pour prioriser le joueur le plus à droite (garantir sa visibilité).
 * - Une fois le zoom maximum atteint, si le groupe est trop étendu vers la gauche, 
 *   les joueurs trop à gauche (dépassant leftFollowThreshold) sont ignorés dans le calcul.
 * - Le zoom est ajusté en fonction de la progression horizontale du joueur le plus à droite.
 * - Un lissage est appliqué sur la position et le zoom pour un effet fluide.
 */
public class CameraController2D : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Composant Cinemachine Camera utilisé pour la gestion de la caméra.")]
    public CinemachineCamera cinemachineCamera;
    [Tooltip("Dummy target suivi par le Cinemachine Position Composer.")]
    public Transform dummyTarget;

    [Header("Paramètres Zoom")]
    [Tooltip("Zoom le plus rapproché (taille orthographique minimale).")]
    public float minZoom = 5f;
    [Tooltip("Zoom le plus éloigné (taille orthographique maximale).")]
    public float maxZoom = 10f;
    [Tooltip("Position X à partir de laquelle le zoom commence à évoluer.")]
    public float zoomStartX = 0f;
    [Tooltip("Position X à laquelle le zoom atteint son maximum.")]
    public float zoomEndX = 100f;

    [Header("Décalage et Marges")]
    [Tooltip("Décalage appliqué à la position calculée du groupe (ex : pour regarder plus en avant ou ajuster la hauteur).")]
    public Vector2 offset = Vector2.zero;
    [Tooltip("Marge horizontale pour garantir que le joueur le plus à droite reste visible.")]
    public float horizontalMargin = 2f;
    [Tooltip("Marge verticale pour garantir que le joueur le plus haut reste visible.")]
    public float verticalMargin = 1f;
    [Tooltip("Seuil en unités monde pour ignorer les joueurs trop à gauche par rapport au joueur le plus à droite, appliqué uniquement en dézoom max.")]
    public float leftFollowThreshold = 10f;

    [Header("Smoothing")]
    [Tooltip("Temps de lissage pour la position de la caméra.")]
    public float positionSmoothTime = 0.3f;
    [Tooltip("Temps de lissage pour le zoom de la caméra.")]
    public float zoomSmoothTime = 0.5f;

    // Variables internes pour le lissage
    private Vector3 positionVelocity = Vector3.zero;
    private float zoomVelocity = 0f;
    // Permet de ne jamais revenir à un zoom rapproché une fois que le dézoom a progressé
    private float maxReachedZoom;

    // Référence à la caméra principale (pour récupérer l'aspect)
    private Camera mainCamera;

    void Awake()
    {
        if (cinemachineCamera == null)
            Debug.LogWarning("Aucune référence assignée pour CinemachineCamera !");
        if (dummyTarget == null)
            Debug.LogWarning("Aucun dummyTarget assigné !");
        mainCamera = Camera.main;
    }

    void Start()
    {
        // Initialisation : le zoom maximum atteint commence à minZoom
        maxReachedZoom = minZoom;
    }

    void LateUpdate()
    {
        // Calcul des bornes du groupe parmi les joueurs vivants
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        bool anyPlayerAlive = false;

        // Joueur 1
        if (GameManager.instance != null && GameManager.instance.player1Location != null && !GameManager.instance.isPlayer1Dead)
        {
            anyPlayerAlive = true;
            Vector3 pos = GameManager.instance.player1Location.position;
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }
        // Joueur 2
        if (GameManager.instance != null && GameManager.instance.player2Location != null && !GameManager.instance.isPlayer2Dead)
        {
            anyPlayerAlive = true;
            Vector3 pos = GameManager.instance.player2Location.position;
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }
        // Joueur 3
        if (GameManager.instance != null && GameManager.instance.player3Location != null && !GameManager.instance.isPlayer3Dead)
        {
            anyPlayerAlive = true;
            Vector3 pos = GameManager.instance.player3Location.position;
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }
        // Joueur 4
        if (GameManager.instance != null && GameManager.instance.player4Location != null && !GameManager.instance.isPlayer4Dead)
        {
            anyPlayerAlive = true;
            Vector3 pos = GameManager.instance.player4Location.position;
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        // Si aucun joueur n'est vivant, on ne fait rien.
        if (!anyPlayerAlive)
            return;

        // Tant que le zoom maximum n'est pas atteint, on affiche tous les joueurs (minX reste tel quel).
        // Si le dézoom max est atteint et que le groupe s'étend trop à gauche, on ignore les joueurs trop à gauche.
        if (maxReachedZoom >= maxZoom - 0.001f && (maxX - minX) > leftFollowThreshold)
        {
            minX = maxX - leftFollowThreshold;
        }

        // Calcul du centre du groupe
        float targetX = (minX + maxX) / 2f;
        float targetY = (minY + maxY) / 2f;

        // Application de l'offset
        targetX += offset.x;
        targetY += offset.y;

        // Calcul des dimensions de l'écran en unités du monde (pour garantir la visibilité des joueurs)
        float currentZoom = cinemachineCamera.Lens.OrthographicSize;
        float halfHeight = currentZoom;
        float halfWidth = mainCamera != null ? halfHeight * mainCamera.aspect : halfHeight;

        // Ajustement pour que le joueur le plus à droite soit toujours visible :
        float rightmostX = maxX;
        float desiredCenterX_Right = rightmostX + horizontalMargin - halfWidth;
        if (targetX < desiredCenterX_Right)
            targetX = desiredCenterX_Right;

        // Ajustement pour la visibilité du joueur le plus à gauche (si nécessaire)
        float leftmostX = minX;
        float desiredCenterX_Left = leftmostX - horizontalMargin + halfWidth;
        if (targetX > desiredCenterX_Left)
            targetX = desiredCenterX_Left;

        // Ajustements verticaux pour garantir la visibilité du joueur le plus haut et du plus bas
        float highestY = maxY;
        float desiredCenterY_Top = highestY + verticalMargin - halfHeight;
        if (targetY < desiredCenterY_Top)
            targetY = desiredCenterY_Top;
        float lowestY = minY;
        float desiredCenterY_Bottom = lowestY - verticalMargin + halfHeight;
        if (targetY > desiredCenterY_Bottom)
            targetY = desiredCenterY_Bottom;

        // Calcul du zoom cible en fonction de la progression horizontale du joueur le plus à droite
        float leadX = maxX;
        float zoomT = zoomEndX != zoomStartX ? Mathf.Clamp01((leadX - zoomStartX) / (zoomEndX - zoomStartX)) : 0f;
        float targetZoom = Mathf.Lerp(minZoom, maxZoom, zoomT);

        // Empêcher le retour à un zoom rapproché une fois qu'un dézoom plus important a été atteint
        if (targetZoom > maxReachedZoom)
        {
            maxReachedZoom = targetZoom;
        }
        else
        {
            targetZoom = maxReachedZoom;
        }

        // Lissage de la position du dummyTarget
        Vector3 targetPosition = new Vector3(targetX, targetY, dummyTarget.position.z);
        Vector3 smoothedPosition = Vector3.SmoothDamp(dummyTarget.position, targetPosition, ref positionVelocity, positionSmoothTime);
        dummyTarget.position = smoothedPosition;

        // Lissage du zoom (OrthographicSize) de la caméra
        float currentSize = cinemachineCamera.Lens.OrthographicSize;
        float smoothedSize = Mathf.SmoothDamp(currentSize, targetZoom, ref zoomVelocity, zoomSmoothTime);
        cinemachineCamera.Lens.OrthographicSize = Mathf.Clamp(smoothedSize, minZoom, maxZoom);
    }
}
