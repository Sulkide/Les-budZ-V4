using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [Header("Settings")]
    public int damage = 1;
    public Transform respawnPoint;
    
    [Header("KnockBack Settings")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private bool doDamage = true; // Si true, appelle la coroutine Death() plutôt que KnockBack.

    // On utilisera les positions de pointA et pointZ pour calculer la direction du KnockBack
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointZ;

    [Header("Overlap Box Settings")]
    [Tooltip("Taille du carré de détection (Largeur, Hauteur).")]
    [SerializeField] private Vector2 detectionSize = new Vector2(2f, 2f);
    [Tooltip("Angle de rotation du carré (en degrés).")]
    [SerializeField] private float detectionAngle = 0f;
    [SerializeField] private LayerMask playerLayerMask;

    // Permet de ne pas relancer l'effet (KnockBack ou Death) sur un même joueur 
    // tant qu'il reste dans la zone.
    private HashSet<Collider2D> _alreadyHit = new HashSet<Collider2D>();

    private void Update()
    {
        // Récupère tous les colliders “Player” dans la zone définie par OverlapBox
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            transform.position,    // Centre du box
            detectionSize,         // Taille du box (larg., haut.)
            detectionAngle,        // Angle en degrés
            playerLayerMask        // LayerMask pour le joueur
        );

        // Retire de _alreadyHit ceux qui ne sont plus dans hits
        _alreadyHit.RemoveWhere(col => !hits.Contains(col));

        // Parcourt les colliders détectés
        foreach (Collider2D col in hits)
        {
            // Si on ne l’a pas encore traité pendant qu’il est dans la zone
            if (!_alreadyHit.Contains(col))
            {
                // Tente de récupérer le PlayerMovement
                PlayerMovement playerMovement = col.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                {
                    // Calcul de la direction : pointA -> pointZ
                    Vector2 direction = (pointZ.position - pointA.position).normalized;
                    // On appelle KnockBack avec le bool "doDamage"
                    playerMovement.KnockBack(direction, doDamage, knockbackForce, true, damage);

                    // On enregistre ce collider pour éviter de le re-traiter 
                    // tant qu’il n’est pas sorti de la zone.
                    _alreadyHit.Add(col);
                    
                    playerMovement.gameObject.transform.position = respawnPoint.position;
                }
            }
        }
    }

    // Pour visualiser la zone de détection en forme de carré dans la scène
    private void OnDrawGizmos()
    {
        // Dessin du box
        Gizmos.color = Color.red;
        // Sauvegarder la matrice d’origine
        Matrix4x4 oldMatrix = Gizmos.matrix;
        // Construire la matrice : position + rotation
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0, 0, detectionAngle), Vector3.one);
        // Dessiner le carré “fil de fer”
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(detectionSize.x, detectionSize.y, 1f));
        // Restaurer la matrice d’origine
        Gizmos.matrix = oldMatrix;

        // Dessin de la ligne entre pointA et pointZ
        if (pointA != null && pointZ != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pointA.position, pointZ.position);
        }
    }
}
