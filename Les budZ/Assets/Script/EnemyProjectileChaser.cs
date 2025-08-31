using System.Collections;
using UnityEngine;

public class EnemyProjectileChaser : MonoBehaviour
{
    [Range(1f, 3f)] 
    public int comporetement;
    
    [Header("comportement 1")]
    public float speed = 3f;

    [Header("comportement 2")]
    public float forceMagnitude = 3f;
    public float maxVelocity = 5f;

    [Header("comportement 3")]
    public float timeToSwitch;

    [Header("Chase who ?")] 
    public string TagName = "Target";
    
    [Header("Detection Settings")]
    // Rayon de détection pour activer le chase
    public float detectionRadius = 1f;
    // Layers qui déclenchent le chase (par exemple "Player", "Projectile", "ProjectileCollision")
    public LayerMask detectionLayer;

    private bool isChasing = false;
    private Transform target;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject targetObj = GameObject.FindGameObjectWithTag(TagName);
        if (targetObj != null)
        {
            target = targetObj.transform;
        }
        else
        {
            TagName = "Target";
        }
    }

    void FixedUpdate()
    {
        // Vérification active de la détection via OverlapCircle
        if (!isChasing)
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, detectionLayer);
            if (hit.gameObject != null)
            {
                isChasing = true;
                // Désactivation du Collider si besoin (optionnel)
                Collider2D col = GetComponent<Collider2D>();
                if (col != null)
                    col.enabled = false;
            }
            
        }

        // Mise à jour de la cible
        GameObject targetObj = GameObject.FindGameObjectWithTag(TagName);
        if (targetObj != null)
        {
            target = targetObj.transform;
        }
        else
        {
            TagName = "Target";
        }
        
        if (comporetement == 1)
        {
            if (isChasing && target != null)
            {
                // Déplacement vers la cible
                Vector2 direction = ((Vector2)target.position - rb.position).normalized;
                rb.linearVelocity = direction * speed;
                
                // Mise à jour de la rotation pour pointer vers la cible
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                rb.rotation = angle;
            }
        }
        else if (comporetement == 2)
        {
            if (isChasing && target != null)
            {
                // Application d'une force continue vers la cible
                Vector2 direction = ((Vector2)target.position - rb.position).normalized;
                rb.AddForce(direction * forceMagnitude, ForceMode2D.Force);

                // Limitation de la vitesse
                if (rb.linearVelocity.magnitude > maxVelocity)
                    rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;

                // Mise à jour de la rotation pour pointer vers la direction du mouvement
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                rb.rotation = angle;
            }
        }
        else if (comporetement == 3)
        {
            StartCoroutine(SwitchState(timeToSwitch));
        }
    }

    public IEnumerator SwitchState(float time)
    {
        comporetement = 1;
        yield return new WaitForSeconds(time);
        comporetement = 2;
        yield return new WaitForSeconds(time);
        StartCoroutine(SwitchState(timeToSwitch));
    }
    
    // Affichage du cercle de détection dans l'éditeur
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
