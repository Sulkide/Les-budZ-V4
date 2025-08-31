using System.Collections;
using UnityEngine;

public class BumperMobil : MonoBehaviour
{
    [Header("Paramètres d'impact")]
    public float collisionForce = 5f;
    public float strongImpactMultiplier = 2f;

    [Header("Gestion de la vie")]
    public int health = 10;
    
    [Header("Cooldown du EnemyProjectileChaser")]
    public float Cooldown = 2f;

    // Récupère le Rigidbody2D du parent
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null)
            Debug.LogWarning("Aucun Rigidbody2D trouvé dans le parent !");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Vérifie que l'objet en collision appartient aux layers ciblés
        int otherLayer = collision.gameObject.layer;
        if (otherLayer == LayerMask.NameToLayer("Player") ||
            otherLayer == LayerMask.NameToLayer("Projectile") ||
            otherLayer == LayerMask.NameToLayer("ProjectileCollision"))
        {
            // Calcule le point de contact (utilise GetContact pour Unity 6)
            Vector2 contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point :
                                     new Vector2(collision.transform.position.x, collision.transform.position.y);

            // Calcule la direction à partir du point de contact jusqu'au centre du parent
            Vector2 direction = (contactPoint - rb.position).normalized;

            // Application de la force sur l'objet collisionné et sur le parent
            Rigidbody2D otherRb = collision.rigidbody;
            if (otherRb != null && rb != null)
            {
                otherRb.AddForce(direction * collisionForce, ForceMode2D.Impulse);
                rb.AddForce(-direction * collisionForce, ForceMode2D.Impulse);
            }

            // Détermine si l'effet doit être appliqué (si layer Player ou tag p1 à p4)
            bool applyTagEffect = false;
            int damage = 0;

            if (otherLayer == LayerMask.NameToLayer("Player"))
            {
                applyTagEffect = true;
                damage = 1;
            }
            else
            {
                string otherTag = collision.gameObject.tag;
                if (otherTag == "p1" || otherTag == "p2" || otherTag == "p3" || otherTag == "p4")
                {
                    applyTagEffect = true;
                    if (otherTag.Length > 1)
                        int.TryParse(otherTag.Substring(1), out damage);
                }
            }

            if (applyTagEffect)
            {
                // Désactive temporairement le script EnemyProjectileChaser sur le parent
                if (transform.parent != null)
                {
                    EnemyProjectileChaser epc = transform.parent.GetComponent<EnemyProjectileChaser>();
                    if (epc != null)
                    {
                        epc.enabled = false;
                        StartCoroutine(ReenableEnemyProjectileChaser(epc, Cooldown));
                    }
                }

                // Application des dégâts
                health -= damage;
                // Si la vie atteint 0 ou moins, applique une impulsion plus forte puis détruit l'objet principal
                if (health <= 0)
                {
                    if (rb != null)
                    {
                        Vector2 strongForceDirection = -direction;
                        rb.AddForce(strongForceDirection * collisionForce * strongImpactMultiplier, ForceMode2D.Impulse);
                    }
                    EnemyProjectileChaser epc = transform.parent.GetComponent<EnemyProjectileChaser>();
                    if (epc != null)
                    {
                        epc.enabled = false;
                        StartCoroutine(ReenableEnemyProjectileChaser(epc, Cooldown));
                    }
                }
            }
        }
    }

    // Coroutine pour réactiver le script EnemyProjectileChaser après le cooldown
    IEnumerator ReenableEnemyProjectileChaser(EnemyProjectileChaser epc, float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        if (health <= 0)
        {
            GameObject mainEntity = transform.parent != null ? transform.parent.gameObject : gameObject;
            Destroy(mainEntity);
        }
        rb.linearVelocity = Vector2.zero;
                if (epc != null)
                    epc.enabled = true;
        
    }
}
