using System.Collections.Generic;
using UnityEngine;

public class ReparentToGrandparentAndFollow : MonoBehaviour
{
    [Header("Target for re-parenting")]
    [Tooltip("On prendra le parent de ce GameObject pour y déposer les enfants de la source.")]
    public GameObject parent;

    [Header("Suivi de position")]
    [Tooltip("Le GameObject qui devra suivre la position de cette source.")]
    public GameObject firstChild;

    void Start()
    {
        if (parent == null)
        {
            Debug.LogWarning("ReparentToGrandparentAndFollow : 'parent' n'est pas défini.", this);
            return;
        }

        Transform grandParent = parent.transform.parent;
        if (grandParent == null)
        {
            Debug.LogWarning(
                $"ReparentToGrandparentAndFollow : '{parent.name}' n'a pas de parent. " +
                "Impossible de re-définir la hiérarchie.",
                this
            );
            return;
        }

        // Copier la liste des enfants pour ne pas modifier la collection en cours d'itération
        var children = new List<Transform>();
        foreach (Transform child in transform)
            children.Add(child);

        // Re-parenter chaque enfant sous le grand-parent
        foreach (Transform child in children)
            child.SetParent(grandParent, worldPositionStays: true);
    }

    void LateUpdate()
    {
        if (firstChild != null)
        {
            // Met à jour chaque frame la position de firstChild
            firstChild.transform.position = transform.position;
        }
    }
}