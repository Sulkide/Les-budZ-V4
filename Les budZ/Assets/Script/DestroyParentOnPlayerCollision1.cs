using System.Collections.Generic;
using UnityEngine;

public class DestroyParentOnPlayerCollision : MonoBehaviour
{
    // Taille de la zone de détection
    public Vector2 detectionBoxSize = new Vector2(1f, 1f);
    // Angle de la zone de détection (en degrés)
    public float detectionBoxAngle = 0f;
    // Option pour que l'angle de détection suive en permanence la rotation Z du parent ciblé
    public bool matchParentRotation = false;
    // Option pour choisir quel parent détruire (0 = parent immédiat, 1 = parent du parent, etc.)
    public int parentIndexToDestroy = 0;
    
    public float bumpForce = 30;
    
    // Masque de layer pour filtrer uniquement les objets du layer "Player"
    private int playerLayerMask;
    List<string> clipsRandomBonk = new List<string> { "bongo1" };
    private void Start()
    {
        // Initialise le masque pour le layer "Player"
        playerLayerMask = LayerMask.GetMask("Player");
    }
    
    private void Update()
    {
        // Si l'option est activée, récupère le parent ciblé et met à jour l'angle de détection
        if (matchParentRotation)
        {
            Transform targetParent = GetParentAtIndex(parentIndexToDestroy);
            if (targetParent != null)
            {
                detectionBoxAngle = targetParent.eulerAngles.z;
            }
        }
        
        // Détecte un collider sur le layer "Player" dans la zone définie
        Collider2D playerCollider = Physics2D.OverlapBox(transform.position, detectionBoxSize, detectionBoxAngle, playerLayerMask);
        if (playerCollider != null)
        {
            // Si l'objet détecté possède un script PlayerMovement, déclenche sa fonction Jump()
            PlayerMovement playerMovement = playerCollider.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                if (!playerMovement.isGrappling && playerMovement.RB.linearVelocityY < 0)
                {
                    playerMovement.Jump();
                    
                    
                    
                    Transform targetParent = GetParentAtIndex(parentIndexToDestroy);
                    if (targetParent != null)
                    {
                        
                        targetParent.GetComponent<Enemy>().SpawnPrefabs();
                        
                        SoundManager.Instance.PlayRandomSFX(clipsRandomBonk, 1f, 1.7f);
                        
                        Destroy(targetParent.gameObject);
                    }
                    else
                    {
                        Debug.LogWarning("Aucun parent à détruire au niveau " + parentIndexToDestroy);
                    }
                }
            }
        }
    }
    
    // Méthode utilitaire qui retourne le parent au niveau souhaité
    Transform GetParentAtIndex(int index)
    {
        Transform currentParent = transform.parent;
        int currentIndex = 0;
        while (currentParent != null && currentIndex < index)
        {
            currentParent = currentParent.parent;
            currentIndex++;
        }
        return currentParent;
    }
    
    // Affiche un Gizmo pour visualiser la zone de détection dans l'éditeur
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        float angleToUse = detectionBoxAngle;
        if (matchParentRotation)
        {
            Transform targetParent = GetParentAtIndex(parentIndexToDestroy);
            if (targetParent != null)
            {
                angleToUse = targetParent.eulerAngles.z;
            }
        }
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0f, 0f, angleToUse), Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, detectionBoxSize);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
