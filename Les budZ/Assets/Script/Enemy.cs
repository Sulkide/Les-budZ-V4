using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Enemy settings")]
    public int health = 4;    
    public int damage = 1;
    public float recoilForce = 10f;      
    public float destructionDelay = 0.2f;
    public float moveSpeed = 3f;
    public float acceleration = 5f;
    public bool doDamage;
    public float knockbackForce = 50f;
    
    [Header("XP Settings")]
    public GameObject prefabToSpawn;
    // Nombre de prefab à générer lors de la collision
    public int numberOfPrefabs = 5;
    // Force appliquée pour propulser les prefab
    public float launchForce = 5f;
    // Temps avant que le prefab instancié ne se détruise
    public float prefabLifetime = 3f;
    
    [Header("Collision Box Detection")]
    public Vector2 collisionBoxSize = new Vector2(1f, 1f);
    public float collisionBoxAngle = 0f;
    // Vous pouvez définir ici un LayerMask afin de filtrer les objets à détecter (ex. "Player")
    private LayerMask collisionDetectionMask;
    
    [Header("CoolDown settings")]
    public bool canWait = true;
    public float cooldown = 2f;
    
    [Header("Jump settings")]
    public bool CanJump = true;
    public float jumpInterval = 2f;
    public float jumpForce = 5f;
    public float jumpingGravityScale = 7f;
    public float fallingGravityScale = 14f;

    [Header("Chase settings")]
    public bool CanChase = true;
    public float chaseRadius = 5f;
    public bool dontStopChase = false;
    
    [Header("Wander settings")]
    public bool isWandering = false;
    public float wanderTiming = 0f;
    private float currentWanderTimer = 0f;
    private int wanderDirection = 1; // 1 pour droite, -1 pour gauche

    public bool canFall = true;
    public float groundDetectionDistance = 1f;
    public float groundDetectionOffset = 0.5f;
    public LayerMask groundLayer;
    
    [Header("Shoot settings")]
    public bool canShoot = false;
    public float shootRadius = 5f;
    public float shootCooldown = 2f;
    public GameObject shootPrefab;
    public float projectileSpeed = 10f;
    public bool canPredict = false;
    public float predictOffset = 1f;
    public bool dontStopShoot = false;
    private bool isShootOnCooldown = false;
    private bool isShooting = false;

    // Nouveau : zone de détection par OverlapBox pour simuler un trigger
    private bool collisionProcessed = false;
    
    // Nouvelle variable pour la rotation lors de la mort
    [Header("Death settings")]
    public float deathSpinSpeed = 360f; // degrés par seconde pendant la mort

    private Rigidbody2D rb;
    private Vector3 recoilDirection;
    private bool isChasing = false;
    
    List<string> clipsRandom = new List<string> { "punch1", "punch2", "punch3", "punch4" };
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = jumpingGravityScale;
        
        StartCoroutine(JumpRoutine());
        
        collisionDetectionMask = LayerMask.GetMask("Player", "Projectile", "ProjectileCollision");

        if (CanJump)
        {
            
            rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
            rb.constraints &= ~RigidbodyConstraints2D.FreezePositionY;
            
        }

        if (CanChase || isWandering)
        {
            rb.constraints &= ~RigidbodyConstraints2D.FreezePosition;
        }
        

    }
    
    void Update()
    {
        if (health <= 0)
        {
            // Ne rien faire si l'ennemi est en phase de mort
            return;
        }
        
        // Gestion de la gravité selon la vitesse verticale
        if (rb.linearVelocity.y < 0)
            rb.gravityScale = fallingGravityScale;
        else
            rb.gravityScale = jumpingGravityScale;
        
        // Gestion du déplacement (chase et wandering)
        if (CanChase)
            MoveTowardsTargets();
        
        if (isWandering && !isChasing)
        {
            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f)
            {
                FlipDirection();
                currentWanderTimer = 0f;
            }
            else if (wanderTiming > 0)
            {
                currentWanderTimer += Time.deltaTime;
                if (currentWanderTimer >= wanderTiming)
                {
                    FlipDirection();
                    currentWanderTimer = 0f;
                }
            }
            
            if (!canFall)
            {
                Vector2 rayOrigin = (Vector2)transform.position + new Vector2(wanderDirection * groundDetectionOffset, 0f);
                RaycastHit2D groundInfo = Physics2D.Raycast(rayOrigin, Vector2.down, groundDetectionDistance, groundLayer);
                Debug.DrawRay(rayOrigin, Vector2.down * groundDetectionDistance, Color.green);
                if (groundInfo.collider == null)
                {
                    FlipDirection();
                    currentWanderTimer = 0f;
                }
            }
            
            rb.linearVelocity = new Vector2(moveSpeed * wanderDirection, rb.linearVelocity.y);
        }
        
        // Gestion du tir (inchangée)
        if (canShoot && !isShootOnCooldown)
        {
            int shootLayerMask = LayerMask.GetMask("Player");
            Collider2D detected = Physics2D.OverlapCircle(transform.position, shootRadius, shootLayerMask);
            if (detected != null)
                isShooting = true;
            else if (!dontStopShoot)
                isShooting = false;
            
            if (isShooting)
            {
                ShootAtClosestTarget();
                StartCoroutine(ShootCooldown());
            }
        }
        
        // Remplacer OnTriggerEnter par une détection active via OverlapBox
        Collider2D other = Physics2D.OverlapBox(transform.position, collisionBoxSize, collisionBoxAngle, collisionDetectionMask);
        if (other != null)
        {
            if (!collisionProcessed)
            {
                ProcessCollision(other);
                collisionProcessed = true;
            }
        }
        else
        {
            collisionProcessed = false;
        }
    }
    
    // Cette méthode reprend la logique de votre OnTriggerEnter2D
    void ProcessCollision(Collider2D other)
    {
        int damage = 0;
        
        if (other.gameObject.CompareTag("Dash"))
        {
            Debug.Log("test2");
            
            gameObject.layer = LayerMask.NameToLayer("ProjectileCollision");
            int projectileCollisionLayer = LayerMask.NameToLayer("ProjectileCollision");
            SetLayerRecursively(gameObject, projectileCollisionLayer);
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                gameObject.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("ProjectileCollision");
                gameObject.transform.GetChild(i).tag = "p4";
            }
            
            canWait = false;
            CanChase = false;
            CanJump = false;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.freezeRotation = false;
            rb.constraints = RigidbodyConstraints2D.None;
            gameObject.tag = "p4";
            
                
            Vector2 collisionDir = Vector2.zero;
            if (other.attachedRigidbody != null && other.attachedRigidbody.linearVelocity.magnitude > 0.1f)
                collisionDir = other.attachedRigidbody.linearVelocity.normalized;
            if (collisionDir == Vector2.zero)
                collisionDir = (other.transform.position - transform.position).normalized;
                
            rb.AddForce(-collisionDir * recoilForce*10, ForceMode2D.Impulse);

            // Lancer la rotation continue lors de la mort avant destruction
            StartCoroutine(DeathRotation());
            
            return;
        }
        
        // Si l'objet détecté est sur le layer "Player"
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            damage = 1;
            PlayerMovement playerMovement2 = other.gameObject.GetComponent<PlayerMovement>();
            if (playerMovement2 != null)
            {
                if (playerMovement2.isDashing)
                {
                    gameObject.layer = LayerMask.NameToLayer("ProjectileCollision");
                    int projectileCollisionLayer = LayerMask.NameToLayer("ProjectileCollision");
                    SetLayerRecursively(gameObject, projectileCollisionLayer);
                    for (int i = 0; i < gameObject.transform.childCount; i++)
                    {
                        gameObject.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("ProjectileCollision");
                        gameObject.transform.GetChild(i).tag = "p4";
                    }
            
                    canWait = false;
                    CanChase = false;
                    CanJump = false;
                    rb.gravityScale = 0f;
                    rb.linearVelocity = Vector2.zero;
                    rb.freezeRotation = false;
                    rb.constraints = RigidbodyConstraints2D.None;
                    gameObject.tag = "p4";
            
                
                    Vector2 collisionDir = Vector2.zero;
                    if (other.attachedRigidbody != null && other.attachedRigidbody.linearVelocity.magnitude > 0.1f)
                        collisionDir = other.attachedRigidbody.linearVelocity.normalized;
                    if (collisionDir == Vector2.zero)
                        collisionDir = (other.transform.position - transform.position).normalized;
                
                    rb.AddForce(collisionDir * recoilForce*10, ForceMode2D.Impulse);
                
                
                    // Lancer la rotation continue lors de la mort avant destruction
                    StartCoroutine(DeathRotation());
            
                    return;
                }
            }
            
        }
        
        
        // Vérification des tags pour gérer d'autres types de dégâts (p1, p2, p3, p4)
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
               
                
                canWait = false;
                CanChase = false;
                CanJump = false;
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero;
                rb.freezeRotation = false;
                rb.constraints = RigidbodyConstraints2D.None;
                gameObject.tag = "p4";
                gameObject.layer = LayerMask.NameToLayer("ProjectileCollision");
                int projectileCollisionLayer = LayerMask.NameToLayer("ProjectileCollision");
                SetLayerRecursively(gameObject, projectileCollisionLayer);
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    gameObject.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("ProjectileCollision");
                    gameObject.transform.GetChild(i).tag = "p4";
                }
                    
                Vector2 collisionDir = Vector2.zero;
                if (other.attachedRigidbody != null && other.attachedRigidbody.linearVelocity.magnitude > 0.1f)
                    collisionDir = other.attachedRigidbody.linearVelocity.normalized;
                if (collisionDir == Vector2.zero)
                    collisionDir = (other.transform.position - transform.position).normalized;
                
                PlayerMovement playerMovement2 = other.gameObject.GetComponent<PlayerMovement>();
                if (playerMovement2 != null)
                {
                    Vector2 knockBackDirection;
                    if (rb.linearVelocity.magnitude > 0.1f)
                        knockBackDirection = rb.linearVelocity.normalized;
                    else
                        knockBackDirection = playerMovement2.isFacingRight ? Vector2.left : Vector2.right;
                
                    playerMovement2.KnockBack(knockBackDirection, doDamage, knockbackForce, true, this.damage);
                }
                
                rb.AddForce(collisionDir * recoilForce*10, ForceMode2D.Impulse);
                
                
                // Lancer la rotation continue lors de la mort avant destruction
                StartCoroutine(DeathRotation());
                return;
            }
            
            PlayerMovement playerMovement = other.gameObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                Vector2 knockBackDirection;
                if (rb.linearVelocity.magnitude > 0.1f)
                    knockBackDirection = rb.linearVelocity.normalized;
                else
                    knockBackDirection = playerMovement.isFacingRight ? Vector2.left : Vector2.right;
                
                playerMovement.KnockBack(knockBackDirection, doDamage, knockbackForce, true, this.damage);
            }
            
            
            Vector2 collDir = Vector2.zero;
            if (other.attachedRigidbody != null && other.attachedRigidbody.linearVelocity.magnitude > 0.1f)
                collDir = other.attachedRigidbody.linearVelocity.normalized;
            if (collDir == Vector2.zero)
                collDir = (other.transform.position - transform.position).normalized;
            
            rb.linearVelocity = Vector2.zero;
            recoilDirection = (transform.position - other.transform.position).normalized;
            
            
            
            
            rb.AddForce(recoilDirection * 0.1f, ForceMode2D.Impulse);
            //
            
            if (canWait)
                StartCoroutine(CooldownCoroutine(collDir));
        }
    }
    
    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
    

    private IEnumerator DeathRotation()
    {
        float elapsed = 0f;
        SoundManager.Instance.PlayRandomSFX(clipsRandom, 1f, 2);
        while (elapsed < destructionDelay)
        {
            transform.Rotate(0f, 0f, deathSpinSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SpawnPrefabs();
        Destroy(gameObject);
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
    
    private IEnumerator JumpRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(jumpInterval);
            if (CanJump)
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }
    
    private void MoveTowardsTargets()
    {
        bool playerInRange = Physics2D.OverlapCircle(transform.position, chaseRadius, LayerMask.GetMask("Player")) != null;
        if (playerInRange)
            isChasing = true;
        else if (!dontStopChase)
            isChasing = false;
        
        if (!isChasing)
            return;
        
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Target");
        if (targets.Length == 0)
            return;
        
        GameObject closestTarget = targets[0];
        float closestDistance = Vector2.Distance(transform.position, closestTarget.transform.position);
        foreach (GameObject t in targets)
        {
            float dist = Vector2.Distance(transform.position, t.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestTarget = t;
            }
        }
        
        Vector2 direction = ((Vector2)closestTarget.transform.position - (Vector2)transform.position).normalized;
        rb.AddForce(direction * acceleration);
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, moveSpeed);
    }
    
    private IEnumerator CooldownCoroutine(Vector2 collisionDir)
    {
        bool originalCanJump = CanJump;
        bool originalCanChase = CanChase;
        
        CanJump = false;
        CanChase = false;
        rb.linearVelocity = Vector2.zero;
        
        if (collisionDir != Vector2.zero)
            rb.AddForce(collisionDir * recoilForce, ForceMode2D.Impulse);
        
        yield return new WaitForSeconds(cooldown);
        
        CanJump = originalCanJump;
        CanChase = originalCanChase;
    }
    
    // Méthode pour inverser la direction en mode wandering
    private void FlipDirection()
    {
        wanderDirection *= -1;
    }
    
    // --- Méthode de tir avec prédiction ---
    private void ShootAtClosestTarget()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Target");
        if (targets.Length == 0)
            return;
        
        GameObject closestTarget = targets[0];
        float closestDistance = Vector2.Distance(transform.position, closestTarget.transform.position);
        foreach (GameObject t in targets)
        {
            float dist = Vector2.Distance(transform.position, t.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestTarget = t;
            }
        }
        
        Vector2 targetPosition = closestTarget.transform.position;
        if (canPredict)
        {
            Rigidbody2D targetRb = closestTarget.GetComponent<Rigidbody2D>();
            if (targetRb != null && Mathf.Abs(targetRb.linearVelocity.x) > 0.1f)
            {
                float offset = (targetRb.linearVelocity.x < 0) ? -predictOffset : predictOffset;
                targetPosition += new Vector2(offset, 0f);
            }
        }
        
        Vector2 shootDirection = (targetPosition - (Vector2)transform.position).normalized;
        GameObject projectileInstance = Instantiate(shootPrefab, transform.position, Quaternion.identity);
        
        Rigidbody2D projRb = projectileInstance.GetComponent<Rigidbody2D>();
        if (projRb != null)
            projRb.linearVelocity = shootDirection * projectileSpeed;
    }
    
    private IEnumerator ShootCooldown()
    {
        isShootOnCooldown = true;
        yield return new WaitForSeconds(shootCooldown);
        isShootOnCooldown = false;
    }
    
    // Affichage des Gizmos pour visualiser les zones de détection
    private void OnDrawGizmosSelected()
    {
        // Rayon de chase
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
        
        // Rayon de tir
        if (canShoot)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, shootRadius);
        }
        
        // Zone de collision (OverLapBox)
        Gizmos.color = Color.red;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0f, 0f, collisionBoxAngle), Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, collisionBoxSize);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
