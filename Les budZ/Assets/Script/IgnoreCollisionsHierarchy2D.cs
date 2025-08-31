using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
public class IgnoreCollisionWithChildren2D : MonoBehaviour
{
    [Tooltip("Racine de l'objet (et de ses enfants) dont on veut ignorer les collisions")]
    public GameObject ignoreRoot;

    Collider2D[] myColliders;
    Collider2D[] targetColliders;

    void Awake()
    {
        // On stocke nos propres colliders une fois pour toutes
        myColliders = GetComponents<Collider2D>();
    }

    void OnEnable()
    {
        if (ignoreRoot == null)
        {
            Debug.LogWarning($"[{nameof(IgnoreCollisionWithChildren2D)}] : ignoreRoot non défini sur {name}");
            return;
        }

        // Récupère à chaque activation tous les Collider2D de ignoreRoot et ses enfants
        targetColliders = ignoreRoot.GetComponentsInChildren<Collider2D>();

        ApplyIgnore();
    }

    void ApplyIgnore()
    {
        foreach (var myCol in myColliders)
        {
            foreach (var targetCol in targetColliders)
            {
                // On évite le self-ignore (collider identique)
                if (myCol == targetCol) 
                    continue;

                Physics2D.IgnoreCollision(myCol, targetCol, true);
            }
        }
    }
}