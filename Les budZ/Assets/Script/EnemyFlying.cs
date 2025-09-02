using UnityEngine;
using System.Collections;

public class EnemyMovement : MonoBehaviour
{
    // Enumération pour choisir le mode de déplacement
    public enum MovementMode { Transform, Force }

    [Header("Paramètres de Déplacement")]
    [Tooltip("Mode de déplacement (Transform ou Force)")]
    public MovementMode movementMode = MovementMode.Transform;
    
    [Header("Damage")]
    public int damage = 1;
    
    [Tooltip("Tag de l'objet cible (ex: 'Player')")]
    public string targetTag = "Player";
    
    [Tooltip("Vitesse de déplacement (ou vitesse maximale en mode Force)")]
    public float speed = 5f;
    
    [Tooltip("Accélération appliquée en mode Force")]
    public float acceleration = 10f;
    
    [Tooltip("Intervalle (en secondes) pour redétecter la cible la plus proche")]
    public float detectionInterval = 1f;

    [Header("Paramètres de Vie")]
    [Tooltip("Vie de l'ennemi par défaut")]
    public int life = 4;

    [Header("Xp Settings")]
    // Prefab à instancier (doit posséder un Rigidbody2D)
    public GameObject prefabToSpawn;
    // Nombre de prefab à générer lors de la collision
    public int numberOfPrefabs = 5;
    // Force appliquée pour propulser les prefab
    public float launchForce = 5f;
    // Temps avant que le prefab instancié ne se détruise
    public float prefabLifetime = 3f;
    
    [Header("Paramètres de Knockback / Cooldown")]
    [Tooltip("Durée pendant laquelle le comportement de l'ennemi est désactivé après un contact")]
    public float cooldownDuration = 1f;

    [Header("Paramètres KnockBack Joueur")]
    [Tooltip("Force de knockback appliquée au joueur lors d'un contact")]
    public float knockBackForce = 10f;

    [Header("Overlap Box de Détection")]
    [Tooltip("Taille de la zone d'overlap autour de l'ennemi")]
    public Vector2 overlapBoxSize = new Vector2(1f, 1f);
    [Tooltip("Décalage de la zone d'overlap par rapport à la position de l'ennemi")]
    public Vector2 overlapBoxOffset = Vector2.zero;

    [Header("Déclenchement par Overlap Circle")]
    [Tooltip("Rayon de l'overlap circle pour déclencher le chase")]
    public float modeTriggerRadius = 3f;
    [Tooltip("Layers définissant l'objet qui déclenche le chase")]
    public LayerMask modeTriggerLayer;
    [Tooltip("Temps avant de pouvoir retoucher le trigger (optionnel)")]
    public float modeTriggerCooldown = 1f;
    
    [Header("Paramètres de Mort")]
    [Tooltip("Temps avant destruction de l'ennemi après la mort")]
    public float destroyDelay = 1f;

    // Référence au Rigidbody2D (pour le mode Force et la gestion des contraintes)
    private Rigidbody2D rb;
    // Sauvegarde des contraintes initiales (ex: FreezeRotation)
    private RigidbodyConstraints2D originalConstraints;
    
    // Cible assignée (détectée grâce au tag targetTag)
    private Transform target;
    
    // Contrôle du déplacement pendant le cooldown
    private bool canMove = true;
    private bool isInCooldown = false;
    
    // Flags et paramètre pour l'overlap box (déjà présent dans ce script)
    private bool overlapKnockbackTriggered = false;
    private bool playerKnockBackParam = true; // true par défaut

    // Flag indiquant que l'ennemi est mort (désactive overlap box et autres détections)
    private bool isDead = false;
    
    // Nouveau booléen pour contrôler si l'ennemi doit chasser (false par défaut)
    private bool shouldChase = false;
    
    // Flag pour éviter plusieurs déclenchements du trigger overlapCircle
    private bool modeTriggered = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (movementMode == MovementMode.Force && rb == null)
        {
            Debug.LogError("Aucun Rigidbody2D n'est attaché à l'objet pour le mode Force.");
        }
        
        if (rb != null)
        {
            originalConstraints = rb.constraints;
        }
        
