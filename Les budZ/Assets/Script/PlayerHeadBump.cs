using Unity.VisualScripting;
using UnityEngine;

public class PlayerHeadBump : MonoBehaviour
{
    public PlayerMovement player;
    // Taille de la zone de détection
    public Vector2 detectionBoxSize = new Vector2(1f, 1f);
    // Angle de la zone de détection (en degrés)
    public float detectionBoxAngle = 0f;
    // Option pour que l'angle de détection suive en permanence la rotation Z du parent ciblé

    public int damage = 0;
    // Masque de layer pour filtrer uniquement les objets du layer "Player"
    private int playerLayerMask;

    private void Start()
    {
        // Initialise le masque pour le layer "Player"
        playerLayerMask = LayerMask.GetMask("Player");
    }
    
    private void Update()
    {
        // Si l'option est activée, récupère le parent ciblé et met à jour l'angle de détection

        
        // Détecte un collider sur le layer "Player" dans la zone définie
        Collider2D playerCollider = Physics2D.OverlapBox(transform.position, detectionBoxSize, detectionBoxAngle, playerLayerMask);
        if (playerCollider != null)
        {
            // Si l'objet détecté possède un script PlayerMovement, déclenche sa fonction Jump()
            PlayerMovement playerMovement = playerCollider.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                if (!playerMovement.isGrappling && playerMovement.RB.linearVelocityY < 0 && playerMovement != player && playerMovement.isJumping == true)
                {
                    playerMovement.Jump();
                    
                    player.KnockBack(Vector2.down, false,10,true, damage);
                }
            }
        }
    }

    
    // Affiche un Gizmo pour visualiser la zone de détection dans l'éditeur
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        float angleToUse = detectionBoxAngle;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0f, 0f, angleToUse), Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, detectionBoxSize);
        Gizmos.matrix = Matrix4x4.identity;
    }
}