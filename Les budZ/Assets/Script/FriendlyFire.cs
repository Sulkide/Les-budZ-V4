using UnityEngine;

public class KnockbackTrigger : MonoBehaviour
{
    [Header("Paramètres KnockBack")]
    [Tooltip("Force appliquée lors du knockback.")]
    public float force = 10f;

    [Tooltip("Si true, le knockback sera appliqué même en cas de friendly fire.")]
    public bool friendlyFire = false;

    [Header("Ignorer certaines collisions")]
    [Tooltip("Objet dont le collider (et ceux de ses enfants) sera ignoré pour les collisions.")]
    public GameObject ignoreCollisionObject;

    private Collider2D[] myColliders;

    void Start()
    {
        // Récupérer tous les Collider2D attachés à cet objet
        myColliders = GetComponents<Collider2D>();

        // Si un objet à ignorer est défini, ignorer les collisions entre ses colliders (et ceux de ses enfants) et les nôtres
        if (ignoreCollisionObject != null)
        {
            Collider2D[] ignoreColliders = ignoreCollisionObject.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col1 in myColliders)
            {
                foreach (Collider2D col2 in ignoreColliders)
                {
                    Physics2D.IgnoreCollision(col1, col2, true);
                }
            }
        }
    }

    // Utilisation d'un trigger pour détecter l'entrée de l'objet
    void OnTriggerEnter2D(Collider2D other)
    {
        // Vérifier si l'objet déclencheur appartient au layer "Player"
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // Vérifier si l'objet possède le composant PlayerMovement
            PlayerMovement playerMovement = other.gameObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                if (playerMovement.lastOnGroundTime > 0)
                {
                    Debug.Log("test");
                    Vector2 knockBackDirection;
                    // Calculer la direction du knockback (du point de vue de cet objet)
                    knockBackDirection = ignoreCollisionObject.GetComponent<PlayerMovement>().isFacingRight ? Vector2.right : Vector2.left;
                    
                    // Appeler la fonction KnockBack sur le composant PlayerMovement
                    playerMovement.KnockBack((knockBackDirection*10)+(Vector2.up/2), friendlyFire, force*1.5f, true);
                    
                }
            }
        }
    }
}