        DetectClosestTarget();
        InvokeRepeating("DetectClosestTarget", detectionInterval, detectionInterval);
    }
    
    /// <summary>
    /// Recherche l'objet cible (ayant le tag défini) le plus proche
    /// et le sauvegarde dans la variable target.
    /// </summary>
    void DetectClosestTarget()
    {
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(targetTag);
        if (potentialTargets.Length == 0)
        {
            Debug.LogWarning("Aucun objet trouvé avec le tag : " + targetTag);
            target = null;
            return;
        }
        
        GameObject closest = potentialTargets[0];
        float minDistance = Vector2.Distance(transform.position, closest.transform.position);
        
        foreach (GameObject obj in potentialTargets)
        {
            float distance = Vector2.Distance(transform.position, obj.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = obj;
            }
        }
        target = closest.transform;
    }
    
    public void SpawnPrefabs()
    {
        
        if (numberOfPrefabs <= 0) return;
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Le prefab à instancier n'est pas assigné.");
            return;
        }
        
        // Calcul de l'angle entre chaque prefab pour couvrir un demi-cercle (0° à 180°)
        // Si un seul prefab est à créer, il sera lancé vers le haut (angle de 90°)
        float stepAngle = (numberOfPrefabs > 1) ? 180f / (numberOfPrefabs - 1) : 0f;
        for (int i = 0; i < numberOfPrefabs; i++)
        {
            // Calcul de l'angle en degrés. Dans ce demi-cercle, 90° correspond au haut.
            float angleDeg = (numberOfPrefabs == 1) ? 90f : i * stepAngle;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            
            // Calcul de la direction en fonction de l'angle (les vecteurs de l'arc ont des y >= 0)
            Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            
            // Instanciation du prefab au centre de l'objet
            GameObject instance = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
            
            // Appliquer une force d'impulsion dans la direction calculée
            Rigidbody2D rb = instance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.AddForce(direction * launchForce, ForceMode2D.Impulse);
            }
            else
            {
                Debug.LogWarning("Le prefab instancié n'a pas de Rigidbody2D.");
            }
            
            // Détruire le prefab après 'prefabLifetime' secondes
            Destroy(instance, prefabLifetime);
        }
    }
    void Update()
    {
        // Vérifie en continu l'overlapCircle pour activer le chase
        CheckModeTrigger();
        // On continue d'exécuter la vérification de l'overlapBox (pour les autres interactions)
        CheckOverlapBox();
        
        // Si l'ennemi est mort ou en cooldown, on n'effectue rien
        if (isDead || !canMove)
            return;
        
        // Si le trigger overlapCircle ne s'est pas encore activé, l'ennemi reste immobile
        if (!shouldChase)
            return;
        
        // L'ennemi chase sa cible uniquement s'il y en a une
        if (target != null)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            if (movementMode == MovementMode.Transform)
            {
                transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            }
            else if (movementMode == MovementMode.Force)
            {
                if (rb != null)
                {
                    rb.AddForce(direction * acceleration);
                    if (rb.linearVelocity.magnitude > speed)
                    {
                        rb.linearVelocity = rb.linearVelocity.normalized * speed;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Vérifie la zone de l'overlapCircle autour de l'ennemi.
    /// Si un objet appartenant aux layers définis (modeTriggerLayer) passe dans cette zone,
    /// active le mode chase en passant shouldChase à true.
    /// </summary>
    void CheckModeTrigger()
    {
        if (isDead)
            return;
        if (modeTriggered)
            return;
        
        Collider2D hit = Physics2D.OverlapCircle(transform.position, modeTriggerRadius, modeTriggerLayer);
        
        
        if (hit != null)
        {
            PlayerMovement pm = hit.GetComponent<PlayerMovement>();
            
            if (pm != null && pm.isDead == false)
            {
                modeTriggered = true;
                shouldChase = true; // On active le chase
                Debug.Log("Trigger OverlapCircle activé, l'ennemi commence à chasser la cible !");
                StartCoroutine(ResetModeTrigger());
            }
            else
            {
                Debug.Log("collisiond détécter mais le perso est décédé");
            }
            
        }
    }
    
    /// <summary>
    /// Réinitialise le flag modeTriggered après un délai afin de permettre de retoucher le trigger.
    /// (Vous pouvez ajuster ce comportement selon vos besoins.)
    /// </summary>
    IEnumerator ResetModeTrigger()
    {
        yield return new WaitForSeconds(modeTriggerCooldown);
        modeTriggered = false;
    }
    
    /// <summary>
    /// Vérifie la zone d'overlapBox autour de l'ennemi.
    /// Déclenche des interactions (ex. knockback sur le joueur) si un PlayerMovement en mouvement la traverse.
    /// </summary>
    void CheckOverlapBox()
    {
        if (isDead)
            return;
            
        Vector2 boxCenter = (Vector2)transform.position + overlapBoxOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, overlapBoxSize, 0f);
        
        foreach (Collider2D col in hits)
        {
            PlayerMovement pm = col.GetComponent<PlayerMovement>();
            if (pm != null && pm.isDead == false)
            {
                Rigidbody2D playerRb = col.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    if (!overlapKnockbackTriggered)
                    {
                        overlapKnockbackTriggered = true;
                        playerKnockBackParam = false; // Paramètre envoyé sera false
                        life = 0;
                        Debug.Log("Ennemi tué via OverlapBox avec KnockBack false !");
                        
                        Vector2 direction = (col.transform.position - transform.position).normalized;
                        pm.KnockBack(direction, playerKnockBackParam, knockBackForce, true, damage);
                        
                        StartCoroutine(ResetKnockBackParamAfterCooldown());
                        CheckDeath();
                    }
                    break;
                }
            }
        }
    }
    
    IEnumerator ResetKnockBackParamAfterCooldown()
    {
        yield return new WaitForSeconds(cooldownDuration);
        playerKnockBackParam = true;
        overlapKnockbackTriggered = false;
    }
    
    /// <summary>
    /// Gère les collisions (OnCollisionEnter2D) pour ajuster la vie et appliquer le knockback.
    /// Si un PlayerMovement est détecté et que le joueur n'est pas en dash, appelle sa méthode KnockBack.
    /// Si le joueur est en dash, la vie de l'ennemi passe à 0.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("p1"))
            life -= 1;
        else if (collision.gameObject.CompareTag("p2"))
            life -= 2;
        else if (collision.gameObject.CompareTag("p3"))
            life -= 3;
        else if (collision.gameObject.CompareTag("p4"))
            life -= 4;
        else if (collision.gameObject.CompareTag("Dash"))
            life = 0;
        
        Debug.Log("Vie restante de l'ennemi : " + life);
        CheckDeath();

        if (!isInCooldown && collision.contacts.Length > 0)
        {
            Vector2 contactNormal = collision.contacts[0].normal;
            float impactForce = collision.relativeVelocity.magnitude;
            StartCoroutine(KnockbackCooldown(contactNormal, impactForce));
        }
        
        PlayerMovement playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            if (playerMovement.isDashing)
            {
                life = 0;
                Debug.Log("Ennemi tué par un joueur en dash !");
                CheckDeath();
            }
            else
            {
                Vector2 enemyDirection = (collision.transform.position - transform.position).normalized;
                playerMovement.KnockBack(enemyDirection, playerKnockBackParam, knockBackForce, true, damage);
            }
        }
    }
    
    /// <summary>
    /// Si la vie de l'ennemi tombe à 0 et qu'il n'est pas déjà mort, déclenche Death().
    /// </summary>
    void CheckDeath()
    {
        if (life <= 0 && !isDead)
        {
            Death();
        }
    }
    
    /// <summary>
    /// La méthode Death désactive le déplacement, modifie layer, tag, active la gravité,
    /// désactive les détections (overlap box et circle) et détruit l'objet après un délai.
    /// </summary>
    void Death()
    {
        if (isDead)
            return;
        
        isDead = true;
        canMove = false;
        
        Debug.Log("L'ennemi est mort !");
        
        gameObject.layer = LayerMask.NameToLayer("ProjectileCollision");
        gameObject.tag = "p4";
        
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.zero;
        }
        
        SpawnPrefabs();
        
        Destroy(gameObject, destroyDelay);
    }
    
    /// <summary>
    /// Coroutine gérant le cooldown et le knockback après un impact.
    /// </summary>
    IEnumerator KnockbackCooldown(Vector2 hitDirection, float forceMagnitude)
    {
        isInCooldown = true;
        canMove = false;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = rb.constraints & ~RigidbodyConstraints2D.FreezeRotation;
            rb.AddForce(hitDirection * forceMagnitude, ForceMode2D.Impulse);
            float torqueMultiplier = 1f;
            rb.AddTorque(forceMagnitude * torqueMultiplier, ForceMode2D.Impulse);
        }
        
        yield return new WaitForSeconds(cooldownDuration);
        
        transform.rotation = Quaternion.Euler(0, 0, 0);
        if (rb != null)
        {
            rb.constraints = originalConstraints;
        }
        
        canMove = true;
        isInCooldown = false;
    }

    // Affichage dans l'éditeur des zones d'overlap (box en rouge, circle en bleu)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 boxCenter = (Vector2)transform.position + overlapBoxOffset;
        Gizmos.DrawWireCube(boxCenter, overlapBoxSize);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, modeTriggerRadius);
    }
}
