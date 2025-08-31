using UnityEngine;

public class DestroyChildOnPlayerCollision : MonoBehaviour
{
    private int playerLayer;

    private void Start()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Vérifie si l'objet en collision appartient au layer "Player"
        if (collision.gameObject.layer == playerLayer)
        {
            // Vérifie qu'il y a au moins un enfant
            if (transform.childCount > 0)
            {
                // Détruit le premier enfant
                Destroy(transform.GetChild(0).gameObject);
            }
            else
            {
                Debug.LogWarning("Aucun enfant à détruire !");
            }
        }
    }
}