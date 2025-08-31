using UnityEngine;

public class BallBouncing : MonoBehaviour
{
    private Rigidbody2D rb;
    private int bounceCount = 0;
    private bool hasStoppedBouncing = false;

    void Start()
    {
        // Récupère le composant Rigidbody2D attaché à la balle
        rb = GetComponent<Rigidbody2D>();
        // Détruit la balle 10 secondes après son apparition
        Destroy(gameObject, 10f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Si la balle n'a pas encore arrêté de rebondir
        if (!hasStoppedBouncing)
        {
            bounceCount++;

            // Après 2 rebonds, on arrête la dynamique de rebond
            if (bounceCount >= 2)
            {
                hasStoppedBouncing = true;
                // Mise à zéro de la vitesse pour empêcher d'autres rebonds
                rb.linearVelocity = Vector2.zero;
                // Désactivation de la gravité pour que la balle ne retombe plus
                rb.gravityScale = 0;
            }
        }
    }
}