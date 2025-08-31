using UnityEngine;

public class AnchorCollisionTrigger : MonoBehaviour
{
    // Assignez via l'inspecteur la référence à la plateforme qui possède le script PlatformWithRopes.
    public PlateformeSuspendue platform;

    // Si le collider de l'ancrage est configuré en "Trigger", utilisez OnTriggerEnter2D :
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("EnemyProjectile") ||
            other.gameObject.layer == LayerMask.NameToLayer("Bullet") ||
            other.gameObject.layer == LayerMask.NameToLayer("BulletProjectile"))
        {
            if (platform != null)
            {
               
            }
        }
    }
    
    
}