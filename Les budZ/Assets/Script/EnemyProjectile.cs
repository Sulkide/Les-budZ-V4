using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEnemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public int health = 4;
    public int damage = 1;
    public float recoilForce = 10f;
    public float destructionDelay = 0.2f;
    public bool doDamage;
    public float knockbackForce = 50f;

    [Header("Collision Box Detection")]
    public Vector2 collisionBoxSize = new Vector2(1f, 1f);
    public float collisionBoxAngle = 0f;
    // Définir ici les layers à détecter (ex : "Player", "Projectile", etc.)
    private LayerMask collisionDetectionMask;

    [Header("CoolDown settings")]
    public bool canWait = true;
    public float cooldown = 2f;
    // Vitesse de rotation en degrés par seconde pendant le cooldown
    public float spinSpeed = 360f;

    // Vous pouvez ajouter ici d'autres paramètres spécifiques à l'ennemi volant

    private Rigidbody2D rb;
    private bool collisionProcessed = false;
    private EnemyProjectileChaser chaser;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        chaser = GetComponent<EnemyProjectileChaser>();
        rb.gravityScale = 0f; // Pour un ennemi volant, pas de gravité

        // Initialiser le LayerMask de détection (adapté à votre projet)
        collisionDetectionMask = LayerMask.GetMask("Player", "Projectile", "ProjectileCollision");
    }

    void Update()
    {
        if (health <= 0)
            return;

        // Détection de collision active via un OverlapBox (similaire à ce qui est fait dans Enemy.cs)
        Collider2D hit = Physics2D.OverlapBox(transform.position, collisionBoxSize, collisionBoxAngle, collisionDetectionMask);
        if (hit != null)
        {
            if (!collisionProcessed)
            {
                ProcessCollision(hit);
                collisionProcessed = true;
            }
        }
        else
        {
            collisionProcessed = false;
        }
    }
    
    void ProcessCollision(Collider2D other)
    {
        int damage = 0;

        if (other.gameObject.CompareTag("Dash"))
        {
            Die(other);
            
            return;
        }
        
        // Si la collision se fait avec le joueur
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            damage = 1;
            PlayerMovement playerMovement2 = other.gameObject.GetComponent<PlayerMovement>();
            if (playerMovement2 != null)
            {
                if (playerMovement2.isDashing)
                {
                    Die(other);

                    return;
                }
            }
        }

        // Gestion de la collision avec d'autres tags (p1, p2, p3, p4) comme dans Enemy.cs
        if (other.gameObject.CompareTag("p1"))
            damage = 1;
        else if (other.gameObject.CompareTag("p2"))
            damage = 2;
        else if (other.gameObject.CompareTag("p3"))
            damage = 3;
        else if (other.gameObject.CompareTag("p4"))
            damage = 4;

        if (damage > 0)
        {
            health -= damage;
            if (health <= 0)
            {
                Die(other);
            }
            else
            {
                // Lancer le cooldown après la collision et faire tourner l'ennemi pendant ce temps
                StartCoroutine(CooldownCoroutine(other));
            }
        }
    }

    IEnumerator CooldownCoroutine(Collider2D other)
    {
        // Désactivation temporaire des actions pendant le cooldown
        chaser.enabled = false;
        bool originalWait = canWait;
        canWait = false;
        rb.linearVelocity = Vector2.zero;
        Vector2 recoilDirection = (transform.position - other.transform.position).normalized;
        
        PlayerMovement playerMovement = other.gameObject.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            Vector2 knockBackDirection;
            if (rb.linearVelocity.magnitude > 0.1f)
                knockBackDirection = rb.linearVelocity.normalized;
            else
                knockBackDirection = !playerMovement.isFacingRight ? Vector2.left : Vector2.right;

            playerMovement.KnockBack(knockBackDirection, doDamage, knockbackForce, true, damage);
        }
        
        rb.AddForce(recoilDirection * recoilForce, ForceMode2D.Impulse);

        
        
        float elapsedTime = 0f;
        while (elapsedTime < cooldown)
        {
            // Faire tourner l'ennemi sur lui-même
            transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        chaser.enabled = true;
        canWait = originalWait;
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
    
    void Die(Collider2D other)
    {
        StartCoroutine(CooldownCoroutine(other));
        // Gestion de la mort de l'ennemi volant
        rb.linearVelocity = Vector2.zero;
        Vector2 recoilDirection = (transform.position - other.transform.position).normalized;
        rb.AddForce(recoilDirection * recoilForce, ForceMode2D.Impulse);
        Destroy(gameObject, destructionDelay);
    }

    // Affichage de la zone de détection dans l'éditeur
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0f, 0f, collisionBoxAngle), Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, collisionBoxSize);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
