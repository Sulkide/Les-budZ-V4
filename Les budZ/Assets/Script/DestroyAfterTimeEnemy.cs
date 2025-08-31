using System.Collections;
using UnityEngine;

public class DestroyAfterTimeEnemy : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Timing & Physics")]
    public float dampingFactor = 0.8f;
    public float destroyAfterTime;

    [Header("KnockBack Settings")]
    public bool doDamage = true;               // Si vrai, le KnockBack appliquera également des dégâts
    public float knockbackForce = 50f;  // Force utilisée pour le knockback sur le joueur

    [Header("Detection Settings")]
    // Rayon de détection pour OverlapCircle
    public float detectionRadius = 0.5f;
    // Masque de layers à détecter (ex : Player, Ground, Projectile, ProjectileCollision)
    public LayerMask collisionDetectionMask;
    
    // Permet d'éviter de traiter plusieurs fois la même collision en rafale
    private bool collisionProcessed = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(DestroyAfter(destroyAfterTime));
    }
    
    private IEnumerator DestroyAfter(float timeToDestroy)
    {
        yield return new WaitForSeconds(timeToDestroy);
        Destroy(gameObject);
    }
    
    void Update()
    {
        // Détection active via OverlapCircle à la position de l'ennemi
        Collider2D detected = Physics2D.OverlapCircle(transform.position, detectionRadius, collisionDetectionMask);
        if (detected != null)
        {
            Debug.Log(detected.name);
            
            if (!collisionProcessed)
            {
                ProcessCollision(detected);
                collisionProcessed = true;
            }
        }
        else
        {
            collisionProcessed = false;
        }
    }
    
    void ProcessCollision(Collider2D detected)
    {
        
        PlayerMovement playerMovement = detected.gameObject.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            Vector2 knockBackDirection;
            if (rb.linearVelocity.magnitude > 0.1f)
                knockBackDirection = rb.linearVelocity.normalized;
            else
                knockBackDirection = playerMovement.isFacingRight ? Vector2.left : Vector2.right;
                
            playerMovement.KnockBack(knockBackDirection, doDamage, knockbackForce, true);
            
            
        }
        
        Debug.Log(detected.name);
        // Change le layer de l'ennemi
        gameObject.layer = LayerMask.NameToLayer("Default");
        collisionDetectionMask = LayerMask.GetMask("Default");
        // Comme OverlapCircle ne fournit pas de contact, on estime la normale à partir de la différence de position
        Vector2 collisionNormal = (transform.position - detected.transform.position).normalized;
        Vector2 newDirection = Vector2.Reflect(rb.linearVelocity, collisionNormal);
        rb.linearVelocity = newDirection * dampingFactor;
        rb.gravityScale = 2f;
        Destroy(gameObject);
            
        // Si l'objet détecté possède le script PlayerMovement, on active sa fonction KnockBack()


    }
    
    // Affiche un cercle de détection dans l'éditeur
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
