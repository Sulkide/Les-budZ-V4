using UnityEngine;

public class XPbox : MonoBehaviour
{
    // Prefab à instancier (doit posséder un Rigidbody2D)
    public GameObject prefabToSpawn;
    // Nombre de prefab à générer lors de la collision
    public int numberOfPrefabs = 5;
    // Force appliquée pour propulser les prefab
    public float launchForce = 5f;
    // Temps avant que le prefab instancié ne se détruise
    public float prefabLifetime = 3f;

    // Déclenché lors d'une collision 2D
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Vérifier que l'objet en collision possède le composant PlayerMovement
        // et que sa variable lastOnGroundTime est <= 0
        PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();

        // Vérifier si l'objet est sur le layer "Projectile" ou "ProjectileCollision"
        bool isProjectileLayer = collision.gameObject.layer == LayerMask.NameToLayer("Projectile") ||
                                 collision.gameObject.layer == LayerMask.NameToLayer("ProjectileCollision");

        // Déclencher l'action si c'est le joueur ET que lastOnGroundTime <= 0
        // OU si l'objet appartient aux layers "Projectile" ou "ProjectileCollision"
        if ((player != null && player.lastOnGroundTime <= 0) || isProjectileLayer)
        {
            if (player != null)
            {
                player.Jump();
            }
            
            // Appelle la fonction qui instancie et propulse les prefab autour de l'objet
            SpawnPrefabs();

            // Détruire l'objet XPbox après le déclenchement
            Destroy(gameObject);
        }
    }

    // Fonction pour instancier et lancer les prefab
    private void SpawnPrefabs()
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
}
