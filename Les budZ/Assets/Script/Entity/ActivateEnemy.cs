using System.Collections.Generic;
using UnityEngine;

public class ActivationScripts : MonoBehaviour
{
    // Référence au collider à utiliser (assignable dans l'inspecteur)
    public Collider2D triggerCollider;
    
    // Liste des scripts à activer lors de la collision
    public List<MonoBehaviour> scriptsToActivate;
    
    // Tableau temporaire pour stocker les résultats de détection
    private Collider2D[] results = new Collider2D[10];

    // Stockage des layers valides
    private int layerPlayer;
    private int layerProjectile;
    private int layerProjectileCollision;

    private void Awake()
    {
        // Convertir les noms de layer en int pour faciliter la comparaison
        layerPlayer = LayerMask.NameToLayer("Player");
        layerProjectile = LayerMask.NameToLayer("Projectile");
        layerProjectileCollision = LayerMask.NameToLayer("ProjectileCollision");
    }

    private void Update()
    {
        if (triggerCollider == null)
        {
            Debug.LogError("Le collider de détection n'est pas assigné dans l'inspecteur !");
            return;
        }

        // Configuration du filtre de détection (ici, on inclut les triggers)
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = false; // Modifier si vous souhaitez restreindre à un LayerMask

        // Détecter les collisions sur le collider référencé
        int count = triggerCollider.Overlap(filter, results);
        
        for (int i = 0; i < count; i++)
        {
            int objLayer = results[i].gameObject.layer;
            // Vérifier si l'objet appartient à l'un des layers spécifiés
            if (objLayer == layerPlayer || objLayer == layerProjectile || objLayer == layerProjectileCollision)
            {
                // Activation de tous les scripts listés
                foreach (MonoBehaviour script in scriptsToActivate)
                {
                    if (script != null)
                    {
                        script.enabled = true;
                    }
                }
                // Désactivation de ce script pour éviter de réactiver plusieurs fois
                triggerCollider.enabled = false;
                return; // Sortir dès qu'une détection est effectuée
            }
        }
    }
}
